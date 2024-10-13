using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.PersistentTypes.Policy
{
    public class PhysicalPolicy
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SecurityPolicyRule Rule { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SecurityPolicyPermission Permission { get; set; }

        /// <summary>
        /// RoleId to apply this policy to.
        /// </summary>
        public Guid RoleId { get; set; }
        public bool IsRecursive { get; set; }
    }
}
