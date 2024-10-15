using NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations;

namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreTests : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public EngineCoreTests(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact(DisplayName = "Select value from single")]
        public void SelectFromSingle()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var actual = ephemeral.Transaction.ExecuteScalar<string?>("SELECT Value FROM Single");
            Assert.Null(actual);
        }

        [Fact(DisplayName = "Query basics")]
        public void TestBasicQueries()
        {
            QueryExpectation.ValidateScriptResults(_engine, @"Features\ConstantsAndSimpleExpressions.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\ConstantsAndSimpleExpressions.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\NullsAndNullPropagation.kbs");
        }

        [Fact(DisplayName = "Mock ERP queries")]
        public void MockERPQueries()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"MockERPQueries\CreateSchema.kbs");
            ephemeral.Transaction.ExecuteNonQuery(@"MockERPQueries\CreateSchemaData.kbs");
            ephemeral.Commit();

            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndItemsForSpecificPerson.kbs", new { PersonId = 1 });
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersAndTheirItems.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\OrdersWithPersonDetails.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsAndTheirAddresses.kbs");
            //QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonsWithoutOrders.kbs"); //Not implemented: LEFT OUTER
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\PersonWithOrdersDetails.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalAmountSpentByPerson.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"MockERPQueries\TotalQuantityOfItemsOrderedPerOrder.kbs");
        }


        [Fact(DisplayName = "Insert queries")]
        public void TestInsertQueries()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\CreateJsonNotationSchema.kbs");
            ephemeral.Transaction.ExecuteNonQuery(@"Features\Inserts\CreateValuesListSchema.kbs");
            ephemeral.Commit();

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotation.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\JsonNotationWithExpressions.kbs");

            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesList.kbs");
            QueryExpectation.ValidateScriptResults(_engine, @"Features\Inserts\ValuesListWithExpressions.kbs");
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
