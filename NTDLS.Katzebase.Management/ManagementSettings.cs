namespace NTDLS.Katzebase.Management
{
    internal class ManagementSettings
    {
        public int QueryMaximumRows { get; set; } = 1000;
        public int UIQueryTimeOut { get; set; } = 10;
        public int UserQueryTimeOut { get; set; } = -1;

        public bool EditorShowLineNumbers { get; set; } = true;
        public bool EditorWordWrap { get; set; } = false;
        public double EditorFontSize { get; set; } = 12.5;
        public string EditorFontFamily { get; set; } = "Cascadia Mono";
    }
}
