namespace Katzebase.PublicLibrary.Payloads
{
    public class KbQueryResult : KbActionResponse
    {
        public List<KbQueryField> Fields { get; set; }
        public List<KbQueryRow> Rows { get; set; }

        public KbQueryResult()
        {
            Fields = new List<KbQueryField>();
            Rows = new List<KbQueryRow>();
        }
    }
}
