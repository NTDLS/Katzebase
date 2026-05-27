using ProtoBuf;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.PersistentTypes.Policy
{
    [Serializable]
    [ProtoContract]
    public class PhysicalPolicy
    {
        [ProtoMember(1)]
        public Guid PolicyId { get; set; } = Guid.NewGuid();

        [ProtoMember(2)]
        public SecurityPolicyRule Rule { get; set; }

        [ProtoMember(3)]
        public SecurityPolicyPermission Permission { get; set; }

        /// <summary>
        /// RoleId to apply this policy to.
        /// </summary>
        [ProtoMember(4)]
        public Guid RoleId { get; set; }

        [ProtoMember(5)]
        public bool IsRecursive { get; set; }
    }
}
