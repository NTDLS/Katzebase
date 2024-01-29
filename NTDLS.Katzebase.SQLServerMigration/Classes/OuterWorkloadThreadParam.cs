namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class OuterWorkloadThreadParam
    {
        public List<SelectedSqlItem> Items { get; set; } = new();
        public string TargetServerHost { get; set; }
        public int TargetServerPort { get; set; }
        public string TargetServerSchema { get; set; }

        public OuterWorkloadThreadParam(string targetServerHost, int targetServerPort, string targetServerSchema)
        {
            TargetServerHost = targetServerHost;
            TargetServerPort = targetServerPort;
            TargetServerSchema = targetServerSchema;
        }
    }
}
