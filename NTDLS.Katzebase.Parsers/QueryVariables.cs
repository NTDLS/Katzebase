using NTDLS.Helpers;
using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Types;
using NTDLS.Katzebase.Parsers.Tokens;
using static NTDLS.Katzebase.Api.KbConstants;

namespace NTDLS.Katzebase.Parsers
{
    public class QueryVariables
    {
        /// <summary>
        /// These are used to map variable names to placeholder, not used for literals.
        /// </summary>
        public KbInsensitiveDictionary<string> VariableForwardLookup { get; private set; } = new();

        /// <summary>
        /// These are used to map variable placeholder to names, not used for literals.
        /// </summary>
        public KbInsensitiveDictionary<string> VariableReverseLookup { get; private set; } = new();

        /// <summary>
        /// Variables contains placeholder lookups for both variables (@VariableName) and string/numeric literals.
        /// </summary>
        public KbInsensitiveDictionary<KbVariable> Collection { get; set; } = new();

        /// <summary>
        /// Resolves a variable or a constant placeholder to its original value.
        /// This method is related to <see cref="Tokenizer.SwapOutVariables(ref string)"/>.
        /// </summary>
        public string? Resolve(string? token)
        {
            if (token == null) return null;

            if (Collection.TryGetValue(token, out var literal))
            {
                if (literal.DataType == KbBasicDataType.Undefined)
                {
                    if (VariableReverseLookup.TryGetValue(token, out var variableName))
                    {
                        if (variableName.Is("null"))
                        {
                            return null;
                        }

                        throw new KbParserException($"Variable is undefined: [{variableName}].");
                    }

                    throw new KbParserException("Variable is undefined.");
                }

                return literal.Value?.AssertUnresolvedExpression();
            }
            else return token?.AssertUnresolvedExpression();
        }

        /// <summary>
        /// Resolves a variable or a constant placeholder to its original value.
        /// This method is related to <see cref="Tokenizer.SwapOutVariables(ref string)"/>.
        /// </summary>
        public string? Resolve(string? token, out KbBasicDataType outDataType)
        {
            if (token != null)
            {
                if (Collection.TryGetValue(token, out var literal))
                {
                    if (literal.DataType == KbBasicDataType.Undefined)
                    {
                        if (VariableReverseLookup.TryGetValue(token, out var variableName))
                        {
                            if (variableName.Is("null"))
                            {
                                outDataType = KbBasicDataType.Undefined;
                                return null;
                            }

                            throw new KbParserException($"Variable is undefined: [{variableName}].");
                        }

                        throw new KbParserException("Variable is undefined.");
                    }

                    outDataType = literal.DataType;
                    return literal.Value?.AssertUnresolvedExpression();
                }
            }
            outDataType = KbBasicDataType.Undefined;
            return token?.AssertUnresolvedExpression();
        }
    }
}
