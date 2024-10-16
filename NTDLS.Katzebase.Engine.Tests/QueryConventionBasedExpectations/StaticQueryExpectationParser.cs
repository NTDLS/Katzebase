using NTDLS.Helpers;
using NTDLS.Katzebase.Engine.Scripts;
using NTDLS.Katzebase.Parsers.Query.Specific;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations.Constants;

namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    class StaticQueryExpectationParser
    {
        /// <summary>
        /// Finds a script in the project and parsed an "expected results" section from it.
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public static QueryExpectation Parse(string scriptName)
        {
            var result = new QueryExpectation();

            var queryText = EmbeddedScripts.Load(scriptName);

            int indexOfOptions = queryText.IndexOf("#BatchOptions", StringComparison.InvariantCultureIgnoreCase);
            int indexOfFirstExpectations = queryText.IndexOf("#Expected", StringComparison.InvariantCultureIgnoreCase);
            int startIndex = GetSmallestPositive(indexOfOptions, indexOfFirstExpectations);

            if (startIndex < 0)
            {
                result.QueryText = queryText; //We have no expected datasets.
            }
            else
            {
                result.QueryText = queryText.Substring(0, startIndex);
                string expectedDatasetTextBlock = queryText.Substring(startIndex);

                var tokenizer = new Tokenizer(expectedDatasetTextBlock, [' ', '\t', '(', ')', ',', '='])
                {
                    EatDelimiters = false
                };

                if (tokenizer.TryEatIfNext("#BatchOptions"))
                {
                    var validBatchOptions = new ExpectedQueryAttributes
                    {
                        { BatchExpectationOption.DoNotValidate.ToString(), typeof(bool) }
                    };
                    result.BatchOptions = StaticParserAttributes.Parse(tokenizer, validBatchOptions);
                }

                while (!tokenizer.IsExhausted())
                {
                    var expectedDataset = new ExpectedDataset();

                    //Find the next expected data-set definition.
                    int expectedIndex = tokenizer.GetNextIndexOf("#Expected");
                    tokenizer.SetCaret(expectedIndex + "#Expected".Length);

                    var validOptions = new ExpectedQueryAttributes
                    {
                        { DatasetExpectationOption.EnforceRowOrder.ToString(), typeof(bool) },
                        { DatasetExpectationOption.AffectedCount.ToString(), typeof(int) },
                        { DatasetExpectationOption.MaxDuration.ToString(), typeof(int) }
                    };
                    expectedDataset.Options = StaticParserAttributes.Parse(tokenizer, validOptions);

                    tokenizer.IsNext('{');

                    var datasetExpectationTextBlock = tokenizer.EatGetMatchingScope('{', '}').Replace("\r\n", "\n");

                    var expectationBlock = new Tokenizer(datasetExpectationTextBlock);
                    if (expectationBlock.TryEatIfNext("#FieldPatterns"))
                    {
                        var fieldPatternLines = expectationBlock.EatGetMatchingScope('{', '}').Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var fieldPatternLine in fieldPatternLines)
                        {
                            var fieldLine = new Tokenizer(fieldPatternLine, [' ', '\t', '(', ')', ',', '='])
                            {
                                EatDelimiters = false
                            };

                            var fieldName = fieldLine.EatGetNext();
                            fieldLine.EatIfNext('=');
                            var patternType = fieldLine.EatIfNextEnum<FieldPatternType>();
                            fieldLine.IsNext('(');
                            var pattern = fieldLine.EatGetMatchingScope();

                            expectedDataset.FieldPatterns.Add(fieldName, new FieldPattern(patternType, pattern));
                        }
                    }

                    var expectedRowLines = expectationBlock.Remainder().Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var expectedRowLine in expectedRowLines)
                    {
                        if (expectedDataset.Fields.Count == 0)
                        {
                            //Parse the field names from the first row.
                            expectedDataset.Fields = expectedRowLine.Split('\t').ToList();
                            continue;
                        }

                        expectedDataset.Rows.Add(new ExpectedRow()
                        {
                            Values = expectedRowLine.Split('\t').Select(o => o.Is("<null>") ? null : o).ToList()
                        });
                    }

                    result.ExpectedDatasets.Add(expectedDataset);
                }
            }

            return result;
        }

        private static int GetSmallestPositive(int a, int b)
        {
            // Check if both numbers are greater than 0
            if (a > 0 && b > 0)
            {
                return Math.Min(a, b);
            }
            // If only one number is positive, return that one
            else if (a > 0)
            {
                return a;
            }
            else if (b > 0)
            {
                return b;
            }
            // If neither is positive, return 0
            return 0;
        }
    }
}
