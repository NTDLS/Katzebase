using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestInserts(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Insert using Json notation with and without indexes")]
        public void InsertJsonNotation()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\JsonNotation\CreateSchema.kbs");
            ephemeral.Commit();

            //Test inserts.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\Insert.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\InsertWithExpressions.kbs");

            //Create a unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\CreateUniqueKey.kbs");

            //Ensure that inserting a duplicate causes a failure.
            Assert.Throws<KbDuplicateKeyViolationException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\InsertCreatingDuplicates.kbs"));

            //Remove the unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\DropUniqueKey.kbs");

            //Ensure we can insert duplicates.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\InsertCreatingDuplicates.kbs");

            //Ensure the unique key creation causes a failure.
            var exception = Assert.Throws<AggregateException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\CreateUniqueKey.kbs"));
            Assert.Contains(exception.InnerExceptions, ex => ex is KbDuplicateKeyViolationException);

            //Test select before and after creation of an index.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\SelectBeforeAndAfterIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\CreateIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\SelectBeforeAndAfterIndex.kbs");

            //Insert rows into new index and validate result.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation\InsertAfterIndex.kbs");
        }

        [Fact(DisplayName = "Insert using values list with and without indexes")]
        public void InsertValuesList()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\ValuesList\CreateSchema.kbs");
            ephemeral.Commit();

            //Test inserts.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\Insert.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\InsertWithExpressions.kbs");

            //Create a unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\CreateUniqueKey.kbs");

            //Ensure that inserting a duplicate causes a failure.
            Assert.Throws<KbDuplicateKeyViolationException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\InsertCreatingDuplicates.kbs"));

            //Remove the unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\DropUniqueKey.kbs");

            //Ensure we can insert duplicates.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\InsertCreatingDuplicates.kbs");

            //Ensure the unique key creation causes a failure.
            var exception = Assert.Throws<AggregateException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\CreateUniqueKey.kbs"));
            Assert.Contains(exception.InnerExceptions, ex => ex is KbDuplicateKeyViolationException);

            //Test select before and after creation of an index.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\SelectBeforeAndAfterIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\CreateIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\SelectBeforeAndAfterIndex.kbs");

            //Insert rows into new index and validate result.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList\InsertAfterIndex.kbs");
        }
    }
}
