using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    public class PhysicalIndexAttribute
    {
        public string? Field { get; set; }

        public static PhysicalIndexAttribute FromClientPayload(KbIndexAttribute indexAttribute)
        {
            return new PhysicalIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public static KbIndexAttribute ToClientPayload(PhysicalIndexAttribute indexAttribute)
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
