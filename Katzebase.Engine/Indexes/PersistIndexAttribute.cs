using Katzebase.Library.Payloads;

namespace Katzebase.Engine.Indexes
{
    public class PersistIndexAttribute
    {
        public string Name { get; set; }

        public static PersistIndexAttribute FromPayload(KbIndexAttribute indexAttribute)
        {
            return new PersistIndexAttribute()
            {
                Name = indexAttribute.Name
            };
        }

        public PersistIndexAttribute Clone()
        {
            return new PersistIndexAttribute()
            {
                Name = Name
            };
        }
    }
}
