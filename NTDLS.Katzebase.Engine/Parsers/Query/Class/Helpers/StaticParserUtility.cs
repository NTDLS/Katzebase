using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace NTDLS.Katzebase.Engine.Parsers.Query.Class.Helpers
{
    internal static class StaticParserUtility
    {
        /// <summary>
        /// Returns true if the next token in the sequence is a valid token as would be expected as the start of a new query.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsStartOfQuery(string token, out QueryType type)
        {
            return Enum.TryParse(token.ToLowerInvariant(), true, out type) //Enum parse.
                && Enum.IsDefined(typeof(QueryType), type) //Is enum value über lenient.
                && int.TryParse(token, out _) == false; //Is not number, because enum parsing is "too" flexible.
        }

        public static bool IsStartOfQuery(string token)
        {
            return Enum.TryParse(token.ToLowerInvariant(), true, out QueryType type) //Enum parse.
                && Enum.IsDefined(typeof(QueryType), type) //Is enum value über lenient.
                && int.TryParse(token, out _) == false; //Is not number, because enum parsing is "too" flexible.
        }
    }
}
