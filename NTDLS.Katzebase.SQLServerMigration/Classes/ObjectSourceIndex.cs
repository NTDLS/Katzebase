namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class ObjectSourceIndex
    {
        public string? IndexName { get; set; }
        public string? ColumnName { get; set; }
        public bool IsUnique { get; set; }
    }
}
