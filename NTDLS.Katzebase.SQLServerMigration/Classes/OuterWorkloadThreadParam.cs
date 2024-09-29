namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class OuterWorkloadThreadParam
    {
        public List<SelectedImportObject> Items { get; set; } = new();
        public string TargetServerHost { get; set; }
        public int TargetServerPort { get; set; }
        public string TargetServerUsername { get; set; }
        public string TargetServerPasswordHash { get; set; }

        public OuterWorkloadThreadParam(string targetServerHost, int targetServerPort, string targetServerUsername, string targetServerPasswordHash)
        {
            TargetServerHost = targetServerHost;
            TargetServerPort = targetServerPort;
            TargetServerUsername = targetServerUsername;
            TargetServerPasswordHash = targetServerPasswordHash;
        }
    }
}
