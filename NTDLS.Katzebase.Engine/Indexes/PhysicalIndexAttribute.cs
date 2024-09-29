using NTDLS.Katzebase.Client.Payloads;
using NTDLS.Katzebase.Parsers.Interfaces;

namespace NTDLS.Katzebase.Engine.Indexes
{
    public class PhysicalIndexAttribute : IPhysicalIndexAttribute
    {
        public string? Field { get; set; }

        public static PhysicalIndexAttribute FromClientPayload(KbIndexAttribute indexAttribute)
        {
            return new PhysicalIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public static KbIndexAttribute ToClientPayload(IPhysicalIndexAttribute indexAttribute)
        {
            return new KbIndexAttribute()
            {
                Field = indexAttribute.Field
            };
        }

        public IPhysicalIndexAttribute Clone()
        {
            return new PhysicalIndexAttribute()
            {
                Field = Field
            };
        }
    }
}
