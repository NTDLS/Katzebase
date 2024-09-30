namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class SelectedImportObject
    {
        public string SourceObjectName { get; set; }
        public string TargetServerSchema { get; set; }
        public int TargetServerSchemaPageSize { get; set; }
        public DataGridViewRow RowItem { get; set; }

        public bool ImportData { get; set; }
        public bool ImportIndexes { get; set; }

        public SelectedImportObject(DataGridViewRow rowItem, string sourceObjectName, string targetSchemaName, int targetServerSchemaPageSize)
        {
            RowItem = rowItem;
            SourceObjectName = sourceObjectName;
            TargetServerSchema = targetSchemaName;
            TargetServerSchemaPageSize = targetServerSchemaPageSize;
        }
    }
}
