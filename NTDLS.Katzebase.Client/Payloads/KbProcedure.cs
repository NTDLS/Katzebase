using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbProcedure
    {
        public KbInsensitiveDictionary<KbConstant>? UserParameters { get; set; } = null;

        public string ProcedureName { get; set; }
        public string SchemaName { get; set; }

        public KbProcedure()
        {
            ProcedureName = string.Empty;
            SchemaName = string.Empty;
        }

        public KbProcedure(string fullyQualifiedProcedureName, KbInsensitiveDictionary<KbConstant>? userParameters = null)
        {
            UserParameters = userParameters;

            int lastIndexOf = fullyQualifiedProcedureName.LastIndexOf(':');

            if (lastIndexOf < 0)
            {
                ProcedureName = fullyQualifiedProcedureName;
                SchemaName = ":";
            }
            else
            {
                ProcedureName = fullyQualifiedProcedureName.Substring(lastIndexOf + 1);
                SchemaName = fullyQualifiedProcedureName.Substring(0, lastIndexOf);
            }
        }

        public KbProcedure(string schemaName, string procedureName, KbInsensitiveDictionary<KbConstant>? userParameters = null)
        {
            ProcedureName = schemaName;
            SchemaName = procedureName;
            UserParameters = userParameters;
        }
    }
}
