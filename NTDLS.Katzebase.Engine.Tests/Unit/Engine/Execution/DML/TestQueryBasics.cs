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

        [Fact(DisplayName = "Constants and simple expressions")]
        public void TestConstantsAndSimpleExpressions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\ConstantsAndSimpleExpressions.kbs");

        [Fact(DisplayName = "Nulls and Null Propagation")]
        public void TestNullsAndNullPropagation()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\NullsAndNullPropagation.kbs");

        [Fact(DisplayName = "Nested conditions or And/Or")]
        public void TestNestedConditions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\NestedConditions.kbs");

        [Fact(DisplayName = "All scalar functions")]
        public void TestAllScalarFunctions()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\AllScalarFunctions.kbs");

        [Fact(DisplayName = "String and DateTime formatting")]
        public void TestFormatting()
            => QueryExpectation.ValidateScriptResults(_engine, @"Features\Formatting.kbs");
    }
}
