using System.Security.Cryptography;
using System.Text;
using static NTDLS.Katzebase.Parsers.Constants;

namespace NTDLS.Katzebase.Parsers
{
    public static class StaticParserUtility
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

        public static string ComputeSHA256(string input)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);

            var builder = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
