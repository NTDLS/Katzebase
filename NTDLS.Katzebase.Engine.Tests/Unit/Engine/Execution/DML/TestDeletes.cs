using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestDeletes(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Deletes with and without indexes")]
        public void Deletes()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Deletes\CreateSchema.kbs");
            ephemeral.Commit();

            //Insert some test data.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\Insert.kbs");

            //Delete a few rows.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\DeleteWhereLastNameBrown.kbs");

            //Create an index.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\CreateIndex.kbs");

            //Delete a few rows.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\DeleteWhereLastNameDoe.kbs");

            //Ensure the unique key creation causes a failure.
            var exception = Assert.Throws<AggregateException>(()
                => QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\CreateUniqueKey.kbs"));
            Assert.Contains(exception.InnerExceptions, ex => ex is KbDuplicateKeyViolationException);

            //Remove the duplicate rows
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\DeleteDuplicates.kbs");

            //Create a unique key.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\CreateUniqueKey.kbs");

            //More sophisticated delete.
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Deletes\DeleteWithJoin.kbs");
        }
    }
}
