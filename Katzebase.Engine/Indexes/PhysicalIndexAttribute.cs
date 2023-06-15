using Katzebase.PublicLibrary.Payloads;

namespace Katzebase.Engine.Indexes
{
    public class PhysicalIndexAttribute
    {
        public string? Field { get; set; }

        public static PhysicalIndexAttribute FromPayload(KbIndexAttribute indexAttribute)
        {
            return new PhysicalIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public static KbIndexAttribute ToPayload(PhysicalIndexAttribute indexAttribute)
        {
            return new KbIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public PhysicalIndexAttribute Clone()
        {
            return new PhysicalIndexAttribute()
            {
                Field = Field
            };
        }
    }
}
