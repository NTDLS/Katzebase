using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Parsers.Query.Specific;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Diagnostics.CodeAnalysis;
using static NTDLS.Katzebase.Engine.Tests.Helpers.Constants;

namespace NTDLS.Katzebase.Engine.Tests.Helpers
{
    internal class QueryExpectation
    {
        public List<string>? FieldNames { get; set; }
        public List<List<ExpectedRow>> ExpectedDatasets { get; set; } = new();
        public string QueryText { get; set; } = string.Empty;

        public KbInsensitiveDictionary<QueryAttribute> Options = new();

        /// <summary>
        /// Tests the actual data-set against the expectations.
        /// </summary>
        public static void ValidateScriptResults(EngineCore engine, string scriptFileName)
        {
            var expectation = Parse(scriptFileName);

            using var ephemeral = engine.Sessions.CreateEphemeralSystemSession();
            var resultsCollection = ephemeral.Transaction.ExecuteQuery(expectation.QueryText);
            ephemeral.Commit();

            expectation.Validate(resultsCollection);
        }

        /// <summary>
        /// Finds a script in the project and parsed an "expected results" section from it.
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public static QueryExpectation Parse(string scriptName)
        {
            var result = new QueryExpectation()
            {
                QueryText = EmbeddedScripts.Load("NumberOfOrdersPlacedByEachPerson.kbs")
            };

            var tokenizer = new Tokenizer(result.QueryText, [' ', '(', ')', ',', '='])
            {
                SkipDelimiter = false
            };

            int expectedIndex = tokenizer.GetNextIndexOf("Expected(");

            tokenizer.SetCaret(expectedIndex + "Expected".Length);

            var validOptions = new ExpectedQueryAttributes
                {
                    { ExpectationAttribute.EnforceRowOrder.ToString(), typeof(bool) },
                    { ExpectationAttribute.HasFieldNames.ToString(), typeof(bool) }
                };
            result.Options = StaticParserAttributes.Parse(tokenizer, validOptions);

            while (tokenizer.TryIsNext('{'))
            {
                var expectedDataset = new List<ExpectedRow>();

                bool isFirstRow = true;

                var expectedBlock = tokenizer.EatGetMatchingScope('{', '}').Replace("\r\n", "\n");
                var expectedRowLines = expectedBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var expectedRowLine in expectedRowLines)
                {
                    if (isFirstRow)
                    {
                        isFirstRow = false;
                        if (result.GetAttribute(ExpectationAttribute.HasFieldNames, false) == true)
                        {
                            result.FieldNames = expectedRowLine.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                            continue;
                        }
                    }

                    expectedDataset.Add(new ExpectedRow()
                    {
                        Values = expectedRowLine.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                    });
                }

                result.ExpectedDatasets.Add(expectedDataset);
            }

            return result;
        }

        /// <summary>
        /// Loops through all data-sets, their rows, and fields, ensuring that all of them are in the expectations.
        /// Also validates result-set count, row-counts and field-counts.
        /// </summary>
        public void Validate(KbQueryResultCollection actualDatasets)
        {
            //Ensure we have the expected result-set count.
            Assert.Equal(ExpectedDatasets.Count, actualDatasets.Collection.Count);

            if (GetAttribute(ExpectationAttribute.EnforceRowOrder, false))
            {
                for (int datasetOrdinal = 0; datasetOrdinal < actualDatasets.Collection.Count; datasetOrdinal++)
                {
                    var actualDatasetRows = actualDatasets.Collection[datasetOrdinal].Rows;
                    var expectedDatasetRows = ExpectedDatasets[datasetOrdinal];

                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDatasetRows.Count, actualDatasetRows.Count);

                    for (int rowOrdinal = 0; rowOrdinal < actualDatasetRows.Count; rowOrdinal++)
                    {
                        var actualDatasetRow = actualDatasets.Collection[datasetOrdinal].Rows[rowOrdinal];
                        var expectedDatasetRow = expectedDatasetRows[datasetOrdinal];

                        //Ensure that the row at the same index matches the row expectation.
                        Assert.Equal(ValuesHash(expectedDatasetRow.Values), ValuesHash(actualDatasetRow.Values));
                    }
                }
            }
            else
            {
                for (int datasetOrdinal = 0; datasetOrdinal < actualDatasets.Collection.Count; datasetOrdinal++)
                {
                    var actualDatasetRows = actualDatasets.Collection[datasetOrdinal].Rows;
                    var expectedDatasetRows = ExpectedDatasets[datasetOrdinal];

                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDatasetRows.Count, actualDatasetRows.Count);

                    var matchedExpectationRows = new HashSet<ExpectedRow>();

                    for (int rowOrdinal = 0; rowOrdinal < actualDatasetRows.Count; rowOrdinal++)
                    {
                        var actualDatasetRow = actualDatasets.Collection[datasetOrdinal].Rows[rowOrdinal];

                        var actualDatasetValuesHash = ValuesHash(actualDatasetRow.Values);

                        //Find the actual row in the expected dataset, omitting rows we have already matched.
                        var matchedExpectation = expectedDatasetRows.FirstOrDefault(o =>
                            matchedExpectationRows.Contains(o) == false && ValuesHash(o.Values) == actualDatasetValuesHash);

                        Assert.NotNull(matchedExpectation);
                        matchedExpectationRows.Add(matchedExpectation);
                    }
                }
            }
        }

        private static string ValuesHash(IEnumerable<string?> values)
        {
            //We bake the row-count into the hash so that it gets validated too.
            string valuesHash = $"[{string.Join("],[", values.Select(o => o ?? "_$NULL$_"))}]({values.Count()})";
            return Shared.Helpers.GetSHA256Hash(valuesHash);
        }

        #region Get Attributes.

        public bool IsAttributeSet(ExpectationAttribute attribute)
            => Options.TryGetValue(attribute.ToString(), out var _);

        public bool TryGetAttribute<T>(ExpectationAttribute attribute, out T outValue, T defaultValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = defaultValue;
            return false;
        }

        public bool TryGetAttribute<T>(ExpectationAttribute attribute, [NotNullWhen(true)] out T? outValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = default;
            return false;
        }

        public T GetAttribute<T>(ExpectationAttribute attribute, T defaultValue)
        {
            if (Options.TryGetValue(attribute.ToString(), out var option))
            {
                return (T)option.Value;
            }
            return defaultValue;
        }

        #endregion
    }
}
