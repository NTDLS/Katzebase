using NTDLS.Katzebase.Client.Payloads;

namespace NTDLS.Katzebase.Management.StaticAnalysis
{
    /// <summary>
    /// Schemas that are cached for static analysis
    /// </summary>
    public class CachedSchema
    {
        public DateTime CachedDateTime { get; set; } = DateTime.UtcNow;
        public KbSchema Schema { get; set; }

        public List<string>? Fields => throw new NotImplementedException();
        public List<KbIndex> Indexes { get; set; } = new();

        public CachedSchema(KbSchema schema)
        {
            Schema = schema;
        }
    }
}
