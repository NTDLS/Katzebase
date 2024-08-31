using NTDLS.Katzebase.Engine.Query.Tokenizers;
using static NTDLS.Katzebase.Engine.Functions.StaticFunctionParsers;

namespace NTDLS.Katzebase.Engine.QueryExpression
{
    internal static class StaticExpressionParser
    {

        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        public static QueryExpressions ParseSelectFields(QueryTokenizer queryTokenizer)
        {
            var result = new QueryExpressions();

            if (queryTokenizer.Text.Contains("Language"))
            {
                int stopAt = queryTokenizer.GetNextIndexOf([" from ", " into "]);

                var fieldsText = queryTokenizer.SubString(stopAt);

                var fields = TokenHelpers.SplitWhileObeyingScope(fieldsText);

                foreach (var field in fields)
                {
                    ParseExpression(field, queryTokenizer);
                }

            }

            return result;
        }

        private static NewFunctionCall ParseExpression(string text, QueryTokenizer queryTokenizer)
        {
            var result = new NewFunctionCall();

            QueryTokenizer tokenizer = new(text);

            if (tokenizer.IsNextCharacter(char.IsLetter) && IsNextNonIdentifier(text, 0, ['('], out var identIndex))
            {
                var functionName = tokenizer.GetNext();

                string functionBody = tokenizer.GetMatchingBraces('(', ')');

                var parameters = TokenHelpers.SplitWhileObeyingScope(functionBody);

                foreach (var param in parameters)
                {
                    if (IsNumericOperation(param))
                    {
                    }
                    else
                    {
                    }
                }
            }

            return result;
        }

    }
}
