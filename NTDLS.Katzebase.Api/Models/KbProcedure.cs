using NTDLS.Katzebase.Api.Types;

namespace NTDLS.Katzebase.Api.Models
{
    public class KbProcedure
    {
        public KbInsensitiveDictionary<KbVariable>? UserParameters { get; set; } = null;

        public string ProcedureName { get; set; }
        public string SchemaName { get; set; }

        public KbProcedure()
        {
            ProcedureName = string.Empty;
            SchemaName = string.Empty;
        }

        public KbProcedure(string fullyQualifiedProcedureName, KbInsensitiveDictionary<KbVariable>? userParameters = null)
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

        public KbProcedure(string schemaName, string procedureName, KbInsensitiveDictionary<KbVariable>? userParameters = null)
        {
            ProcedureName = schemaName;
            SchemaName = procedureName;
            UserParameters = userParameters;
        }
    }
}
