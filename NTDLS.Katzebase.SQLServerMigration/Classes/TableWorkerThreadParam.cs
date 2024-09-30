namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class TableWorkerThreadParam
    {
        public SelectedImportObject Item { get; set; }
        public int TargetServerPort { get; set; }
        public string TargetServerHost { get; set; }
        public string TargetServerSchema { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public TableWorkerThreadParam(string targetServerHost, int targetServerPort, string targetServerSchema, string username, string password, SelectedImportObject item)
        {
            TargetServerHost = targetServerHost;
            TargetServerPort = targetServerPort;
            TargetServerSchema = targetServerSchema;
            Username = username;
            Password = password;
            Item = item;
        }
    }
}
