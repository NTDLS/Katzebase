using NTDLS.Katzebase.Client;

namespace QueryTest
{
    internal static class Program
    {
        const string _serverHost = "127.0.0.1";
        const int _serverPort = 6858;
        const string _username = "admin";
        const string _password = "";

        class Word
        {
            public int Len { get; set; }
            public int Id { get; set; }
            public string? Text { get; set; }
            public string? Language { get; set; }
            public int LanguageId { get; set; }
            public int SourceId { get; set; }
            public bool IsDirty { get; set; }

            public string? Test1 { get; set; }
            public string? Test2 { get; set; }
            public string? Test3 { get; set; }
        }

        class BigQuery
        {
            public int SourceWordId { get; set; }
            public string? SourceWord { get; set; }
            public string? SourceLanguage { get; set; }
            public int TargetWordId { get; set; }
            public string? TargetWord { get; set; }
            public string? TargetLanguage { get; set; }
            public string? Test { get; set; }
        }

        class GroupTest
        {
            public int NumberOf1 { get; set; }
            public int NumberOf2 { get; set; }
            public string? Latin { get; set; }
            public string? Spanish { get; set; }
        }

        class DistinctTest
        {
            public string? v1 { get; set; }
            public string? v2 { get; set; }
            public string? v3 { get; set; }
            public string? Latin { get; set; }
            public int RowCount { get; set; }
            public int GermanWordCount { get; set; }
            public int SpanishWordCount { get; set; }
        }

        static void Main()
        {
            try
            {
                using var client = new KbClient(_serverHost, _serverPort, _username, KbClient.HashPassword(_password));

                //var queryText = "SELECT\r\n\tLanguageId,\r\n\tSum(Id + 10) as NumberOf1,Min(Id - 10) as NumberOf2\r\nFROM\r\n\tWordList:Word\r\nWHERE\r\n\tText LIKE 'Tab%'\r\nGROUP BY\r\n\tLanguageId";
                //var queryText = "select Spanish, count(Id + 7, true) + min(Id) as NumberOf1, Latin, Count(Id) as NumberOf2 from WordList:FlatTranslate where English like 'bed%' group by Spanish, Latin";
                //var queryText = "SELECT\r\n\tSha1Agg('fff' + German) as Latin1, \r\n Latin,\r\n\tCount('hh' + sha1(Guid()) + 'j4', true) as RowCount,\r\n\tCount(German + 'hh', true) as GermanWordCount,\r\n\tCount(Spanish, true) as SpanishWordCount\r\nFROM\r\n\tWordList:FlatTranslate\r\nWHERE\r\n\tEnglish LIKE 'Car%'\r\nGROUP BY\r\n\tLatin\r\n";
                //var queryText = "SELECT\r\n\t\r\nSha1Agg('fff' + German) as Latin, Count(German + 'hh', true) as RowCount,\r\nLatin as sss\r\nFROM\r\n\tWordList:FlatTranslate\r\nWHERE\r\n\tEnglish LIKE 'Car%'\r\nGROUP BY\r\n\tLatin\r\n";
                //var queryText = "SELECT\r\n\tLanguageId,\r\n\tId - 651947 as NumberOf\r\nFROM\r\n\tWordList:Word\r\nWHERE\r\n\tText LIKE 'Tab%'";
                //var queryText = "select top 100\r\n\tsw.Id as SourceWordId,\r\n\t'yo' + 'to' + 10 as Test,\r\n\tToProper(sw.Text) as SourceWord,\r\n\tsl.Name as SourceLanguage,\r\n\ttw.Id as TargetWordId,\t\r\n\tToProper(tw.Text) as TargetWord,\r\n\ttl.Name as TargetLanguage\r\nfrom\r\n\tWordList:Word as sw\r\ninner join WordList:Language as sl\r\n\ton sl.Id = sw.LanguageId\r\ninner join WordList:Synonym as S\r\n\ton S.SourceWordId = sw.Id\r\ninner join WordList:Word as tw\r\n\ton tw.Id = S.TargetWordId\r\ninner join WordList:Language as tl\r\n\ton tl.Id = TW.LanguageId\r\nwhere\r\n\tsw.Text LIKE 'Ta%'\r\n\tand sw.Text LIKE '%le'\r\n\tand sl.Name = 'English'\r\n\tand tl.Name = 'English'\r\n\tand sw.Text != tw.Text\r\norder by\r\n\tsw.Text,\r\n\ttw.Text";
                //This should work:
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 * FROM WordList:Word WHERE Text LIKE @Text + '%'", new { Text = "Fly" });
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 Text, LanguageId, Id, SourceId, IsDirty FROM WordList:Word WHERE Text LIKE @Text + '%'", new { Text = "Fly" });
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 Length('Hello' + 'World') as Test1, length((Text + @MyText) + Sha1('ooo')) as Len, Text, LanguageId, Id, SourceId, IsDirty FROM WordList:Word WHERE Text LIKE 'Fly%'", new { MyText = "Smurf" });
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 'hello' + 'world' as Test2, Text, Length(Text + 'World') + 20 as Test1 FROM WordList:Word WHERE Text LIKE 'Fly%'", new { MyText = "Smurf" });
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 Text, 'hello ' + 'world' as Test1 FROM WordList:Word WHERE Text LIKE 'Fly%'", new { MyText = "Smurf" });
                //This should NOT work:
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 'Text1' + 'Text2' FROM WordList:Word WHERE Text LIKE @Text", new { Text = "Fly%" }, TimeSpan.FromMinutes(600));
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 Concat('Text1', 'Text2') as Text FROM WordList:Word WHERE Text LIKE @Text", new { Text = "Fly%" }, TimeSpan.FromMinutes(600));
                //var words = client.Query.Fetch<Word>("SELECT TOP 100  Concat('Text1: ', 10 + 10 + Length(Concat('Other', 'Text'))) as Text, 10 * 10 as Id, Length('This is text') as LanguageId, 'English' as Language FROM WordList:Word WHERE Text LIKE @Text", new { Text = "Fly%" }, TimeSpan.FromMinutes(600));

                var results = client.Query.Fetch<DistinctTest>(
                    "SELECT\r\n\t@Var1 as v1,\r\n\t@Var2 as v2,\r\n\t@Var3 as v3,\r\n\tLanguageId,\r\n\tId - 651947 as NumberOf\r\nFROM\r\n\tWordList:Word\r\nWHERE\r\n\tText LIKE 'Tab%'"
                    , new { Var1 = 124, Var2 = "My Text", Var3 = "321" });

                //var results = client.Query.Fetch<BigQuery>(queryText);
                foreach (var result in results)
                {
                    Console.WriteLine($"[{result.Latin}],[{result.GermanWordCount}],[{result.SpanishWordCount}],[{result.RowCount}]");
                    //Console.WriteLine($"[{result.Test}],[{result.SourceWordId}],[{result.SourceWord}],[{result.SourceLanguage}],{result.TargetWordId},{result.TargetWord},{result.TargetLanguage}");
                    //Console.WriteLine($"{word.Len},{word.Id},{word.Text},{word.LanguageId},{word.SourceId},{word.IsDirty}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
