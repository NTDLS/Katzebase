namespace NTDLS.Katzebase.Api.Models
{
    public class KbResponseFieldSampleItem
    {
        public string Name { get; set; } = string.Empty;

        public KbResponseFieldSampleItem()
        {
        }

        public KbResponseFieldSampleItem(string name)
        {
            Name = name;
        }
    }
}
