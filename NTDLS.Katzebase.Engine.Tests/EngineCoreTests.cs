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
        public void TestFunction1()
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var actual = ephemeral.Transaction.ExecuteScalar<string?>("SelectFromSingle.kbs");
            Assert.Null(actual);
        }

        [Fact]
        public void TestFunction2()
        {
            int expected = 0;
            int actual = 1;

            Assert.Equal(expected, actual);
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
