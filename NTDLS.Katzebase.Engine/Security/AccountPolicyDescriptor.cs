using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Security
{
    internal class AccountPolicyDescriptor
    {
        /// <summary>
        /// The permission being granted or denied.
        /// </summary>
        public SecurityPolicyPermission Permission { get; set; }

        /// <summary>
        /// Whether the permission is granted or denied.
        /// </summary>
        public SecurityPolicyRule Rule { get; set; }

        /// <summary>
        /// The role from which this policy is redrived.
        /// </summary>
        public string? InheritedFromRole { get; set; }

        /// <summary>
        /// The schema from which this policy is redrived.
        /// </summary>
        public string? InheritedFromSchema { get; set; }

        /// <summary>
        /// Whether this policy has been explicitly set.
        /// </summary>
        public bool IsSet { get; set; } = false;
    }
}
