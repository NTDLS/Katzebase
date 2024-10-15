using NTDLS.Katzebase.Engine.Tests.QueryExpectations;

namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreTests : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public EngineCoreTests(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "Select Single Value.")]
        public void SelectFromSingle()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var actual = ephemeral.Transaction.ExecuteScalar<string?>("SELECT Value FROM Single");
            Assert.Null(actual);
        }

        [Fact(DisplayName = "Constants and Simple Expressions.")]
        public void ConstantsAndSimpleExpressions()
        {
            QueryExpectation.ValidateScriptResults(_engine, "ConstantsAndSimpleExpressions.kbs");
        }

        [Fact(DisplayName = "Simple Selects, Joins, Groups and Order By.")]
        public void TestBasicQueries()
        {
            QueryExpectation.ValidateScriptResults(_engine, "RaggedInsert.kbs");
            QueryExpectation.ValidateScriptResults(_engine, "OrdersAndItemsForSpecificPerson.kbs", new { PersonId = 1 });
            QueryExpectation.ValidateScriptResults(_engine, "OrdersAndTheirItems.kbs");
            QueryExpectation.ValidateScriptResults(_engine, "OrdersWithPersonDetails.kbs");
            QueryExpectation.ValidateScriptResults(_engine, "PersonsAndTheirAddresses.kbs");
            //QueryExpectation.ValidateScriptResults(_engine, "PersonsWithoutOrders.kbs"); //Not implemented: LEFT OUTER
            QueryExpectation.ValidateScriptResults(_engine, "PersonWithOrdersDetails.kbs");
            QueryExpectation.ValidateScriptResults(_engine, "TotalAmountSpentByPerson.kbs");
            QueryExpectation.ValidateScriptResults(_engine, "TotalQuantityOfItemsOrderedPerOrder.kbs");
        }

        [Fact]
        public async Task TestParallelExecution()
        {
            var tasks = new List<Task>();

            tasks.Add(Task.Run(() => DoParallelUnitWork()));
            tasks.Add(Task.Run(() => DoParallelUnitWork()));

            await Task.WhenAll(tasks);
        }

        private Task<int> DoParallelUnitWork()
        {

            return Task.FromResult(0);
        }
    }
}
