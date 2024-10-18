using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Tests
{
    public class TestInserts : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public TestInserts(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "Insert JsonNotation")]
        public void InsertJsonNotation()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\CreateJsonNotationSchema.kbs");
            ephemeral.Commit();

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotationWithExpressions.kbs");
        }

        [Fact(DisplayName = "Insert ValuesList")]
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
