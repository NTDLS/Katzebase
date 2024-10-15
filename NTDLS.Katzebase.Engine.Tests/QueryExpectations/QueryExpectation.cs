using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Parsers.Query.Specific;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Tests.Helpers.Constants;

namespace NTDLS.Katzebase.Engine.Tests.Helpers
{

    internal class QueryExpectation
    {
        public List<ExpectedDataset> ExpectedDatasets { get; set; } = new();

        public string QueryText { get; set; } = string.Empty;

        /// <summary>
        /// Tests the actual data-set against the expectations.
        /// </summary>
        public static void ValidateScriptResults(EngineCore engine, string scriptFileName, object? userParameters = null)
        {
            var expectation = Parse(scriptFileName);

            using var ephemeral = engine.Sessions.CreateEphemeralSystemSession();
            var resultsCollection = ephemeral.Transaction.ExecuteQuery(expectation.QueryText, userParameters);
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
            var result = new QueryExpectation();

            var queryText = EmbeddedScripts.Load(scriptName);

            int firstExpectedIndex = queryText.IndexOf("#Expected", StringComparison.InvariantCultureIgnoreCase);
            if (firstExpectedIndex < 0)
            {
                result.QueryText = queryText; //We have no expected datasets.
            }
            else
            {
                result.QueryText = queryText.Substring(0, firstExpectedIndex);
                string expectedDatasetTextBlock = queryText.Substring(firstExpectedIndex);

                var tokenizer = new Tokenizer(expectedDatasetTextBlock, [' ', '(', ')', ',', '='])
                {
                    SkipDelimiter = false
                };

                while (!tokenizer.IsExhausted())
                {
                    var expectedDataset = new ExpectedDataset();

                    //Find the next expected data-set definition.
                    int expectedIndex = tokenizer.GetNextIndexOf("#Expected");
                    tokenizer.SetCaret(expectedIndex + "#Expected".Length);

                    var validOptions = new ExpectedQueryAttributes
                    {
                        { ExpectationAttribute.EnforceRowOrder.ToString(), typeof(bool) },
                        { ExpectationAttribute.HasFieldNames.ToString(), typeof(bool) },
                        { ExpectationAttribute.AffectedCount.ToString(), typeof(int) },
                        { ExpectationAttribute.MaxDuration.ToString(), typeof(int) }
                    };
                    expectedDataset.Options = StaticParserAttributes.Parse(tokenizer, validOptions);

                    tokenizer.IsNext('{');

                    var expectedRowsTextBlock = tokenizer.EatGetMatchingScope('{', '}').Replace("\r\n", "\n");
                    var expectedRowLines = expectedRowsTextBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var expectedRowLine in expectedRowLines)
                    {
                        if (expectedDataset.Fields == null && expectedDataset.GetAttribute(ExpectationAttribute.HasFieldNames, false))
                        {
                            expectedDataset.Fields = expectedRowLine.Split('\t').ToList();
                            continue;
                        }

                        expectedDataset.Rows.Add(new ExpectedRow()
                        {
                            Values = expectedRowLine.Split('\t').ToList()
                        });
                    }

                    result.ExpectedDatasets.Add(expectedDataset);
                }
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

            for (int datasetIndex = 0; datasetIndex < actualDatasets.Collection.Count; datasetIndex++)
            {
                var expectedDataset = ExpectedDatasets[datasetIndex];
                var actualDataset = actualDatasets.Collection[datasetIndex];

                if (expectedDataset.TryGetAttribute<int>(ExpectationAttribute.MaxDuration, out var expectedMaxDuration))
                {
                    //Ensure the query ran in the within expected duration.
                    Assert.InRange(actualDataset.Duration, 0, expectedMaxDuration);
                }

                if (expectedDataset.TryGetAttribute<int>(ExpectationAttribute.AffectedCount, out var expectedAffectedCount))
                {
                    //Ensure we have the expected "affected row count".
                    Assert.Equal(expectedAffectedCount, actualDataset.RowCount);
                }

                if (expectedDataset.GetAttribute(ExpectationAttribute.EnforceRowOrder, false))
                {
                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDataset.Rows.Count, actualDataset.Rows.Count);

                    if (expectedDataset.GetAttribute<bool>(ExpectationAttribute.HasFieldNames, false))
                    {
                        //Ensure that the field names, count and ordinals match.

                        Assert.NotNull(expectedDataset.Fields);
                        var actualDatasetFields = actualDataset.Fields.Select(f => f.Name);
                        Assert.Equal(ValuesHash(expectedDataset.Fields), ValuesHash(actualDatasetFields));
                    }

                    for (int rowOrdinal = 0; rowOrdinal < actualDataset.Rows.Count; rowOrdinal++)
                    {
                        //Ensure that the row at the same index matches the row expectation.
                        Assert.Equal(ValuesHash(expectedDataset.Rows[datasetIndex].Values),
                            ValuesHash(actualDataset.Rows[rowOrdinal].Values));
                    }
                }
                else
                {
                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDataset.Rows.Count, actualDataset.Rows.Count);

                    if (expectedDataset.GetAttribute<bool>(ExpectationAttribute.HasFieldNames, false))
                    {
                        //Ensure that the field names, count and ordinals match.

                        Assert.NotNull(expectedDataset.Fields);
                        var actualDatasetFields = actualDataset.Fields.Select(f => f.Name);
                        Assert.Equal(ValuesHash(expectedDataset.Fields), ValuesHash(actualDatasetFields));
                    }

                    //We keep track of which rows have been matched because there may be duplicates and we only want to match each one once.
                    var matchedExpectationRows = new HashSet<ExpectedRow>();

                    for (int rowIndex = 0; rowIndex < actualDataset.Rows.Count; rowIndex++)
                    {
                        var actualDatasetValuesHash = ValuesHash(actualDataset.Rows[rowIndex].Values);

                        //Find the actual row in the expected dataset, omitting rows we have already matched.
                        var matchedExpectation = expectedDataset.Rows.FirstOrDefault(o =>
                            !matchedExpectationRows.Contains(o) && ValuesHash(o.Values) == actualDatasetValuesHash);

                        Assert.NotNull(matchedExpectation);
                        matchedExpectationRows.Add(matchedExpectation);
                    }
                }
            }
        }

        private static string ValuesHash(IEnumerable<string?> values)
        {
            //We bake the row-count into the hash so that it gets validated too.
            string valuesHash = $"[{string.Join("],[", values.Select(o => o ?? "<null>"))}]({values.Count()})";
            return valuesHash;
            //return Shared.Helpers.GetSHA256Hash(valuesHash);
        }
    }
}
