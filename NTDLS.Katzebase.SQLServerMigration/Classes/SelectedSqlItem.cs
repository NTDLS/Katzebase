namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class SelectedSqlItem
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public int ListViewRowIndex { get; set; }

        public SelectedSqlItem(int listViewRowIndex, string schema, string name)
        {
            ListViewRowIndex = listViewRowIndex;
            Schema = schema;
            Table = name;
        }
    }
}
