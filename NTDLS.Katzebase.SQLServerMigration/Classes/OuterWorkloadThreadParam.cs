namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class OuterWorkloadThreadParam
    {
        public List<SelectedSqlItem> Items { get; set; } = new();
        public string TargetServerAddress { get; set; }
        public string TargetServerSchema { get; set; }

        public OuterWorkloadThreadParam(string targetServerAddress, string targetServerSchema)
        {
            TargetServerAddress = targetServerAddress;
            TargetServerSchema = targetServerSchema;
        }
    }
}
