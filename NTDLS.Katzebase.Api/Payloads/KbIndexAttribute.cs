using System.Collections.Concurrent;
using System.Xml.Linq;

namespace NTDLS.Katzebase.Api.Payloads
{
    public class KbIndexAttribute
    {
        public string? Field { get; set; }

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(
                Field
            );

            return hash;
        }
    }
}
