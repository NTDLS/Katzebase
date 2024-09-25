namespace NTDLS.Katzebase.Engine.Sessions
{
    public class Account
    {
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }

        public Account()
        {
        }

        public Account(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
        }
    }
}
