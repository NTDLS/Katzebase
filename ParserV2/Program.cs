using NTDLS.Katzebase.Client.Exceptions;
using ParserV2.Parsers;
using ParserV2.Parsers.Query;
using static ParserV2.StandIn.Types;

namespace ParserV2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TestParse("SELECT TOP 100\r\n\tschema.Field as Doc,\t (11 * 11) + 7 as SomeMath,\r\n 'Text' as Name, 10 + Length('some text') as MathFirst, Concat('Text1: ', 10 + 10 + Length(Concat('Other', 'Text'))) as Text,\r\n\t10 * 10 as Id,\r\n\tLength('This is text') as LanguageId,\r\n\t'English' as Language\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
            TestParse("SELECT TOP 100\r\n\t10 + Length(Concat('Other', SHA1('Text' + 'Text2'))) as Text\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
            TestParse("SELECT TOP 100\r\n\tConcat('Text1: ', 10 + 10 + Length(Concat('Other', 'Text'))) as Text\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
        }

        static private QueryFieldCollection TestParse(string queryText)
        {
            var tokenizer = new Tokenizer(queryText, true);

            var token = tokenizer.GetNext();

            if (StaticQueryParser.IsNextStartOfQuery(token, out var queryType) == false)
            {
                string acceptableValues = string.Join("', '", Enum.GetValues<QueryType>().Where(o => o != QueryType.None));
                throw new KbParserException($"Invalid query. Found '{tokenizer.InertGetNext()}', expected: '{acceptableValues}'.");
            }

            if (queryType == QueryType.Select)
            {
                if (tokenizer.TryIsNextToken("top"))
                {
                    tokenizer.SkipNext();
                }

                var selectFields = StaticQueryParser.ParseSelectFields(tokenizer);
                return selectFields;
            }
            else if (queryType == QueryType.Select)
            {
                //var updateFields = StaticQueryParser.ParseUpdateFields(tokenizer);
                //return updateFields;
            }

            throw new Exception("Unknown query type.");
        }

    }
}
