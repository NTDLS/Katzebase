using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Indexes
{
    public class PersistIndexAttribute
    {
        public string? Field { get; set; }

        public static PersistIndexAttribute FromPayload(KbIndexAttribute indexAttribute)
        {
            return new PersistIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public PersistIndexAttribute Clone()
        {
            return new PersistIndexAttribute()
            {
                Field = Field
            };
        }
    }
}
