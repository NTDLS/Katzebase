using NTDLS.Katzebase.Api.Models;
using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.SupportingTypes;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using static NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations.Constants;

namespace NTDLS.Katzebase.Engine.Tests.QueryConventionBasedExpectations
{
    internal class QueryExpectation
    {
        public List<ExpectedDataset> ExpectedDatasets { get; set; } = new();
        public KbInsensitiveDictionary<QueryAttribute> BatchOptions { get; set; } = new();

        public string QueryText { get; set; } = string.Empty;

        /// <summary>
        /// Tests the actual data-set against the expectations.
        /// </summary>
        public static void ValidateScriptResults(EngineCore engine, string scriptFileName, object? userParameters = null)
        {
            var expectation = StaticQueryExpectationParser.Parse(scriptFileName);

            using var ephemeral = engine.Sessions.CreateEphemeralSystemSession();
            var resultsCollection = ephemeral.Transaction.ExecuteQuery(expectation.QueryText, userParameters);
            ephemeral.Commit();

            if (expectation.GetOption(BatchExpectationOption.DoNotValidate, false) == false)
            {
                expectation.Validate(resultsCollection);
            }
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

                if (expectedDataset.GetOption(DatasetExpectationOption.DoNotValidate, false))
                {
                    continue;
                }

                var actualDataset = actualDatasets.Collection[datasetIndex];
                if (expectedDataset.TryGetOption<int>(DatasetExpectationOption.MaxDuration, out var expectedMaxDuration))
                {
                    //Ensure the query ran in the within expected duration.
                    Assert.InRange(actualDataset.Duration, 0, expectedMaxDuration);
                }

                if (expectedDataset.TryGetOption<int>(DatasetExpectationOption.AffectedCount, out var expectedAffectedCount))
                {
                    //Ensure we have the expected "affected row count".
                    Assert.Equal(expectedAffectedCount, actualDataset.RowCount);
                }

                //Ensure that the field names, count and ordinals match.
                Assert.NotNull(expectedDataset.Fields);
                var actualDatasetFields = actualDataset.Fields.Select(f => f.Name);
                if (ValuesHash(expectedDataset.Fields) != ValuesHash(actualDatasetFields))
                {
                    throw new Exception("Dataset field names do not match.");
                }

                if (expectedDataset.GetOption(DatasetExpectationOption.EnforceRowOrder, false))
                {
                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDataset.Rows.Count, actualDataset.Rows.Count);

                    for (int rowOrdinal = 0; rowOrdinal < actualDataset.Rows.Count; rowOrdinal++)
                    {
                        IsExpectedRowMatch(expectedDataset, expectedDataset.Rows[datasetIndex],
                            actualDataset, actualDataset.Rows[rowOrdinal]);
                    }
                }
                else
                {
                    //Ensure that this result-set has the expected row count.
                    Assert.Equal(expectedDataset.Rows.Count, actualDataset.Rows.Count);

                    //We keep track of which rows have been matched because there may be duplicates and we only want to match each one once.
                    var matchedExpectationRows = new HashSet<ExpectedRow>();

                    for (int rowIndex = 0; rowIndex < actualDataset.Rows.Count; rowIndex++)
                    {
                        var foundMatch = FindMatchingExpectedRow(expectedDataset, actualDataset, actualDataset.Rows[rowIndex]);
                        if (foundMatch == null)
                        {
                            throw new Exception($"Expected row not found: [{string.Join("],[", actualDataset.Rows[rowIndex].Values)}]");
                        }

                        matchedExpectationRows.Add(foundMatch);
                    }
                }
            }
        }

        #region Pattern matching.

        private ExpectedRow? FindMatchingExpectedRow(ExpectedDataset expectedDataset, KbQueryResult actualResultSet, KbQueryRow actualRow)
        {
            foreach (var expectedRow in expectedDataset.Rows)
            {
                bool matchedAll = true;

                foreach (var field in actualResultSet.Fields)
                {
                    if (!IsExpectedRowMatch(expectedDataset, expectedRow, actualResultSet, actualRow))
                    {
                        matchedAll = false;
                        break;
                    }
                }

                if (matchedAll)
                {
                    return expectedRow;
                }
            }

            return null;
        }

        private bool IsExpectedRowMatch(ExpectedDataset expectedDataset, ExpectedRow expectedRow, KbQueryResult actualResultSet, KbQueryRow actualRow)
        {
            bool matchedAll = true;

            foreach (var field in actualResultSet.Fields)
            {
                var expectedValue = expectedRow.Values[expectedDataset.FieldIndex(field.Name)];
                var actualValue = actualResultSet.Value(actualRow, field.Name);

                if (expectedDataset.FieldPatterns.TryGetValue(field.Name, out var fieldPattern))
                {
                    if (PatternMatch(expectedValue, actualValue, fieldPattern) == false)
                    {
                        matchedAll = false;
                        break;
                    }
                }
                else
                {
                    if (actualValue != expectedValue)
                    {
                        matchedAll = false;
                        break;
                    }
                }
            }

            return matchedAll;
        }

        private bool PatternMatch(string? expectedValue, string? actualValue, FieldPattern fieldPattern)
        {
            switch (fieldPattern.PatternType)
            {
                case FieldPatternType.Null:
                    return actualValue == null;
                case FieldPatternType.Exact:
                    return expectedValue == actualValue;
                case FieldPatternType.Format:
                    return IsFormatMatch(actualValue, fieldPattern.Pattern);
                case FieldPatternType.Like:
                    return IsLikeMatch(actualValue, fieldPattern.Pattern);
                case FieldPatternType.NotNull:
                    return actualValue != null;
                case FieldPatternType.Numeric:
                    return double.TryParse(actualValue, out _);
                case FieldPatternType.Integer:
                    return int.TryParse(actualValue, out _);
                case FieldPatternType.DateTime:
                    return DateTime.TryParse(actualValue, out _);
                case FieldPatternType.Guid:
                    return Guid.TryParse(actualValue, out _);
            }

            return true;
        }

        public static bool IsLikeMatch(string? value, string pattern)
        {
            if (value == null)
            {
                return false;
            }
            ArgumentNullException.ThrowIfNull(pattern);

            var regex = new Regex("^" + Regex.Escape(pattern).Replace("%", ".*").Replace("_", ".") + "$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            return regex.IsMatch(value);
        }


        public static bool IsFormatMatch(string? value, string customPattern)
        {
            if (value == null)
            {
                return false;
            }

            string regex = "^" + customPattern
                                .Replace("n", @"\d")    // 'n' -> numeric digit (0-9)
                                .Replace("_", ".")      // '_' -> any single character
                                .Replace("c", "[^0-9]") // 'c' -> non-numeric character
                            + "$";

            return Regex.IsMatch(value, regex);
        }

        private static string ValuesHash(IEnumerable<string?> values)
        {
            //We bake the row-count into the hash so that it gets validated too.
            return $"[{string.Join("],[", values.Select(o => o ?? "<null>"))}]({values.Count()})";
        }

        #endregion

        #region Get Options.

        public bool IsOptionSet(BatchExpectationOption opt)
            => BatchOptions.TryGetValue(opt.ToString(), out var _);

        public bool TryGetOption<T>(BatchExpectationOption opt, out T outValue, T defaultValue)
        {
            if (BatchOptions.TryGetValue(opt.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = defaultValue;
            return false;
        }

        public bool TryGetOption<T>(BatchExpectationOption opt, [NotNullWhen(true)] out T? outValue)
        {
            if (BatchOptions.TryGetValue(opt.ToString(), out var option))
            {
                outValue = (T)option.Value;
                return true;
            }
            outValue = default;
            return false;
        }

        public T GetOption<T>(BatchExpectationOption opt, T defaultValue)
        {
            if (BatchOptions.TryGetValue(opt.ToString(), out var option))
            {
                return (T)option.Value;
            }
            return defaultValue;
        }

        #endregion
    }
}
