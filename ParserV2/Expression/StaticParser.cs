namespace ParserV2.Expression
{
    internal static class StaticParser
    {

        /// <summary>
        /// Parses the field expressions for a "select" or "select into" query.
        /// </summary>
        public static Expressions ParseSelectFields(Tokenizer queryTokenizer)
        {
            var result = new Expressions();

            if (queryTokenizer.InertContains("Language"))
            {
                int stopAt = queryTokenizer.InertGetNextIndexOf([" from ", " into "]);

                var fieldsText = queryTokenizer.SubString(stopAt);

                var fields = fieldsText.ScopeSensitiveSplit();

                foreach (var field in fields)
                {
                    //ParseExpression(field, queryTokenizer);
                }

            }

            return result;
        }

        /*
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
        */

    }
}
