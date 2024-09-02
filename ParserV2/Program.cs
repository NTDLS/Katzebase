using NTDLS.Katzebase.Client.Exceptions;
using NTDLS.Katzebase.Engine.Functions.Aggregate;
using NTDLS.Katzebase.Engine.Functions.Scaler;
using NTDLS.Katzebase.Engine.Parsers;
using NTDLS.Katzebase.Engine.Parsers.Query;
using static NTDLS.Katzebase.Engine.Library.EngineConstants;

namespace ParserV2
{
    internal class Program
    {

        static void Main(string[] args)
        {
            ScalerFunctionCollection.Initialize();
            AggregateFunctionCollection.Initialize();

            TestParse("SELECT TOP 100\r\n\tschema.Field as Doc,\t'Text' + schema.Field as Doc1,\t (11 * 11) + 7 as SomeMath,\r\n 'Text' as Name, 10 + Length('some text') as MathFirst, Concat('Text1: ', 10 + 10 + Length(Concat('Other', someSchema.FirstName))) as Text,\r\n\t10 * 10 as Id,\r\n\tLength('This is text') as LanguageId,\r\n\t'English' as Language\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
            TestParse("SELECT TOP 100\r\n\t10 + Length(Concat('Other', SHA1('Text' + 'Text2'))) as Text\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
            TestParse("SELECT TOP 100\r\n\tConcat('Text1: ', 10 + 10 + Length(Concat(Other, 'Text'))) as Text\r\nFROM\r\n\tWordList:Word WHERE Text LIKE @Text");
        }

        static private QueryFieldCollection TestParse(string queryText)
        {
            var tokenizer = new Tokenizer(queryText, true);

            var token = tokenizer.GetNext();

            if (StaticParser.IsNextStartOfQuery(token, out var queryType) == false)
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

                var selectFields = StaticParser.ParseSelectFields(tokenizer);
                return selectFields;
            }
            else if (queryType == QueryType.Insert)
            {
                //var updateFields = StaticQueryParser.ParseUpdateFields(tokenizer);
                //return updateFields;
            }

            throw new Exception("Unknown query type.");
        }


    }
}
