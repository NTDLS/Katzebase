using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Parses the text for a single comma separated field expression, respecting strings, numeric, formulas and parentheses scopes.
        /// </summary>
        /// <param name="stopAt">Array of strings which indicate the end of the field expression list.</param>
        /// <param name="outFieldExpression">Variable to return the field expression.</param>
        /// <returns>Returns true if there is still content to be parsed with another call, meaning the stopAt was not found and the tokenizer is not exhausted.</returns>
        /// <exception cref="KbParserException"></exception>
        public bool EatGetSingleFieldExpression(string[] stopAt, out string outFieldExpression)
            => EatGetSingleFieldExpression(',', stopAt, out outFieldExpression);

        /// <summary>
        /// Parses the text for a single comma separated field expression, respecting strings, numeric, formulas and parentheses scopes.
        /// </summary>
        /// <param name="fieldSeparator">Field expression separator, typically a comma.</param>
        /// <param name="stopAt">Array of strings which indicate the end of the field expression list.</param>
        /// <param name="outFieldExpression">Variable to return the field expression.</param>
        /// <returns>Returns true if there is still content to be parsed with another call, meaning the stopAt was not found and the tokenizer is not exhausted.</returns>
        /// <exception cref="KbParserException"></exception>
        public bool EatGetSingleFieldExpression(char fieldSeparator, string[] stopAt, out string outFieldExpression)
        {
            string token;
            int startCaret = Caret;
            int endCaret = 0;

            bool isTextRemainingToParse = true;

            while (!IsExhausted())
            {
                token = GetNext();
                if (token == "(")
                {
                    EatMatchingScope();
                }
                else if (stopAt.Contains(token))
                {
                    endCaret = Caret;
                    isTextRemainingToParse = false;
                    break; //exit loop to parse, found where or join clause.
                }
                else if (token.Length == 1 && token[0] == fieldSeparator)
                {
                    endCaret = Caret;
                    EatNext();
                    break; //exit loop to parse next field.
                }
                else if (token == ")")
                {
                    throw new KbParserException($"Invalid expression, found end of scope: [{token}].");
                }
                else if (token.Length == 1 && (token[0].IsTokenConnectorCharacter() || token[0].IsMathematicalOperator()))
                {
                    EatNext();
                }
                else
                {
                    EatNext();
                }
            }

            outFieldExpression = Substring(startCaret, endCaret - startCaret).Trim();

            return isTextRemainingToParse && !IsExhausted();
        }
    }
}
