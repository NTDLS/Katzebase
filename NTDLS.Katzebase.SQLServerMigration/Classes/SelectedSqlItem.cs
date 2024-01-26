namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class SelectedSqlItem
    {
        public string Schema { get; set; }
        public string Table { get; set; }
        public ListViewItem ListItem { get; set; }

        public SelectedSqlItem(ListViewItem item, string schema, string name)
        {
            ListItem = item;
            Schema = schema;
            Table = name;
        }
    }
}
