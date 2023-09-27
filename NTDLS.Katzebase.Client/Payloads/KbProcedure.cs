namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbProcedure
    {
        public KbProcedureParameters Parameters { get; private set; } = new();

        public string ProcedureName { get; set; }
        public string SchemaName { get; set; }

        public KbProcedure()
        {
            ProcedureName = string.Empty;
            SchemaName = string.Empty;
        }

        public KbProcedure(string fullProcedureName)
        {
            int lastIndexOf = fullProcedureName.LastIndexOf(':');

            if (lastIndexOf < 0)
            {
                ProcedureName = fullProcedureName;
                SchemaName = ":";
            }
            else
            {
                ProcedureName = fullProcedureName.Substring(lastIndexOf + 1);
                SchemaName = fullProcedureName.Substring(0, lastIndexOf);
            }
        }

        public KbProcedure(string schemaName, string procedureName)
        {
            ProcedureName = schemaName;
            SchemaName = procedureName;
        }
    }
}
