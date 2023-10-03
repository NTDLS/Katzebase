namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class TabelWorkerThreadParam
    {
        public SelectedSqlItem Item { get; set; }
        public string TargetServerAddress { get; set; }
        public string TargetServerSchema { get; set; }

        public TabelWorkerThreadParam(string targetServerAddress, string targetServerSchema, SelectedSqlItem item)
        {
            TargetServerAddress = targetServerAddress;
            TargetServerSchema = targetServerSchema;
            Item = item;
        }
    }
}
