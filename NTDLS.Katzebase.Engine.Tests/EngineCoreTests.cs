namespace NTDLS.Katzebase.Engine.Tests
{
    public class EngineCoreTests : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine;

        public EngineCoreTests(EngineCoreFixture fixture)
        {
            _engine = fixture.Engine;
        }

        [Fact]
        public void SelectFromSingle()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var actual = ephemeral.Transaction.ExecuteScalar<string?>("SELECT Value FROM Single");
            Assert.Null(actual);
        }

        [Fact]
        public void ConstantsAndSimpleExpressions()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var resultsCollection = ephemeral.Transaction.ExecuteQuery("ConstantsAndSimpleExpressions.kbs");

            Assert.NotNull(resultsCollection);
            Assert.NotNull(resultsCollection.Collection);

            var firstResult = resultsCollection.Collection.Single();

            Assert.Equal("10", firstResult.Value(0, "Element1"));
            Assert.Equal("20", firstResult.Value(0, "Element2"));
            Assert.Equal("Text", firstResult.Value(0, "Element3"));
            Assert.Equal("Hello World", firstResult.Value(0, "Element4"));
            Assert.Equal("Text20", firstResult.Value(0, "Element5"));
            Assert.Equal("20Text", firstResult.Value(0, "Element6"));
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
