using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestInserts(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Insert using Json notation")]
        public void InsertJsonNotation()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\CreateJsonNotationSchema.kbs");
            ephemeral.Commit();

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotationWithExpressions.kbs");
        }

        [Fact(DisplayName = "Insert using values list")]
        public void InsertValuesList()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\CreateValuesListSchema.kbs");
            ephemeral.Commit();

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesListWithExpressions.kbs");
        }
    }
}
