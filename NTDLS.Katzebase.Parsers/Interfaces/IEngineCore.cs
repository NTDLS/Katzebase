using NTDLS.Katzebase.Client.Types;

namespace NTDLS.Katzebase.Parsers.Interfaces
{
    public interface IEngineCore
    {
        KbInsensitiveDictionary<KbConstant> GlobalConstants { get; }
    }

    public interface IStringable
    {
        bool IsNullOrEmpty();
        IStringable ToLowerInvariant();
        string GetKey();
        //char[] ToCharArr();
        //Func<string, IStringable?> Converter { get; }
        T ToT<T>();
        object ToT(Type t);
        T ToNullableT<T>();

    }
}
