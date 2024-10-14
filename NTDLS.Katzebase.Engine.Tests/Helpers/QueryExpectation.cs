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
        /// Tests the actual data-set against the expectations.
        /// </summary>
        public void Validate(KbQueryResultCollection actualDatasets)
        {
            //Ensure we have the same number of result-sets.
            Assert.Equal(ExpectedDatasets.Count, actualDatasets.Collection.Count);

            if (GetAttribute(ExpectationAttribute.EnforceRowOrder, false))
            {
                //Not yet implemented.
                throw new NotImplementedException("EnforceRowOrder is not implemented.");
            }
            else
            {
                for (int i = 0; i < ExpectedDatasets.Count; i++)
                {
                    var expectedDataset = ExpectedDatasets[i];
                    var actualDataset = actualDatasets.Collection[i].Rows;

                    //Ensure that this result-set has the same row count.
                    Assert.Equal(expectedDataset.Count, actualDataset.Count);

                    //TODO: Validate rows.
                }
            }
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
