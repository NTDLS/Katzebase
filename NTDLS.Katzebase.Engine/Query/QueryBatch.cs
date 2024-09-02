using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Engine.Query
{
    internal class QueryBatch : List<PreparedQuery>
    {
        public KbInsensitiveDictionary<string> UserParameters { get; set; } = new();

        public KbInsensitiveDictionary<string> StringLiterals { get; set; } = new();
        public KbInsensitiveDictionary<string> NumericLiterals { get; set; } = new();


        private KbInsensitiveDictionary<string>? _coalescedLiterals = null;
        public KbInsensitiveDictionary<string> CoalescedLiterals
        {
            get
            {
                if (_coalescedLiterals == null)
                {
                    _coalescedLiterals = new KbInsensitiveDictionary<string>();

                    //We do not include UserParameters in this list because they are swapped
                    //  out for numeric or string tokens by Tokenizer.InterpolateUserVariables.

                    foreach (var item in StringLiterals)
                    {
                        CoalescedLiterals.Add(item.Key, item.Value);
                    }

                    foreach (var item in NumericLiterals)
                    {
                        CoalescedLiterals.Add(item.Key, item.Value);
                    }
                }
                return _coalescedLiterals;
            }
        }

        public QueryBatch(KbInsensitiveDictionary<string> userParameters, KbInsensitiveDictionary<string> stringLiterals, KbInsensitiveDictionary<string> numericLiterals)
        {
            UserParameters = userParameters;
            StringLiterals = stringLiterals;
            NumericLiterals = numericLiterals;
        }

        public string GetLiteralValue(string value)
        {
            if (CoalescedLiterals.TryGetValue(value, out string? stringLiteral))
            {
                return stringLiteral;
            }
            else return value;
        }
    }
}
