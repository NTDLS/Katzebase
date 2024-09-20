using NTDLS.Katzebase.Client.Types;
using NTDLS.Katzebase.Engine.Parsers.Query;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    /// <summary>
    /// Used to walk various types of string and expressions.
    /// </summary>
    internal partial class Tokenizer
    {
        #region Rules and Convention.

        /*
         * Public functions that DO NOT modify the internal caret should NOT be prefixed with anything particular.
         * Public functions that DO modify the internal caret should be prefixed with "Eat".
         * Public functions that DO NOT throw exceptions should be prefixed with "Try".
         * Public functions that are NOT prefixed with "Try" should throw exceptions if they do not find/seek what they are intended to do.
        */

        /* Token Placeholders:
         * $n_0$ = numeric.
         * $s_0$ = string.
         * $x_0$ = expression (result from a function call).
         * $f_0$ = document field placeholder.
         */

        #endregion

        #region Delegates.

        public delegate bool GetNextIndexOfProc(string peekedToken);
        public delegate bool TryNextTokenValidationProc(string peekedToken);
        public delegate bool TryNextTokenComparerProc(string peekedToken, string givenToken);
        public delegate bool TryNextTokenProc(string peekedToken);
        public delegate bool NextCharacterProc(char c);

        #endregion

        #region Private backend variables.

        private string? _hash = null;
        private readonly Stack<int> _breadCrumbs = new();
        private int _literalKey = 0;
        private string _text;
        private int _caret = 0;
        private readonly char[] _standardTokenDelimiters;

        #endregion

        #region Public properties.

        public char? NextCharacter => _caret < _text.Length ? _text[_caret] : null;
        public bool IsExhausted() => _caret >= _text.Length;
        public char[] TokenDelimiters => _standardTokenDelimiters;
        public int Caret => _caret;
        public int Length => _text.Length;
        public string Text => _text;
        public string Hash => _hash ??= Library.Helpers.ComputeSHA256(_text);
        public KbInsensitiveDictionary<KbConstant> PredefinedConstants { get; set; }
        public KbInsensitiveDictionary<ConditionFieldLiteral> Literals { get; private set; } = new();

        #endregion

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="standardTokenDelimiters">Delimiters to stop on when parsing tokens. If not specified, will only stop on whitespace.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, char[] standardTokenDelimiters, bool optimizeForTokenization = false,
            KbInsensitiveDictionary<KbConstant>? predefinedConstants = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = standardTokenDelimiters;
            PredefinedConstants = predefinedConstants ?? new();

            PreValidate();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
                PostValidate();
            }
        }

        /// <summary>
        /// Creates a tokenizer which uses only whitespace as a delimiter.
        /// </summary>
        /// <param name="text">Text to tokenize.</param>
        /// <param name="optimizeForTokenization">Whether the query text should be optimized for tokenization.
        /// This only needs to be done once per query text. For example, if you create another Tokenizer
        /// with a subset of this Tokenizer, then the new instance does not need to be optimized</param>
        public Tokenizer(string text, bool optimizeForTokenization = false,
            KbInsensitiveDictionary<KbConstant>? predefinedConstants = null)
        {
            _text = new string(text.ToCharArray());
            _standardTokenDelimiters = ['\r', '\n', ' '];
            PredefinedConstants = predefinedConstants ?? new();

            PreValidate();

            if (optimizeForTokenization)
            {
                OptimizeForTokenization();
                PostValidate();
            }
        }
    }
}
