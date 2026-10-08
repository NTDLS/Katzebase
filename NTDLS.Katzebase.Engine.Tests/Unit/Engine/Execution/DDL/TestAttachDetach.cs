using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.IO;
using static NTDLS.Katzebase.Api.KbConstants;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DDL
{
    /// <summary>
    /// ATTACH SCHEMA ... FROM '...' and DETACH SCHEMA ... TO '...'.
    /// </summary>
    public class TestAttachDetach(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>, IDisposable
    {
        private readonly EngineCore _engine = fixture.Engine;
        private readonly string _workFolder = Path.Combine(Path.GetTempPath(), "KatzebaseAttachDetachTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try { Directory.Delete(_workFolder, true); } catch { }
        }

        private KbQueryResultCollection Execute(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var results = ephemeral.Transaction.ExecuteNonQuery(queryText);
            ephemeral.Commit();
            return results;
        }

        private int RowCount(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var results = ephemeral.Transaction.ExecuteQuery(queryText);
            ephemeral.Commit();
            return results.Collection[0].Rows.Count;
        }

        private Guid SchemaId(string schemaName)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var id = _engine.Schemas.Acquire(ephemeral.Transaction, schemaName, LockOperation.Read).Id;
            ephemeral.Commit();
            return id;
        }

        /// <summary>
        /// Creates [root]:[name] with a child schema, documents, a non-unique index and a unique key.
        /// </summary>
        private string CreateNamespace(string name)
        {
            var schema = $"TestData:AttachDetach:{name}";
            Execute("CREATE SCHEMA TestData:AttachDetach");
            Execute($"DROP SCHEMA {schema}");
            Execute($"CREATE SCHEMA {schema}");
            Execute($"CREATE SCHEMA {schema}:Child");
            Execute($"CREATE INDEX ix_Category (Category) ON {schema}");
            Execute($"CREATE UniqueKey uk_Code (Code) ON {schema}");
            Execute($"INSERT INTO {schema}(Code, Category) VALUES('a1', 'x'),('a2', 'x'),('a3', 'y')");
            Execute($"INSERT INTO {schema}:Child(Name) VALUES('c1'),('c2')");
            return schema;
        }

        [Fact(DisplayName = "Detach and re-attach a namespace with its documents, indexes and child schemas")]
        public void DetachThenAttach()
        {
            var schema = CreateNamespace("RoundTrip");
            var folder = Path.Combine(_workFolder, "RoundTrip");

            Execute($"DETACH SCHEMA {schema} TO '{folder}'");

            Assert.True(Directory.Exists(Path.Combine(folder, DocumentsFile)));
            Assert.True(Directory.Exists(Path.Combine(folder, "Child")));
            Assert.Throws<KbObjectNotFoundException>(() => RowCount($"SELECT * FROM {schema}"));

            var attached = "TestData:AttachDetach:RoundTripRestored";
            Execute($"DROP SCHEMA {attached}");
            Execute($"ATTACH SCHEMA {attached} FROM '{folder}'");

            //The source folder is copied, not consumed.
            Assert.True(Directory.Exists(Path.Combine(folder, DocumentsFile)));

            Assert.Equal(3, RowCount($"SELECT * FROM {attached}"));
            Assert.Equal(2, RowCount($"SELECT * FROM {attached} WHERE Category = 'x'"));
            Assert.Equal(2, RowCount($"SELECT * FROM {attached}:Child"));

            //The attached indexes are maintained and enforced.
            Execute($"INSERT INTO {attached}(Code, Category) VALUES('a4', 'x')");
            Assert.Equal(3, RowCount($"SELECT * FROM {attached} WHERE Category = 'x'"));
            Assert.Throws<KbDuplicateKeyViolationException>(() => Execute($"INSERT INTO {attached}(Code, Category) VALUES('a1', 'z')"));
        }

        [Fact(DisplayName = "Attaching the same folder twice gives each namespace its own schema ids")]
        public void AttachTwice()
        {
            var schema = CreateNamespace("Twice");
            var folder = Path.Combine(_workFolder, "Twice");
            Execute($"DETACH SCHEMA {schema} TO '{folder}'");

            var first = "TestData:AttachDetach:TwiceA";
            var second = "TestData:AttachDetach:TwiceB";
            Execute($"DROP SCHEMA {first}");
            Execute($"DROP SCHEMA {second}");
            Execute($"ATTACH SCHEMA {first} FROM '{folder}'");
            Execute($"ATTACH SCHEMA {second} FROM '{folder}'");

            Assert.NotEqual(SchemaId(first), SchemaId(second));
            Assert.NotEqual(SchemaId($"{first}:Child"), SchemaId($"{second}:Child"));

            //The two copies are independent.
            Execute($"INSERT INTO {first}(Code, Category) VALUES('only-in-first', 'x')");
            Assert.Equal(4, RowCount($"SELECT * FROM {first}"));
            Assert.Equal(3, RowCount($"SELECT * FROM {second}"));
        }

        [Fact(DisplayName = "Attach reports indexes stored in an older format")]
        public void AttachReportsOutdatedIndexes()
        {
            var schema = CreateNamespace("Outdated");

            //Simulate an index created by an older version of the engine.
            using (var ephemeral = _engine.Sessions.CreateEphemeralSystemSession())
            {
                var physicalSchema = _engine.Schemas.Acquire(ephemeral.Transaction, schema, LockOperation.Write);
                var rdb = _engine.IO.AcquireDocumentsRdb(physicalSchema);
                var physicalIndex = _engine.Indexes.AcquireIndex(ephemeral.Transaction, physicalSchema, "ix_Category", LockOperation.Write);
                Assert.NotNull(physicalIndex);
                physicalIndex.StorageVersion = 1;
                _engine.IO.PutJson(ephemeral.Transaction, rdb, KbColumnFamilyName.Indexes, new RdbKey(physicalIndex.Id), physicalIndex);
                ephemeral.Commit();
                _engine.Indexes.InvalidateIndexCatalog(rdb);
            }

            var folder = Path.Combine(_workFolder, "Outdated");
            Execute($"DETACH SCHEMA {schema} TO '{folder}'");

            var attached = "TestData:AttachDetach:OutdatedRestored";
            Execute($"DROP SCHEMA {attached}");
            var results = Execute($"ATTACH SCHEMA {attached} FROM '{folder}'");

            var warnings = results.Collection.SelectMany(o => o.Messages).Where(o => o.MessageType == KbMessageType.Warning).ToList();
            Assert.Contains(warnings, o => o.Text.Contains("ix_Category") && o.Text.Contains("REBUILD INDEX"));
            Assert.DoesNotContain(warnings, o => o.Text.Contains("uk_Code"));

            //Queries still work (without the outdated index) and rebuilding makes it usable.
            Assert.Equal(2, RowCount($"SELECT * FROM {attached} WHERE Category = 'x'"));
            Execute($"REBUILD INDEX ix_Category ON {attached}");
            Execute($"INSERT INTO {attached}(Code, Category) VALUES('a4', 'x')");
            Assert.Equal(3, RowCount($"SELECT * FROM {attached} WHERE Category = 'x'"));
        }

        [Fact(DisplayName = "Attach and detach refuse unsafe or invalid requests")]
        public void InvalidRequests()
        {
            var schema = CreateNamespace("Invalid");
            var folder = Path.Combine(_workFolder, "Invalid");
            Execute($"DETACH SCHEMA {schema} TO '{folder}'");
            Execute($"ATTACH SCHEMA {schema} FROM '{folder}'");

            //The target schema already exists.
            Assert.Throws<KbObjectAlreadyExistsException>(() => Execute($"ATTACH SCHEMA {schema} FROM '{folder}'"));

            //The destination folder already exists.
            Assert.Throws<KbObjectAlreadyExistsException>(() => Execute($"DETACH SCHEMA {schema} TO '{folder}'"));

            //Not a schema folder.
            var notASchema = Path.Combine(_workFolder, "NotASchema");
            Directory.CreateDirectory(notASchema);
            Assert.Throws<KbGenericException>(() => Execute($"ATTACH SCHEMA TestData:AttachDetach:Nope FROM '{notASchema}'"));

            //Missing folder.
            Assert.Throws<KbObjectNotFoundException>(() => Execute($"ATTACH SCHEMA TestData:AttachDetach:Nope FROM '{Path.Combine(_workFolder, "Missing")}'"));

            //Relative paths.
            Assert.Throws<KbInvalidArgumentException>(() => Execute("ATTACH SCHEMA TestData:AttachDetach:Nope FROM 'relative\\folder'"));

            //Folders within the server's data folder.
            Assert.Throws<KbInvalidArgumentException>(() => Execute($"DETACH SCHEMA {schema} TO '{Path.Combine(_engine.Settings.DataRootPath, "Exported")}'"));

            //Not allowed in an explicit transaction.
            Assert.ThrowsAny<Exception>(() => Execute($"BEGIN TRANSACTION\r\nDETACH SCHEMA {schema} TO '{Path.Combine(_workFolder, "InTransaction")}'"));

            //Failed requests leave the schema intact and attached.
            Assert.Equal(3, RowCount($"SELECT * FROM {schema}"));
            Assert.False(Directory.Exists(Path.Combine(_workFolder, "InTransaction")));
            Assert.Throws<KbObjectNotFoundException>(() => RowCount("SELECT * FROM TestData:AttachDetach:Nope"));
        }
    }
}
