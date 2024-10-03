using NTDLS.Katzebase.Parsers.Interfaces;
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

    public class Account<TData> where TData : IStringable
    {
        public TData? Username { get; set; }
        public TData? PasswordHash { get; set; }

        public Account()
        {
        }

        public Account(TData username, TData passwordHash)
        {
            Username = username;
            PasswordHash = passwordHash;
        }
    }
}
