namespace NTDLS.Katzebase.Parsers.Interfaces
{
    public interface IPhysicalIndexAttribute
    {
        string? Field { get; }
        IPhysicalIndexAttribute Clone();
    }
}
