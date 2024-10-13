using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Parsers.Query.Specific.Root;
using NTDLS.Katzebase.Parsers.Query.SupportingTypes;
using NTDLS.Katzebase.Parsers.Query.Validation;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers.Query
{
    public class StaticParserQuery
    {
        /// <summary>
        /// Parse the single query in the batch.
        /// </summary>
        static public PreparedQuery Parse(PreparedQueryBatch queryBatch, Tokenizer tokenizer)
        {
            string token = tokenizer.GetNext();

            if (StaticParserUtility.IsStartOfQuery(token, out var queryType) == false)
            {
                tokenizer.EatGetNext();
                throw new KbParserException(tokenizer.GetCurrentLineNumber(),
                    $"Expected  [{string.Join("],[", Enum.GetValues<QueryType>().Where(o => o != QueryType.None))}], found: [{token}].");
            }

            tokenizer.EatNext();

            var query = queryType switch
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

                QueryType.Grant => StaticParserGrant.Parse(queryBatch, tokenizer),
                QueryType.Deny => StaticParserDeny.Parse(queryBatch, tokenizer),

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

            BasicQueryValidation.Assert(tokenizer, query);

            return query;
        }
    }
}
