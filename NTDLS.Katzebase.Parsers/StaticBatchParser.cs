using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Tokens;

namespace NTDLS.Katzebase.Parsers
{
    public class StaticBatchParser
    {
        /// <summary>
        /// Parse the query batch (a single query text containing multiple queries).
        /// </summary>
        /// <param name="queryText"></param>
        /// <param name="userParameters"></param>
        /// <returns></returns>
        static public PreparedQueryBatch Parse(string queryText, KbInsensitiveDictionary<KbVariable> givenConstants,
            KbInsensitiveDictionary<KbVariable>? userParameters = null)
        {
            var constants = givenConstants.Clone(); //Clone because we do not want to modify the global constants collection.

            userParameters ??= new();

            //If we have user parameters, add them to a clone of the global tokenizer constants.
            foreach (var param in userParameters)
            {
                constants.Add(param.Key, param.Value);
            }

            var tokenizer = new Tokenizer(queryText, true, constants);
            var queryBatch = new PreparedQueryBatch(tokenizer.Variables);

            var exceptions = new List<Exception>();

            while (!tokenizer.IsExhausted())
            {
                int preParseTokenPosition = tokenizer.Caret;

                try
                {
                    var query = StaticQueryParser.Parse(queryBatch, tokenizer);

                    var singleQueryText = tokenizer.Substring(preParseTokenPosition, tokenizer.Caret - preParseTokenPosition);
                    query.Hash = StaticParserUtility.ComputeSHA256(singleQueryText);
                    queryBatch.Add(query);
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
    }
}
