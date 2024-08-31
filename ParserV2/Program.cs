using NTDLS.Katzebase.Client.Exceptions;
using ParserV2.Expression;
using static ParserV2.StandIn.Types;

namespace ParserV2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string cleanQueryText = "SELECT TOP 100\r\n\tschema.Field as Doc,\t 'Text' as Name, 10 + Length('some text') as MathFirst, Concat('Text1: ', 10 + 10 + Length(Concat('Other', 'Text'))) as Text,\r\n\t10 * 10 as Id,\r\n\tLength('This is text') as LanguageId,\r\n\t'English' as Language\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text";

            char[] standardTokenDelimiters = [',', '='];

            var tokenizer = new Tokenizer(cleanQueryText, standardTokenDelimiters);

            tokenizer.Prepare();

            if (tokenizer.IsNextStartOfQuery(out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{tokenizer.InertGetNext()}', expected: '{acceptableValues}'.");
            }

            if (tokenizer.TryIsNextToken("top"))
            {
                tokenizer.SkipNext();
            }

            StaticParser.ParseSelectFields(tokenizer);

            Console.WriteLine("Hello, World!");
        }
    }
}
