namespace NTDLS.Katzebase.SQLServerMigration.Classes
{
    internal class ObjectSourceObject
    {
        public string? SourceSchemaObject { get; set; }
        public string? TargetSchemaObject { get; set; }
        public string? TargetObject { get; set; }
        public string? TotalSizeBytes { get; set; }
        public int TotalRows { get; set; }
        public int AvgRowSizeBytes { get; set; }
        public int TargetPageSize { get; set; }
    }
}
