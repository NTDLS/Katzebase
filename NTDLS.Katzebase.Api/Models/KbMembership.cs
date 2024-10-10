namespace NTDLS.Katzebase.Api.Models
{
    public class KbMembership
    {
        public Guid AccountId { get; set; }
        public Guid RoleId { get; set; }

        public KbMembership(Guid accountId, Guid roleId)
        {
            AccountId = accountId;
            RoleId = roleId;
        }

        public KbMembership()
        {
        }
    }
}
