using System.Diagnostics.CodeAnalysis;

namespace NTDLS.Katzebase.Parsers.Tokens
{
    public partial class Tokenizer
    {
        /// <summary>
        /// Finds the next token that matches using the given comparer.
        /// </summary>
        public bool TryFindCompareNext(TryNextTokenProc comparer, char[] delimiters,
            [NotNullWhen(true)] out string? outFoundToken, [NotNullWhen(true)] out int? foundAtCaret)
        {
            int restoreCaret = Caret;

            while (!IsExhausted())
            {
                foundAtCaret = Caret;
                outFoundToken = EatGetNext(delimiters);
                if (comparer(outFoundToken))
                {
                    Caret = restoreCaret;
                    return true;
                }
            }

            Caret = restoreCaret;

            outFoundToken = null;
            foundAtCaret = null;

            return false;
        }

        /// <summary>
        /// Finds the next token that matches using the given comparer.
        /// </summary>
        public bool TryFindCompareNext(TryNextTokenProc comparer,
            [NotNullWhen(true)] out string? outFoundToken, [NotNullWhen(true)] out int? foundAtCaret)
        {
            foundAtCaret = null;

            int restoreCaret = Caret;
            while (!IsExhausted())
            {
                foundAtCaret = Caret;
                outFoundToken = EatGetNext();
                if (comparer(outFoundToken))
                {
                    Caret = restoreCaret;
                    return true;
                }
            }

            Caret = restoreCaret;

            outFoundToken = null;
            foundAtCaret = null;

            return false;
        }

        /// <summary>
        /// Finds the next token that matches using the given comparer.
        /// </summary>
        public bool TryFindCompareNext(TryNextTokenComparerProc comparer, string[] givenTokens, char[] delimiters,
            [NotNullWhen(true)] out string? outFoundToken, [NotNullWhen(true)] out int? foundAtCaret)
        {
            int restoreCaret = Caret;
            while (!IsExhausted())
            {
                foundAtCaret = Caret;
                outFoundToken = EatGetNext();
                foreach (var givenToken in givenTokens)
                {
                    if (comparer(outFoundToken, givenToken))
                    {
                        Caret = restoreCaret;
                        return true;
                    }
                }
            }

            Caret = restoreCaret;

            outFoundToken = null;
            foundAtCaret = null;

            return false;
        }
    }
}
