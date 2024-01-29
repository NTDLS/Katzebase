namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class TabelWorkerThreadParam
    {
        public SelectedSqlItem Item { get; set; }
        public int TargetServerPort { get; set; }
        public string TargetServerHost { get; set; }
        public string TargetServerSchema { get; set; }

        public TabelWorkerThreadParam(string targetServerHost, int targetServerPort, string targetServerSchema, SelectedSqlItem item)
        {
            TargetServerHost = targetServerHost;
            TargetServerPort = targetServerPort;
            TargetServerSchema = targetServerSchema;
            Item = item;
        }
    }
}
