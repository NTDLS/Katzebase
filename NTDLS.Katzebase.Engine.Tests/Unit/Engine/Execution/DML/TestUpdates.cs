using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestUpdates(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Update with and without indexes")]
        public void Update()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Updates\CreateSchema.kbs");
            ephemeral.Commit();

            //Test inserts.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\Insert.kbs");

            //Create a unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\CreateUniqueKey.kbs");

            //Ensure that updating to a duplicate causes a failure.
            Assert.Throws<KbDuplicateKeyViolationException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\UpdateCreatingDuplicates.kbs"));

            //Remove the unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\DropUniqueKey.kbs");

            //Ensure we can insert duplicates.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\UpdateCreatingDuplicates.kbs");

            //Ensure the unique key creation causes a failure.
            var exception = Assert.Throws<AggregateException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\CreateUniqueKey.kbs"));
            Assert.Contains(exception.InnerExceptions, ex => ex is KbDuplicateKeyViolationException);

            //Test select before and after creation of an index.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\SelectBeforeAndAfterIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\CreateIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\SelectBeforeAndAfterIndex.kbs");

            //Insert rows into new index and validate result.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\UpdateAfterIndex.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\UpdateAfterIndexCreatingNewField.kbs");

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Updates\UpdateWithJoin.kbs");
        }
    }
}
