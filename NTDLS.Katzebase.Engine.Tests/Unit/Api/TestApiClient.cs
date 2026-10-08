using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Models;
using System.Globalization;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Api
{
    /// <summary>
    /// Exercises NTDLS.Katzebase.Api against the test server over the network.
    /// </summary>
    public class TestApiClient(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        private static KbClient NewClient()
            => new("127.0.0.1", Constants.LISTEN_PORT, "admin", KbClient.HashPassword(""), "ApiTests");

        /// <summary>
        /// Creates (or recreates) an empty schema for a test, with an index on [Category].
        /// </summary>
        private static string CreateSchema(KbClient client, string name, bool withIndex = true)
        {
            var schema = $"TestData:Api:{name}";
            client.Schema.CreateRecursive("TestData:Api");
            client.Schema.DropIfExists(schema);
            client.Schema.Create(schema);
            if (withIndex)
            {
                client.Schema.Indexes.Create(schema, new KbIndex("ix_Category", ["Category"]));
            }
            return schema;
        }

        public enum Color { Red, Green, Blue }

        public class Item
        {
            public string? Code { get; set; }
            public string? Category { get; set; }
            public double Price { get; set; }
            public bool Active { get; set; }
            public int? Quantity { get; set; }
            public Color Color { get; set; }
            public string ReadOnly => "not mapped";
        }

        [Fact(DisplayName = "Server errors are rethrown as the same Katzebase exception type")]
        public void TypedExceptions()
        {
            using var client = NewClient();
            var schema = CreateSchema(client, "TypedExceptions", withIndex: false);
            client.Schema.Indexes.Create(schema, new KbIndex("uk_Code", ["Code"]) { IsUnique = true });
            client.Document.Store(schema, new { Code = "a" });

            Assert.Throws<KbDuplicateKeyViolationException>(() => client.Document.Store(schema, new { Code = "a" }));
            Assert.Throws<KbDuplicateKeyViolationException>(()
                => client.Query.ExecuteNonQuery($"INSERT INTO {schema}(Code) VALUES(@Code)", new { Code = "a" }));

            Assert.Throws<KbObjectNotFoundException>(() => client.Query.Fetch("SELECT * FROM TestData:Api:DoesNotExist"));

            var parserException = Assert.Throws<KbParserException>(() => client.Query.Fetch($"SELECT * FROM {schema}\r\nSELEKT * FROM {schema}"));
            Assert.Equal(2, parserException.LineNumber);

            //A timeout passed where parameters are expected is a mistake, not a parameter.
            Assert.Throws<KbInvalidArgumentException>(() => client.Query.ExplainPlan($"SELECT * FROM {schema}", (object)TimeSpan.FromSeconds(1)));

            //Not connected.
            using var disconnected = new KbClient();
            Assert.Throws<KbConnectionException>(() => disconnected.Query.Fetch($"SELECT * FROM {schema}"));
            Assert.Throws<KbConnectionException>(() => new KbClient("127.0.0.1", 1, "admin", KbClient.HashPassword("")));
        }

        [Fact(DisplayName = "Documents can be stored, read, replaced and deleted by id")]
        public void DocumentCrud()
        {
            using var client = NewClient();
            var schema = CreateSchema(client, "DocumentCrud");

            var id = client.Document.Store(schema, new { Code = "a", Category = "x" });
            Assert.NotEqual(0u, id);

            var document = client.Document.Get(schema, id);
            Assert.NotNull(document);
            Assert.Equal(id, document.Id);
            Assert.Equal("a", document.Deserialize<Item>()?.Code);
            Assert.Equal("x", client.Document.Get<Item>(schema, id)?.Category);

            Assert.Null(client.Document.Get(schema, 999_999));

            //Replace the whole document; the index must follow the new value.
            client.Document.Replace(schema, id, new { Code = "a2", Category = "y" });
            var replaced = client.Document.Get<Item>(schema, id);
            Assert.Equal("a2", replaced?.Code);
            Assert.Empty(client.Query.Fetch<Item>($"SELECT * FROM {schema} WHERE Category = 'x'"));
            Assert.Single(client.Query.Fetch<Item>($"SELECT * FROM {schema} WHERE Category = 'y'"));
            Assert.Throws<KbObjectNotFoundException>(() => client.Document.Replace(schema, 999_999, new { Code = "nope" }));

            //Documents can also be given as JSON.
            var jsonId = client.Document.Store(schema, "{\"Code\":\"json\",\"Category\":\"z\"}");
            Assert.Equal("json", client.Document.Get<Item>(schema, jsonId)?.Code);
            Assert.Throws<KbInvalidArgumentException>(() => client.Document.Store(schema, "[1,2,3]"));

            var batchIds = client.Document.StoreMany(schema, [new { Code = "b1" }, new { Code = "b2" }, new { Code = "b3" }]);
            Assert.Equal(3, batchIds.Count);
            Assert.Equal("b2", client.Document.Get<Item>(schema, batchIds[1])?.Code);

            Assert.True(client.Document.Delete(schema, id));
            Assert.False(client.Document.Delete(schema, id));
            Assert.Null(client.Document.Get(schema, id));
            Assert.Empty(client.Query.Fetch<Item>($"SELECT * FROM {schema} WHERE Category = 'y'"));

            Assert.Equal(2, client.Document.DeleteMany(schema, [batchIds[0], batchIds[2], 999_999]));
            Assert.Equal(2, client.Query.Fetch<Item>($"SELECT * FROM {schema}").Count);
        }

        [Fact(DisplayName = "Transaction scopes commit, roll back on dispose and never silently lose a commit")]
        public void Transactions()
        {
            using var client = NewClient();
            var schema = CreateSchema(client, "Transactions");

            using (client.Transaction.Begin())
            {
                client.Document.Store(schema, new { Code = "rolled-back" });
                //Disposed without committing.
            }
            Assert.Empty(client.Query.Fetch<Item>($"SELECT * FROM {schema}"));

            using (var transaction = client.Transaction.Begin())
            {
                client.Document.Store(schema, new { Code = "committed" });

                //Joining an open transaction: committed only when the outer scope commits.
                using (var inner = client.Transaction.Begin())
                {
                    client.Document.Store(schema, new { Code = "committed-inner" });
                    inner.Commit();
                }

                transaction.Commit();
                Assert.Throws<KbTransactionCancelledException>(() => transaction.Commit());
            }
            Assert.Equal(2, client.Query.Fetch<Item>($"SELECT * FROM {schema}").Count);

            //Committing when there is no open transaction (e.g. it was rolled back by the server) must fail.
            Assert.Throws<KbTransactionCancelledException>(() => client.Transaction.Commit());

            //Rolling back with no open transaction is harmless.
            client.Transaction.Rollback();
        }

        [Fact(DisplayName = "Parameters and results are culture invariant and map to typed objects")]
        public void ParametersAndMapping()
        {
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                //A culture with ',' as the decimal separator.
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                using var client = NewClient();
                var schema = CreateSchema(client, "ParametersAndMapping");

                client.Query.ExecuteNonQuery($"INSERT INTO {schema}(Code, Category, Price, Active, Quantity, Color) VALUES(@Code, @Category, @Price, @Active, @Quantity, @Color)",
                    new { Code = "a", Category = "x", Price = 1.5, Active = true, Quantity = (int?)null, Color = Color.Blue });
                client.Query.ExecuteNonQuery($"INSERT INTO {schema}(Code, Category, Price, Active, Quantity, Color) VALUES(@Code, @Category, @Price, @Active, @Quantity, @Color)",
                    new Dictionary<string, object?> { ["Code"] = "b", ["@Category"] = "x", ["Price"] = 2.25m, ["Active"] = false, ["Quantity"] = 7, ["Color"] = Color.Green });

                var items = client.Query.Fetch<Item>($"SELECT * FROM {schema} WHERE Price > @MinPrice ORDER BY Code", new { MinPrice = 1.25 });
                Assert.Equal(2, items.Count);

                Assert.Equal(1.5, items[0].Price);
                Assert.True(items[0].Active);
                Assert.Null(items[0].Quantity);
                Assert.Equal(Color.Blue, items[0].Color);

                Assert.Equal(2.25, items[1].Price);
                Assert.False(items[1].Active);
                Assert.Equal(7, items[1].Quantity);
                Assert.Equal(Color.Green, items[1].Color);

                Assert.Equal("b", client.Query.FetchSingle<Item>($"SELECT * FROM {schema} WHERE Code = @Code", new { Code = "b" }).Code);
                Assert.Null(client.Query.FetchSingleOrDefault<Item>($"SELECT * FROM {schema} WHERE Code = 'none'"));
                Assert.Throws<KbObjectNotFoundException>(() => client.Query.FetchFirst<Item>($"SELECT * FROM {schema} WHERE Code = 'none'"));
                Assert.Throws<KbProcessingException>(() => client.Query.FetchSingle<Item>($"SELECT * FROM {schema}"));
                Assert.Equal(2.25, client.Query.FetchScalar<double>($"SELECT Price FROM {schema} WHERE Code = 'b'"));
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [Fact(DisplayName = "Asynchronous methods work and honor cancellation")]
        public async Task AsyncMethods()
        {
            using var client = NewClient();
            var schema = CreateSchema(client, "AsyncMethods");

            var id = await client.Document.StoreAsync(schema, new { Code = "a", Category = "x" });
            Assert.Equal("a", (await client.Document.GetAsync<Item>(schema, id))?.Code);

            await using (var transaction = await client.Transaction.BeginAsync())
            {
                await client.Document.StoreManyAsync(schema, [new { Code = "b", Category = "x" }, new { Code = "c", Category = "y" }]);
                await transaction.CommitAsync();
            }

            var items = await client.Query.FetchAsync<Item>($"SELECT * FROM {schema} WHERE Category = @Category", new { Category = "x" });
            Assert.Equal(2, items.Count);

            Assert.Equal(3, await client.Query.FetchScalarAsync<int>($"SELECT Count(0) FROM {schema}"));

            Assert.Equal(1, await client.Document.DeleteManyAsync(schema, [id]));

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(()
                => client.Query.FetchAsync($"SELECT * FROM {schema}", cancellationToken: cancellation.Token));

            //The client is still usable after a cancelled request.
            Assert.Equal(2, (await client.Query.FetchAsync<Item>($"SELECT * FROM {schema}")).Count);
        }

        [Fact(DisplayName = "Schemas can be detached and attached through the API")]
        public void AttachDetach()
        {
            using var client = NewClient();
            var schema = CreateSchema(client, "AttachDetach");
            client.Document.Store(schema, new { Code = "a", Category = "x" });

            var folder = Path.Combine(Path.GetTempPath(), "KatzebaseApiTests", Guid.NewGuid().ToString("N"));
            try
            {
                client.Schema.Detach(schema, folder);
                Assert.False(client.Schema.Exists(schema));

                client.Schema.Attach(schema, folder);
                Assert.Single(client.Query.Fetch<Item>($"SELECT * FROM {schema} WHERE Category = 'x'"));

                Assert.Throws<KbInvalidArgumentException>(() => client.Schema.Attach("Bad Name; DROP SCHEMA x", folder));
            }
            finally
            {
                try { Directory.Delete(folder, true); } catch { }
            }
        }
    }
}
