namespace NTDLS.Katzebase.Api.Models
{
    public class KbRole
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public bool IsAdministrator { get; set; }

        public KbRole(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        public KbRole()
        {
        }
    }
}
