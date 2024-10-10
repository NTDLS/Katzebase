using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Query.Specific;
using NTDLS.Katzebase.Parsers.Query.Specific.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.Validation;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Security.Cryptography;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers
{
    public class StaticQueryParser
    {
        /// <summary>
        /// Parse the query batch (a single query text containing multiple queries).
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="userParameters"></param>
        /// <returns></returns>
        static public QueryBatch ParseBatch(string queryText, KbInsensitiveDictionary<KbConstant> givenConstants, KbInsensitiveDictionary<KbConstant>? userParameters = null)
        {
            var constants = givenConstants.Clone(); //Clone because we do not want to modify the global constants collection.

            userParameters ??= new();
            //If we have user parameters, add them to a clone of the global tokenizer constants.
            foreach (var param in userParameters)
            {
                constants.Add(param.Key, param.Value);
            }

            //queryText = PreParseQueryVariableDeclarations(queryText, ref constants);

            var tokenizer = new Tokenizer(queryText, true, constants);
            var queryBatch = new QueryBatch(tokenizer.Literals);

            List<Exception> exceptions = new List<Exception>();

            while (!tokenizer.IsExhausted())
            {
                int preParseTokenPosition = tokenizer.Caret;

                try
                {
                    var preparedQuery = ParseQuery(queryBatch, tokenizer);

                    var singleQueryText = tokenizer.Substring(preParseTokenPosition, tokenizer.Caret - preParseTokenPosition);
                    preparedQuery.Hash = ComputeSHA256(singleQueryText);
                    queryBatch.Add(preparedQuery);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);

                    //For the sake of error reporting, try to find the next query and parse it too.
                    if (tokenizer.TryFindCompareNext((o) => StaticParserUtility.IsStartOfQuery(o), out var foundToken, out var startOfNextQueryCaret))
                    {
                        tokenizer.Caret = startOfNextQueryCaret.EnsureNotNull();
                    }
                    else
                    {
                        break;
                    }

                    continue;
                }
            }

            if (exceptions.Count != 0)
            {
                throw new AggregateException(exceptions);
            }

            return queryBatch;
        }

        /// <summary>
        /// Parse the single query in the batch.
        /// </summary>
        static public PreparedQuery ParseQuery(QueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.GetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                tokenizer.EatGetNext();
                throw new KbParserException(tokenizer.GetCurrentLineNumber(),
                    $"Expected  [{string.Join("],[", Enum.GetValues<QueryType>().Where(o => o != QueryType.None))}], found: [{token}].");
            }

            tokenizer.EatNext();

            var preparedQuery = queryType switch
            {
                QueryType.Select => StaticParserSelect.Parse(queryBatch, tokenizer),
                QueryType.Delete => StaticParserDelete.Parse(queryBatch, tokenizer),
                QueryType.Insert => StaticParserInsert.Parse(queryBatch, tokenizer),
                QueryType.Update => StaticParserUpdate.Parse(queryBatch, tokenizer),
                QueryType.Begin => StaticParserBegin.Parse(queryBatch, tokenizer),
                QueryType.Commit => StaticParserCommit.Parse(queryBatch, tokenizer),
                QueryType.Rollback => StaticParserRollback.Parse(queryBatch, tokenizer),
                QueryType.Create => StaticParserCreate.Parse(queryBatch, tokenizer),
                QueryType.Declare => StaticParserDeclare.Parse(queryBatch, tokenizer),
                QueryType.Drop => StaticParserDrop.Parse(queryBatch, tokenizer),

                QueryType.Sample => StaticParserSample.Parse(queryBatch, tokenizer),
                QueryType.Analyze => StaticParserAnalyze.Parse(queryBatch, tokenizer),
                QueryType.List => StaticParserList.Parse(queryBatch, tokenizer),
                QueryType.Alter => StaticParserAlter.Parse(queryBatch, tokenizer),
                QueryType.Rebuild => StaticParserRebuild.Parse(queryBatch, tokenizer),
                QueryType.Set => StaticParserSet.Parse(queryBatch, tokenizer),
                QueryType.Kill => StaticParserKill.Parse(queryBatch, tokenizer),
                QueryType.Exec => StaticParserExec.Parse(queryBatch, tokenizer),

                _ => throw new KbNotImplementedException($"Query type is not implemented: [{token}]."),
            };

            ValidateFieldSchemaReferences.Validate(tokenizer, preparedQuery);

            return preparedQuery;
        }

        /*
        /// <summary>
        /// Parse the variable declaration in the query and remove them from the query text.
        /// </summary>
        static string PreParseQueryVariableDeclarations(string queryText, ref KbInsensitiveDictionary<KbConstant> tokenizerConstants)
        {
            var lines = queryText.Split("\n").Select(s => s.Trim());
            lines = lines.Where(o => o.StartsWith("const", StringComparison.InvariantCultureIgnoreCase));

            int lineNumber = 1;

            foreach (var line in lines)
            {
                var lineTokenizer = new TokenizerSlim(line);

                if (!lineTokenizer.TryEatIsNextToken("const", out var token))
                {
                    throw new KbParserException(lineNumber, $"Expected [const], found: [{token}].");
                }

                if (lineTokenizer.NextCharacter != '@')
                {
                    throw new KbParserException(lineNumber, $"Expected [@], found: [{lineTokenizer.NextCharacter}].");
                }
                lineTokenizer.EatNextCharacter();

                if (lineTokenizer.TryEatValidateNextToken((o) => TokenizerExtensions.IsIdentifier(o), out var variableName) == false)
                {
                    throw new KbParserException(lineNumber, $"Expected constant variable name, found: [{token}].");
                }

                if (lineTokenizer.NextCharacter != '=')
                {
                    throw new KbParserException(lineNumber, $"Expected [=], found: [{lineTokenizer.NextCharacter}].");
                }
                lineTokenizer.EatNextCharacter();

                var variableValue = lineTokenizer.Remainder().Trim();

                KbBasicDataType variableType;
                if (variableValue.StartsWith('\'') && variableValue.EndsWith('\''))
                {
                    variableType = KbBasicDataType.String;
                    variableValue = variableValue[1..^1];
                }
                else
                {
                    variableType = KbBasicDataType.Numeric;
                    if (variableValue != null && double.TryParse(variableValue?.ToString(), out _) == false)
                    {
                        throw new KbParserException(lineNumber, $"Expected numeric value, found: [{variableName}].");
                    }
                }

                tokenizerConstants.Add($"@{variableName}", new KbConstant(variableValue, variableType));

                queryText = queryText.Replace(line, "\n");

                lineNumber++;
            }

            return queryText.TrimEnd() + "\n";
        }
        */

        public static string ComputeSHA256(string rawData)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

            var builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
