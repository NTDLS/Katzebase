using NTDLS.Katzebase.Api.Models;

namespace NTDLS.Katzebase.PersistentTypes.Index
{
    public class PhysicalIndexAttribute
    {
        public string? Field { get; set; }

        public static PhysicalIndexAttribute FromClientPayload(KbIndexAttribute indexAttribute)
            => new()
            {
                Field = indexAttribute.Field
            };

        public static KbIndexAttribute ToClientPayload(PhysicalIndexAttribute indexAttribute)
            => new()
            {
                Field = indexAttribute.Field
            };

        public PhysicalIndexAttribute Clone()
            => new()
            {
                Field = Field
            };
    }
}
