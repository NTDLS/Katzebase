namespace NTDLS.Katzebase.Api.Models
{
    public class KbAccount
    {
        public int Id { get; set; }
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }

        public KbAccount(int id, string username, string passwordHash)
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
