using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    public class TestMockERPQueries(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        [Fact(DisplayName = "Number of orders placed by each person (1-to-many, group by)")]
        public void NumberOfOrdersPlacedByEachPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\NumberOfOrdersPlacedByEachPerson.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "Persons without orders (left-outer-join)")]
        public void PersonsWithoutOrders()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsWithoutOrders.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "Orders and items for specific person (1-to-many, variable)")]
        public void OrdersAndItemsForSpecificPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndItemsForSpecificPerson.kbs", new { PersonId = 1 });

        [Fact(DisplayName = "Orders and their items (1-to-many)")]
        public void TestOrdersAndTheirItems()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndTheirItems.kbs");

        [Fact(DisplayName = "Orders with person details (1-to-many)")]
        public void TestOrdersWithPersonDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersWithPersonDetails.kbs");

        [Fact(DisplayName = "Persons and their addresses (1-to-1)")]
        public void TestPersonsAndTheirAddresses()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsAndTheirAddresses.kbs");

        [Fact(DisplayName = "Person with orders details (many-to-1)")]
        public void TestPersonWithOrdersDetails()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonWithOrdersDetails.kbs");

        [Fact(DisplayName = "Total amount spent by person (1-to-many)")]
        public void TestTotalAmountSpentByPerson()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalAmountSpentByPerson.kbs");

        [Fact(DisplayName = "Total quantity of items ordered per order (1-to-many, group by)")]
        public void TestTotalQuantityOfItemsOrderedPerOrder()
            => QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalQuantityOfItemsOrderedPerOrder.kbs");
    }
}
