using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Tests
{
    public class TestMockERPQueries : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public TestMockERPQueries(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        //QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsWithoutOrders.kbs"); //Not implemented: LEFT OUTER

        [Fact(DisplayName = "OrdersAndItemsForSpecificPerson")]
        public void OrdersAndItemsForSpecificPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndItemsForSpecificPerson.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "OrdersAndTheirItems")]
        public void TestOrdersAndTheirItems()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndTheirItems.kbs");

        [Fact(DisplayName = "OrdersWithPersonDetails")]
        public void TestOrdersWithPersonDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersWithPersonDetails.kbs");

        [Fact(DisplayName = "PersonsAndTheirAddresses")]
        public void TestPersonsAndTheirAddresses()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsAndTheirAddresses.kbs");

        [Fact(DisplayName = "PersonWithOrdersDetails")]
        public void TestPersonWithOrdersDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonWithOrdersDetails.kbs");

        [Fact(DisplayName = "TotalAmountSpentByPerson")]
        public void TestTotalAmountSpentByPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalAmountSpentByPerson.kbs");

        [Fact(DisplayName = "TotalQuantityOfItemsOrderedPerOrder")]
        public void TestTotalQuantityOfItemsOrderedPerOrder()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalQuantityOfItemsOrderedPerOrder.kbs");
    }
}
