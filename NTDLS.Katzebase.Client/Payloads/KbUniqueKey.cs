namespace NTDLS.Katzebase.Client.Payloads
{
    public class KbUniqueKey : KbIndex
    {
        public override bool IsUnique
        {
            get => true;
            set { }
        }

        public KbUniqueKey(string name) : base(name) { }

        public KbUniqueKey(string name, string[] attributes) : base(name, attributes) { }

        public KbUniqueKey(string name, string attributesCsv) : base(name, attributesCsv) { }

        public KbUniqueKey() : base() { }
    }
}
