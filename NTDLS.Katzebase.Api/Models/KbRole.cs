namespace NTDLS.Katzebase.Api.Models
{
    public class KbRole
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsAdministrator { get; set; }

        public KbRole(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public KbRole()
        {
        }
    }
}
