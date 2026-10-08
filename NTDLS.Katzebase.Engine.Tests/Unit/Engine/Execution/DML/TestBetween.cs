using NTDLS.Katzebase.Api.Exceptions;

namespace NTDLS.Katzebase.Engine.Tests.Unit.Engine.Execution.DML
{
    /// <summary>
    /// BETWEEN and NOT BETWEEN in WHERE clauses and join conditions, with and without indexes.
    /// </summary>
    public class TestBetween(EngineCoreFixture fixture) : IClassFixture<EngineCoreFixture>
    {
        private readonly EngineCore _engine = fixture.Engine;

        private const string Numbers = "TestData:Between:Numbers";
        private const string IndexedNumbers = "TestData:Between:IndexedNumbers";
        private const string Ranges = "TestData:Between:Ranges";

        private static readonly object _setupLock = new();
        private static bool _isSetup;

        private void Execute(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            ephemeral.Transaction.ExecuteNonQuery(queryText);
            ephemeral.Commit();
        }

        /// <summary>
        /// Returns the values of the first field of the last result set, sorted.
        /// </summary>
        private List<string> Values(string queryText)
        {
            using var ephemeral = _engine.Sessions.CreateEphemeralSystemSession();
            var results = ephemeral.Transaction.ExecuteQuery(queryText);
            ephemeral.Commit();
            return results.Collection.Last().Rows.Select(o => o.Values[0] ?? "<null>").Order(StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Runs a query that is expected to fail, unwrapping the AggregateException that the engine's worker threads produce.
        /// </summary>
        private void AssertQueryThrows<T>(string queryText) where T : Exception
        {
            var exception = Assert.ThrowsAny<Exception>(() => Values(queryText));
            if (exception is AggregateException aggregate)
            {
                //Each worker thread that evaluated a document reports the same error.
                Assert.All(aggregate.Flatten().InnerExceptions, inner => Assert.IsType<T>(inner));
            }
            else
            {
                Assert.IsType<T>(exception);
            }
        }

        private static List<string> Expected(params object[] values)
            => values.Select(o => o.ToString()!).Order(StringComparer.Ordinal).ToList();

        /// <summary>
        /// Ids 1 through 10 with Name = 'n{Id}' and Value = Id / 2, plus one document without an Id or Value.
        /// The indexed copy has an index on Id. Ranges has two ranges of Ids for the join tests.
        /// </summary>
        private void Setup()
        {
            lock (_setupLock)
            {
                if (_isSetup)
                {
                    return;
                }

                Execute("CREATE SCHEMA TestData:Between");
                foreach (var schema in new[] { Numbers, IndexedNumbers })
                {
                    Execute($"DROP SCHEMA {schema}");
                    Execute($"CREATE SCHEMA {schema}");
                    Execute($"INSERT INTO {schema} (Id, Name, Value) VALUES "
                        + string.Join(", ", Enumerable.Range(1, 10).Select(i => $"({i}, 'n{i}', {i} / 2)")));
                    Execute($"INSERT INTO {schema} (Name: 'no-id')");
                }
                Execute($"CREATE INDEX IX_Id (Id) ON {IndexedNumbers}");

                Execute($"DROP SCHEMA {Ranges}");
                Execute($"CREATE SCHEMA {Ranges}");
                Execute($"INSERT INTO {Ranges} (Name, Low, High) VALUES ('low', 1, 3), ('high', 8, 10)");

                _isSetup = true;
            }
        }

        [Theory(DisplayName = "BETWEEN includes the low and high values")]
        [InlineData(Numbers)]
        [InlineData(IndexedNumbers)]
        public void Between(string schema)
        {
            Setup();
            Assert.Equal(Expected(3, 4, 5, 6), Values($"SELECT Id FROM {schema} WHERE Id BETWEEN 3 AND 6"));
            Assert.Equal(Expected(5), Values($"SELECT Id FROM {schema} WHERE Id BETWEEN 5 AND 5"));
            Assert.Empty(Values($"SELECT Id FROM {schema} WHERE Id BETWEEN 6 AND 3"));
        }

        [Theory(DisplayName = "NOT BETWEEN excludes the range and documents without the field")]
        [InlineData(Numbers)]
        [InlineData(IndexedNumbers)]
        public void NotBetween(string schema)
        {
            Setup();
            Assert.Equal(Expected(1, 2, 7, 8, 9, 10), Values($"SELECT Id FROM {schema} WHERE Id NOT BETWEEN 3 AND 6"));
            Assert.Equal(Expected("n1", "n2", "n7", "n8", "n9", "n10"), Values($"SELECT Name FROM {schema} WHERE Id NOT BETWEEN 3 AND 6"));
        }

        [Theory(DisplayName = "BETWEEN combines with AND, OR and parentheses")]
        [InlineData(Numbers)]
        [InlineData(IndexedNumbers)]
        public void Connectors(string schema)
        {
            Setup();
            Assert.Equal(Expected(3, 5, 6), Values($"SELECT Id FROM {schema} WHERE Id BETWEEN 3 AND 6 AND Id != 4"));
            Assert.Equal(Expected(3, 5, 6), Values($"SELECT Id FROM {schema} WHERE Id != 4 AND Id BETWEEN 3 AND 6"));
            Assert.Equal(Expected(1, 2, 9, 10), Values($"SELECT Id FROM {schema} WHERE Id BETWEEN 1 AND 2 OR Id BETWEEN 9 AND 10"));
            Assert.Equal(Expected(2, 9), Values($"SELECT Id FROM {schema} WHERE (Id BETWEEN 1 AND 2 OR Id NOT BETWEEN 1 AND 8) AND (Name = 'n2' OR Name = 'n9')"));
            Assert.Equal(Expected(1, 10), Values($"SELECT Id FROM {schema} WHERE Name = 'n1' OR (Id NOT BETWEEN 2 AND 9 AND Name != 'n1')"));
        }

        [Fact(DisplayName = "BETWEEN keywords are not case-sensitive and may span lines")]
        public void Formatting()
        {
            Setup();
            Assert.Equal(Expected(3, 4, 5, 6), Values($"SELECT Id FROM {Numbers} WHERE Id between 3 and 6"));
            Assert.Equal(Expected(3, 4, 5, 6), Values($"SELECT Id FROM {Numbers}\r\nWHERE\r\n\tId BETWEEN 3\r\n\tAND 6"));
            Assert.Equal(Expected(1, 2, 7, 8, 9, 10), Values($"SELECT Id FROM {Numbers} WHERE Id not between 3 and 6"));
        }

        [Fact(DisplayName = "BETWEEN values can be expressions, functions, variables and decimals")]
        public void Expressions()
        {
            Setup();
            Assert.Equal(Expected(2, 3, 4), Values($"SELECT Id FROM {Numbers} WHERE Id BETWEEN (1 + 1) AND (2 * 2)"));
            Assert.Equal(Expected(3, 4, 5, 6), Values($"DECLARE @Low = 2\r\nSELECT Id FROM {Numbers} WHERE Id BETWEEN @Low + 1 AND Length('abcdef')"));
            Assert.Equal(Expected(3, 4, 5), Values($"SELECT Id FROM {Numbers} WHERE Value BETWEEN 1.5 AND 2.5"));
            Assert.Equal(Expected(4, 5, 6), Values($"SELECT Id FROM {Numbers} WHERE Id + 1 BETWEEN 5 AND 7"));
        }

        [Theory(DisplayName = "BETWEEN works in join conditions")]
        [InlineData(Numbers)]
        [InlineData(IndexedNumbers)]
        public void Join(string schema)
        {
            Setup();
            Assert.Equal(Expected("high:10", "high:8", "high:9", "low:1", "low:2", "low:3"),
                Values($"SELECT r.Name + ':' + n.Id FROM {Ranges} as r INNER JOIN {schema} as n ON n.Id BETWEEN r.Low AND r.High"));

            //Each range matches the seven Ids outside of it.
            Assert.Equal(14, Values($"SELECT n.Id FROM {Ranges} as r INNER JOIN {schema} as n ON n.Id NOT BETWEEN r.Low AND r.High").Count);
        }

        [Fact(DisplayName = "IsBetween and IsNotBetween accept decimals")]
        public void ScalarFunctions()
        {
            Setup();
            Assert.Equal(Expected(1), Values("SELECT IsBetween(2.5, 2, 3) FROM Single"));
            Assert.Equal(Expected(0), Values("SELECT IsNotBetween(2.5, 2, 3) FROM Single"));
        }

        [Fact(DisplayName = "Malformed BETWEEN conditions are rejected")]
        public void Errors()
        {
            Setup();
            AssertQueryThrows<KbParserException>($"SELECT Id FROM {Numbers} WHERE Id BETWEEN 3");
            AssertQueryThrows<KbParserException>($"SELECT Id FROM {Numbers} WHERE Id BETWEEN '3:6'");
            AssertQueryThrows<KbParserException>($"SELECT Id FROM {Numbers} WHERE Id NOT EQUALS 3");
            AssertQueryThrows<KbProcessingException>($"SELECT Id FROM {Numbers} WHERE Name BETWEEN 1 AND 2");
        }
    }
}
