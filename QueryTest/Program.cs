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
            public int Id { get; set; }
            public string? Text { get; set; }
            public int LanguageId { get; set; }
            public int SourceId { get; set; }
            public bool IsDirty { get; set; }
        }

        static void Main()
        {
            try
            {
                using var client = new KbClient(_serverHost, _serverPort, _username, KbClient.HashPassword(_password));

                //This should work:
                //var words = client.Query.Fetch<Word>("SELECT TOP 100 * FROM WordList:Word WHERE Text LIKE @Text + '%'", new { Text = "Fly" });
                //This should NOT work:
                var words = client.Query.Fetch<Word>("SELECT TOP 100 * FROM WordList:Word WHERE Text LIKE @Text", new { Text = "Fly%" });

                foreach (var word in words)
                {
                    Console.WriteLine($"{word.Id},{word.Text},{word.LanguageId},{word.SourceId},{word.IsDirty}");
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
