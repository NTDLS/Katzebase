using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;

namespace NugetTest
{
    internal class Program
    {
        const string _serverHost = "127.0.0.1";
        const int _serverPort = 6858;

        static void Main()
        {
            var threads = new List<Thread>()
            {
                new Thread(InsertUsingAPI),
                new Thread(InsertUsingQueries),
            };

            using (var client = new KbClient(_serverHost, _serverPort))
            {
                client.Schema.DropIfExists("ClientTest");
            }

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            Console.WriteLine("Complete.");

            Console.ReadLine();
        }

        public static void InsertUsingAPI()
        {
            try
            {
                using var client = new KbClient(_serverHost, _serverPort);

                string schemaName = "ClientTest:B";
                int id = 0;

                client.Schema.Create(schemaName);

                client.Transaction.Begin();

                for (int s = 0; s < 10; s++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine($"InsertUsingAPI: {id}");

                        client.Document.Store(schemaName, new
                        {
                            Id = id++,
                            SegmentA = i,
                            SegmentB = s,
                            UUID = Guid.NewGuid()
                        });
                    }
                }
                client.Transaction.Commit();

                client.Schema.Indexes.Create(schemaName, new KbUniqueKey("IX_UUID", "UUID"));
                client.Schema.Indexes.Create(schemaName, new KbUniqueKey("IX_ID", "Id"));
                client.Schema.Indexes.Create(schemaName, new KbIndex("IX_Segments", "SegmentA,SegmentB"));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void InsertUsingQueries()
        {
            try
            {
                using var client = new KbClient(_serverHost, _serverPort);
                string schemaName = "ClientTest:A";
                int id = 0;

                client.Schema.Create(schemaName);

                client.Transaction.Begin();
                for (int s = 0; s < 10; s++)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        Console.WriteLine($"InsertUsingQueries: {id}");
                        client.Query.ExecuteQuery($"INSERT INTO {schemaName} (Id = {id++}, SegmentA = {i}, SegmentB = {s}, UUID = '{Guid.NewGuid()}')");
                    }
                }
                client.Transaction.Commit();

                client.Query.ExecuteQuery($"CREATE UniqueKey IX_UUID (UUID) ON {schemaName}");
                client.Query.ExecuteQuery($"CREATE UniqueKey IX_ID (Id) ON {schemaName}");
                client.Query.ExecuteQuery($"CREATE INDEX IX_Segments (SegmentA, SegmentB) ON {schemaName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
