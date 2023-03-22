using System;

namespace Katzebase.Library.Payloads
{
    public class KbSchema
    {
        public string? Name { get; set; }
        public Guid? Id { get; set; }

        public KbSchema Clone()
        {
            return new KbSchema
            {
                Id = this.Id,
                Name = this.Name
            };
        }
    }
}
