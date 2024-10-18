using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestQueryBasics : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public TestQueryBasics(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "Select value from single")]
        public void SelectFromSingle()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\SelectFromSingle.kbs");


        [Fact(DisplayName = "ConstantsAndSimpleExpressions")]
        public void TestConstantsAndSimpleExpressions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\ConstantsAndSimpleExpressions.kbs");

        [Fact(DisplayName = "NullsAndNullPropagation")]
        public void TestNullsAndNullPropagation()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\NullsAndNullPropagation.kbs");

        [Fact(DisplayName = "NestedConditions")]
        public void TestNestedConditions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\NestedConditions.kbs");

        [Fact(DisplayName = "AllScalarFunctions")]
        public void TestAllScalarFunctions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\AllScalarFunctions.kbs");

        [Fact(DisplayName = "Formatting")]
        public void TestFormatting()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\Formatting.kbs");
    }
}
