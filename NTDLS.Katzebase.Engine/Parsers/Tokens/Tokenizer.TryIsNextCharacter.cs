using NTDLS.Katzebase.Client.Exceptions;

namespace NTDLS.Katzebase.Engine.Parsers.Tokens
{
    internal partial class Tokenizer
    {
        /// <summary>
        /// Returns the boolean value from the given delegate which is passed the next character in the sequence.
        /// </summary>
        public bool TryIsNextCharacter(NextCharacterProc proc)
        {
            var next = NextCharacter ?? throw new KbParserException("The tokenizer sequence is empty.");
            return proc(next);
        }

        /// <summary>
        /// Returns true if the next character matches the given value.
        /// </summary>
        public bool TryIsNextCharacter(char character)
            => NextCharacter == character;
    }
}
