namespace NTDLS.Katzebase.Api.Models
{
    public class KbAccount
    {
        public Guid Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }

        public KbAccount(Guid id, string username, string passwordHash)
        {
            Id = id;
            Username = username;
            PasswordHash = passwordHash;
        }

        public KbAccount()
        {
        }
    }
}
