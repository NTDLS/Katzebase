namespace NTDLS.Katzebase.Api.Models
{
    public class KbIndexAttribute
    {
        public string? Field { get; set; }

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(
                Field
            );

            return hash;
        }
    }
}
