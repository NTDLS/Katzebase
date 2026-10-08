using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Engine.IO;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    /// <summary>
    /// The index suggestions that are returned with an explain plan.
    /// </summary>
    public class TestIndexAdvisor(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        private static KbClient NewClient()
            => new("127.0.0.1", Constants.LISTEN_PORT, "admin", KbClient.HashPassword(""), "IndexAdvisorTests");

        /// <summary>
        /// Creates (or recreates) an Orders schema with the given indexes, and a Customers schema without any.
        /// </summary>
        private static string CreateSchemas(KbClient client, string name, params string[] indexes)
        {
            var schema = $"TestData:IndexAdvisor:{name}";
            client.Schema.CreateRecursive("TestData:IndexAdvisor");
            client.Schema.DropIfExists(schema);
            client.Schema.Create(schema);
            client.Schema.Create($"{schema}:Orders");
            client.Schema.Create($"{schema}:Customers");
            client.Query.ExecuteNonQuery($"INSERT INTO {schema}:Orders (OrderId, CustomerId, Status, Total) VALUES (1, 1, 'Open', 10), (2, 2, 'Closed', 20)");
            client.Query.ExecuteNonQuery($"INSERT INTO {schema}:Customers (CustomerId, Name) VALUES (1, 'Contoso'), (2, 'Fabrikam')");
            foreach (var index in indexes)
            {
                client.Query.ExecuteNonQuery(index.Replace("{schema}", schema));
            }
            return schema;
        }

        private static string Explain(KbClient client, string statement)
            => string.Join("\n", client.Query.ExplainPlan(statement).Collection.SelectMany(o => o.Messages).Select(o => o.Text));

        [Fact(DisplayName = "Suggests an index for conditions that read every document")]
        public void SuggestsForSchemaScan()
        {
            using var client = NewClient();
            var schema = CreateSchemas(client, "SchemaScan");

            var plan = Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open'");
            Assert.Contains($"CREATE INDEX IX_Orders_Status (Status) ON {schema}:Orders", plan);
            Assert.Contains("every document is read", plan);
        }

        [Fact(DisplayName = "Equality fields come first and at most one range field last")]
        public void CompositeOrdering()
        {
            using var client = NewClient();
            var schema = CreateSchemas(client, "Composite");

            var plan = Explain(client, $"SELECT * FROM {schema}:Orders WHERE Total > 5 AND Status = 'Open' AND CustomerId = 1 AND OrderId BETWEEN 1 AND 9");
            Assert.Contains($"CREATE INDEX IX_Orders_Status_CustomerId_Total (Status, CustomerId, Total) ON {schema}:Orders", plan);

            plan = Explain(client, $"SELECT * FROM {schema}:Orders WHERE Total BETWEEN 5 AND 15");
            Assert.Contains($"CREATE INDEX IX_Orders_Total (Total) ON {schema}:Orders", plan);
        }

        [Fact(DisplayName = "No suggestion when existing indexes already serve the conditions")]
        public void NoSuggestionWhenCovered()
        {
            using var client = NewClient();
            var schema = CreateSchemas(client, "Covered",
                "CREATE INDEX IX_Status_CustomerId (Status, CustomerId) ON {schema}:Orders",
                "CREATE UNIQUEKEY UK_OrderId (OrderId) ON {schema}:Orders",
                "CREATE INDEX IX_Total (Total) ON {schema}:Orders");

            Assert.DoesNotContain("Suggested indexes", Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open' AND CustomerId = 1"));
            //A composite index serves its leading fields.
            Assert.DoesNotContain("Suggested indexes", Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open'"));
            //A unique key pinned by an equality is a single key read, whatever else is filtered.
            Assert.DoesNotContain("Suggested indexes", Explain(client, $"SELECT * FROM {schema}:Orders WHERE OrderId = 1 AND Total = 10"));
            //Indexes that are each pinned by an equality are intersected.
            Assert.DoesNotContain("Suggested indexes", Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open' AND Total = 10"));
            //Conditions that no index can serve.
            Assert.DoesNotContain("Suggested indexes", Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status LIKE '%pen' AND Status != 'x'"));

            //But a field that is not the leading field of an index is not served.
            Assert.Contains($"CREATE INDEX IX_Orders_CustomerId (CustomerId) ON {schema}:Orders",
                Explain(client, $"SELECT * FROM {schema}:Orders WHERE CustomerId = 1"));
        }

        [Fact(DisplayName = "Suggests an index for the joined schema and for each OR group")]
        public void JoinsAndOrGroups()
        {
            using var client = NewClient();
            var schema = CreateSchemas(client, "Join", "CREATE INDEX IX_Status (Status) ON {schema}:Orders");

            var plan = Explain(client, $"SELECT * FROM {schema}:Orders as o INNER JOIN {schema}:Customers as c ON c.CustomerId = o.CustomerId WHERE o.Status = 'Open'");
            Assert.Contains($"CREATE INDEX IX_Customers_CustomerId (CustomerId) ON {schema}:Customers", plan);
            Assert.DoesNotContain("IX_Orders_Status", plan);

            //One OR group without an index makes the whole schema scan.
            plan = Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open' OR Total = 10");
            Assert.Contains($"CREATE INDEX IX_Orders_Total (Total) ON {schema}:Orders", plan);
            Assert.DoesNotContain("IX_Orders_Status", plan);

            //An OR group that does not filter the schema at all: no index on it can help.
            plan = Explain(client, $"SELECT * FROM {schema}:Orders as o INNER JOIN {schema}:Customers as c ON c.CustomerId = o.CustomerId WHERE o.Total = 10 OR c.Name = 'Contoso'");
            Assert.DoesNotContain("IX_Orders_Total", plan);
        }

        [Fact(DisplayName = "Suggests rebuilding an outdated index instead of creating a new one")]
        public void OutdatedIndex()
        {
            using var client = NewClient();
            var schema = CreateSchemas(client, "Outdated", "CREATE INDEX IX_Status (Status) ON {schema}:Orders");

            //Simulate an index created by an older version of the engine.
            using (var ephemeral = _engine.Sessions.CreateEphemeralSystemSession())
            {
                var physicalSchema = _engine.Schemas.Acquire(ephemeral.Transaction, $"{schema}:Orders", LockOperation.Write);
                var rdb = _engine.IO.AcquireDocumentsRdb(physicalSchema);
                var physicalIndex = _engine.Indexes.AcquireIndex(ephemeral.Transaction, physicalSchema, "IX_Status", LockOperation.Write);
                Assert.NotNull(physicalIndex);
                physicalIndex.StorageVersion = 1;
                _engine.IO.PutJson(ephemeral.Transaction, rdb, KbColumnFamilyName.Indexes, new RdbKey(physicalIndex.Id), physicalIndex);
                ephemeral.Commit();
                _engine.Indexes.InvalidateIndexCatalog(rdb);
            }

            var plan = Explain(client, $"SELECT * FROM {schema}:Orders WHERE Status = 'Open'");
            Assert.Contains($"REBUILD INDEX IX_Status ON {schema}:Orders", plan);
            Assert.DoesNotContain("CREATE INDEX", plan);
        }
    }
}
