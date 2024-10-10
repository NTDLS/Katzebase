namespace NTDLS.Katzebase.Api.Models
{
    public class KbMembership
    {
        public int AccountId { get; set; }
        public int RoleId { get; set; }

        public KbMembership(int accountId, int roleId)
        {
            AccountId = accountId;
            RoleId = roleId;
        }

        public KbMembership()
        {
        }
    }
}
