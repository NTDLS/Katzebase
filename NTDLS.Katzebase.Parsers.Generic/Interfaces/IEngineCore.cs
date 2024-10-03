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

    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this IStringable value)
        {
            if (value == null) return true;
            return value.IsNullOrEmpty();
        }
        public static T? ParseToT<T>(this string value, Func<string, T> parse)
        {
            if (value == null) return default(T);
            return parse(value);
        }

        public static T? CastToT<T>(this string value, Func<string, T> cast)
        {
            if (value == null) return default(T);
            return cast(value);
        }
        public static IStringable? Empty => null;

    }
}
