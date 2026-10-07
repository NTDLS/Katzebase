using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    /// <summary>
    /// Ensures that index entries stay consistent with documents across rollbacks, updates and concurrent writers.
    /// </summary>
    public class TestIndexConsistency(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        private void Execute(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(queryText);
            ephemeral.Commit();
        }

        private void ExecuteAndRollback(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(queryText);
            ephemeral.Rollback();
        }

        private int RowCount(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var results = ephemeral.Transaction.ExecuteQuery(queryText);
            ephemeral.Commit();
            return results.Collection[0].Rows.Count;
        }

        /// <summary>
        /// Creates an empty schema with a non-unique index on [Category] and a unique key on [Code].
        /// </summary>
        private string CreateIndexedSchema(string name)
        {
            var schema = $"TestData:IndexConsistency:{name}";
            Execute("CREATE SCHEMA TestData:IndexConsistency");
            Execute($"DROP SCHEMA {schema}");
            Execute($"CREATE SCHEMA {schema}");
            Execute($"CREATE INDEX ix_Category (Category) ON {schema}");
            Execute($"CREATE UniqueKey uk_Code (Code) ON {schema}");
            Execute($"INSERT INTO {schema}(Code, Category) VALUES('a1', 'x'),('a2', 'x'),('a3', 'y')");
            return schema;
        }

        [Fact(DisplayName = "Rolled back insert does not leave index entries behind")]
        public void RolledBackInsert()
        {
            var schema = CreateIndexedSchema("RolledBackInsert");

            ExecuteAndRollback($"INSERT INTO {schema}(Code, Category) VALUES('r1', 'x')");

            Assert.Equal(2, RowCount($"SELECT * FROM {schema} WHERE Category = 'x'"));
            Assert.Equal(0, RowCount($"SELECT * FROM {schema} WHERE Code = 'r1'"));

            //The unique value must be available again.
            Execute($"INSERT INTO {schema}(Code, Category) VALUES('r1', 'z')");
            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'r1'"));
        }

        [Fact(DisplayName = "Failed insert (unique violation) does not leave index entries behind")]
        public void FailedInsert()
        {
            var schema = CreateIndexedSchema("FailedInsert");

            Assert.Throws<KbDuplicateKeyViolationException>(()
                => Execute($"INSERT INTO {schema}(Code, Category) VALUES('a1', 'q')"));

            Assert.Equal(0, RowCount($"SELECT * FROM {schema} WHERE Category = 'q'"));
            Assert.Equal(3, RowCount($"SELECT * FROM {schema} WHERE Category != 'q'"));
        }

        [Fact(DisplayName = "Rolled back delete restores index entries")]
        public void RolledBackDelete()
        {
            var schema = CreateIndexedSchema("RolledBackDelete");

            ExecuteAndRollback($"DELETE FROM {schema} WHERE Code = 'a1'");

            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'a1'"));
            Assert.Equal(2, RowCount($"SELECT * FROM {schema} WHERE Category = 'x'"));
        }

        [Fact(DisplayName = "Rolled back update restores index entries")]
        public void RolledBackUpdate()
        {
            var schema = CreateIndexedSchema("RolledBackUpdate");

            ExecuteAndRollback($"UPDATE {schema} SET Category = 'q', Code = 'b3' WHERE Code = 'a3'");

            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Category = 'y'"));
            Assert.Equal(0, RowCount($"SELECT * FROM {schema} WHERE Category = 'q'"));
            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'a3'"));
            Assert.Equal(0, RowCount($"SELECT * FROM {schema} WHERE Code = 'b3'"));
        }

        [Fact(DisplayName = "Updating an indexed value moves the document to the new index entry")]
        public void UpdateMovesIndexEntry()
        {
            var schema = CreateIndexedSchema("UpdateMovesIndexEntry");

            Execute($"UPDATE {schema} SET Code = 'b1' WHERE Code = 'a1'");

            //The old unique value must have been released.
            Execute($"INSERT INTO {schema}(Code, Category) VALUES('a1', 'z')");
            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'a1'"));
            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'b1'"));

            //Moving a value away and back again must not leave a duplicate entry.
            Execute($"UPDATE {schema} SET Code = 'c1' WHERE Code = 'b1'");
            Execute($"UPDATE {schema} SET Code = 'b1' WHERE Code = 'c1'");
            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Code = 'b1'"));
        }

        [Fact(DisplayName = "Lookups that use multiple indexes return exact results")]
        public void MultipleIndexLookups()
        {
            var schema = CreateIndexedSchema("MultipleIndexLookups");

            Assert.Equal(1, RowCount($"SELECT * FROM {schema} WHERE Category = 'x' AND Code = 'a2'"));
            Assert.Equal(0, RowCount($"SELECT * FROM {schema} WHERE Category = 'y' AND Code = 'a2'"));
            Assert.Equal(2, RowCount($"SELECT * FROM {schema} WHERE Category = 'y' OR Code = 'a1'"));
        }

        [Fact(DisplayName = "Concurrent updates of the same schema do not deadlock")]
        public void ConcurrentUpdates()
        {
            var schema = CreateIndexedSchema("ConcurrentUpdates");

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();

            Parallel.For(0, 8, thread =>
            {
                for (int i = 0; i < 20; i++)
                {
                    try
                    {
                        Execute($"UPDATE {schema} SET Value = '{thread}:{i}' WHERE Code = 'a{1 + (thread % 3)}'");
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }
            });

            Assert.Empty(exceptions);
        }
    }
}
