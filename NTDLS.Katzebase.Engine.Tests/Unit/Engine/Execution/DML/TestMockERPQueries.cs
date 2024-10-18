using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestMockERPQueries : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public TestMockERPQueries(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "NumberOfOrdersPlacedByEachPerson (1-to-many, group by)")]
        public void NumberOfOrdersPlacedByEachPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\NumberOfOrdersPlacedByEachPerson.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "PersonsWithoutOrders (left-outer-join)")]
        public void PersonsWithoutOrders()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsWithoutOrders.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "OrdersAndItemsForSpecificPerson (1-to-many, variable)")]
        public void OrdersAndItemsForSpecificPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndItemsForSpecificPerson.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "OrdersAndTheirItems (1-to-many)")]
        public void TestOrdersAndTheirItems()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndTheirItems.kbs");

        [Fact(DisplayName = "OrdersWithPersonDetails (1-to-many)")]
        public void TestOrdersWithPersonDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersWithPersonDetails.kbs");

        [Fact(DisplayName = "PersonsAndTheirAddresses (1-to-1)")]
        public void TestPersonsAndTheirAddresses()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsAndTheirAddresses.kbs");

        [Fact(DisplayName = "PersonWithOrdersDetails (many-to-1)")]
        public void TestPersonWithOrdersDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonWithOrdersDetails.kbs");

        [Fact(DisplayName = "TotalAmountSpentByPerson (1-to-many)")]
        public void TestTotalAmountSpentByPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalAmountSpentByPerson.kbs");

        [Fact(DisplayName = "TotalQuantityOfItemsOrderedPerOrder (1-to-many, group by)")]
        public void TestTotalQuantityOfItemsOrderedPerOrder()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalQuantityOfItemsOrderedPerOrder.kbs");
    }
}
