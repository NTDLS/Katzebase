using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Parsers.Interfaces;
using NTDLS.Katzebase.Parsers.Query.Class;
using NTDLS.Katzebase.Parsers.Query.Class.Helpers;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Tokens;
using System.Security.Cryptography;
using System.Text;
using static NTDLS.Katzebase.Client.KbConstants;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers
{
    public class StaticQueryParser<TData> where TData : IStringable
    {
        /// <summary>
        /// Parse the query batch (a single query text containing multiple queries).
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="userParameters"></param>
        /// <returns></returns>
        static public QueryBatch<TData> ParseBatch(IEngineCore core, string queryText, Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc, KbInsensitiveDictionary<KbConstant>? userParameters = null)
        {
            var tokenizerConstants = core.GlobalConstants.Clone();

            userParameters ??= new();
            //If we have user parameters, add them to a clone of the global tokenizer constants.
            foreach (var param in userParameters)
            {
                tokenizerConstants.Add(param.Key, param.Value);
            }

            queryText = PreParseQueryVariableDeclarations(queryText, ref tokenizerConstants);

            var tokenizer = new Tokenizer<TData>(queryText, parseStringToDoc, true, tokenizerConstants);
            var queryBatch = new QueryBatch<TData>(tokenizer.Literals, parseStringToDoc);

            while (!tokenizer.IsExhausted())
            {
                int preParseTokenPosition = tokenizer.Caret;
                var preparedQuery = ParseQuery(queryBatch, tokenizer, parseStringToDoc, castStringToDoc);

                var singleQueryText = tokenizer.Substring(preParseTokenPosition, tokenizer.Caret - preParseTokenPosition);
                preparedQuery.Hash = ComputeSHA256(singleQueryText);

                queryBatch.Add(preparedQuery);
            }

            return queryBatch;
        }

        /// <summary>
        /// Parse the single query in the batch.
        /// </summary>
        static public PreparedQuery<TData> ParseQuery(QueryBatch<TData> queryBatch, Tokenizer<TData> tokenizer
            , Func<string, TData> parseStringToDoc, Func<string, TData> castStringToDoc)
        {
            string token = tokenizer.GetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"Found [{token}], expected: [{string.Join("],[", Enum.GetValues<QueryType>().Where(o => o != QueryType.None))}].");
            }

            tokenizer.EatNext();

            return queryType switch
            {
                QueryType.Select => StaticParserSelect<TData>.Parse(queryBatch, tokenizer, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc),
                QueryType.Delete => StaticParserDelete<TData>.Parse(queryBatch, tokenizer, parseStringToDoc, castStringToDoc),
                QueryType.Insert => StaticParserInsert<TData>.Parse(queryBatch, tokenizer, parseStringToDoc: parseStringToDoc, castStringToDoc: castStringToDoc),
                QueryType.Update => StaticParserUpdate<TData>.Parse(queryBatch, tokenizer),
                QueryType.Begin => StaticParserBegin<TData>.Parse(queryBatch, tokenizer),
                QueryType.Commit => StaticParserCommit<TData>.Parse(queryBatch, tokenizer),
                QueryType.Rollback => StaticParserRollback<TData>.Parse(queryBatch, tokenizer),
                QueryType.Create => StaticParserCreate<TData>.Parse(queryBatch, tokenizer),
                QueryType.Drop => StaticParserDrop<TData>.Parse(queryBatch, tokenizer),

                QueryType.Sample => StaticParserSample<TData>.Parse(queryBatch, tokenizer),
                QueryType.Analyze => StaticParserAnalyze<TData>.Parse(queryBatch, tokenizer),
                QueryType.List => StaticParserList<TData>.Parse(queryBatch, tokenizer),
                QueryType.Alter => StaticParserAlter<TData>.Parse(queryBatch, tokenizer),
                QueryType.Rebuild => StaticParserRebuild<TData>.Parse(queryBatch, tokenizer),
                QueryType.Set => StaticParserSet.Parse<TData>(queryBatch, tokenizer),
                QueryType.Kill => StaticParserKill<TData>.Parse(queryBatch, tokenizer),
                QueryType.Exec => StaticParserExec<TData>.Parse(queryBatch, tokenizer, parseStringToDoc, castStringToDoc),

                _ => throw new KbParserException(tokenizer.GetCurrentLineNumber(), $"The query type is not implemented: [{token}]."),
            };
        }

        /// <summary>
        /// Parse the variable declaration in the query and remove them from the query text.
        /// </summary>
        static string PreParseQueryVariableDeclarations(string queryText, ref KbInsensitiveDictionary<KbConstant> tokenizerConstants)
        {
            var lines = queryText.Split("\n").Select(s => s.Trim());
            lines = lines.Where(o => o.StartsWith("const", StringComparison.InvariantCultureIgnoreCase));

            foreach (var line in lines)
            {
                var lineTokenizer = new TokenizerSlim(line);

                if (!lineTokenizer.TryEatIsNextToken("const", out var token))
                {
                    throw new KbParserException($"Invalid query. Found [{token}], expected: [const].");
                }

                if (lineTokenizer.NextCharacter != '@')
                {
                    throw new KbParserException($"Invalid query. Found [{lineTokenizer.NextCharacter}], expected: [@].");
                }
                lineTokenizer.EatNextCharacter();

                if (lineTokenizer.TryEatValidateNextToken((o) => TokenizerExtensions.IsIdentifier(o), out var variableName) == false)
                {
                    throw new KbParserException($"Invalid query. Found [{token}], expected: [constant variable name].");
                }

                if (lineTokenizer.NextCharacter != '=')
                {
                    throw new KbParserException($"Invalid query. Found [{lineTokenizer.NextCharacter}], expected: [=].");
                }
                lineTokenizer.EatNextCharacter();

                var variableValue = lineTokenizer.Remainder().Trim();

                KbBasicDataType variableType;
                if (variableValue.StartsWith('\'') && variableValue.EndsWith('\''))
                {
                    variableType = KbBasicDataType.String;
                    variableValue = variableValue.Substring(1, variableValue.Length - 2);
                }
                else
                {
                    variableType = KbBasicDataType.Numeric;
                    if (variableValue != null && double.TryParse(variableValue?.ToString(), out _) == false)
                    {
                        throw new Exception($"Non-string value of [{variableName}] cannot be converted to numeric.");
                    }
                }

                tokenizerConstants.Add($"@{variableName}", new KbConstant(variableValue, variableType));

                queryText = queryText.Replace(line, "\n");
            }

            return queryText.TrimEnd() + "\n";
        }

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
