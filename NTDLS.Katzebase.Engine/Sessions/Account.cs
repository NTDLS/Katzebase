namespace NTDLS.Katzebase.Engine.Sessions
{
    internal class Account
    {
        public string? Username { get; set; }
        public string? PasswordHash { get; set; }

        public Account(string username, string passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
        }

        public Account()
        {
        }
    }
}
