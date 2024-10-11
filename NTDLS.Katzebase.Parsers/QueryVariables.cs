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

        public string? Resolve(string? value)
        {
            if (value == null) return null;

            if (Collection.TryGetValue(value, out var literal))
            {
                if (literal.DataType == KbBasicDataType.Undefined)
                {
                    if (VariableReverseLookup.TryGetValue(value, out var variableName))
                    {
                        throw new KbParserException($"Variable is undefined: [{variableName}].");
                    }

                    throw new KbParserException("Variable is undefined.");
                }

                return literal.Value?.AssertUnresolvedExpression();
            }
            else return value?.AssertUnresolvedExpression();
        }

        public string? Resolve(string? value, out KbBasicDataType outDataType)
        {
            if (value != null)
            {
                if (Collection.TryGetValue(value, out var literal))
                {
                    if (literal.DataType == KbBasicDataType.Undefined)
                    {
                        if (VariableReverseLookup.TryGetValue(value, out var variableName))
                        {
                            throw new KbParserException($"Variable is undefined: [{variableName}].");
                        }

                        throw new KbParserException("Variable is undefined.");
                    }

                    outDataType = literal.DataType;
                    return literal.Value?.AssertUnresolvedExpression();
                }
            }
            outDataType = KbBasicDataType.Undefined;
            return value?.AssertUnresolvedExpression();
        }
    }
}
