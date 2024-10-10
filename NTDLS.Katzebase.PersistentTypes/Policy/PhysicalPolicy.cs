using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.PersistentTypes.Policy
{
    public class PhysicalPolicy
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SecurityPolicy Policy { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SecurityPolicyType PolicyType { get; set; }

        /// <summary>
        /// AccountId to apply this policy to, if both AccountId and RoleId are NULL then this policy applies to all accounts.
        /// </summary>
        public int? AccountId { get; set; }
        /// <summary>
        /// RoleId to apply this policy to, if both AccountId and RoleId are NULL then this policy applies to all accounts.
        /// </summary>
        public int? RoleId { get; set; }
        public bool IsRecursive { get; set; }
    }
}
