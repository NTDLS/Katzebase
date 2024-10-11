using NTDLS.Katzebase.Api.Types;

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
    }
}
