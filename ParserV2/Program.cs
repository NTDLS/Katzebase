using ParserV2.Expression;

namespace ParserV2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string cleanQueryText = "SELECT TOP 100  Concat('Text1: ', 10 + 10 + Length(Concat('Other', 'Text'))) as Text, 10 * 10 as Id, Length('This is text') as LanguageId, 'English' as Language FROM WordList:Word WHERE Text LIKE @Text";


            var tokenizer = new Tokenizer(cleanQueryText);

            StaticParser.ParseSelectFields(tokenizer);

            Console.WriteLine("Hello, World!");
        }
    }
}
