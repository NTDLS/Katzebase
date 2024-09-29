using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Parsers.Interfaces
{
    public interface IEngineCore
    {
        KbInsensitiveDictionary<KbConstant> GlobalConstants { get; }
    }
}
