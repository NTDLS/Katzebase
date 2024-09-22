using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;

namespace InsertLockTest
{
    internal class Program
    {
        static void Main()
        {
            using (var client = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword("")))
            {
                client.QueryTimeout = TimeSpan.FromDays(10);

                client.Transaction.Begin();
                client.Schema.CreateRecursive("LockTesting:A");
                client.Schema.CreateRecursive("LockTesting:B");
                client.Transaction.Commit();
            }

            new Thread(InsertA).Start();
            Thread.Sleep(1000);
            new Thread(InsertB).Start();
        }

        static void InsertA()
        {
            using var client = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword(""), "TestA");
            client.QueryTimeout = TimeSpan.FromDays(10);

            while (true)
            {
                client.Transaction.Begin();

                for (int i = 0; i < 100; i++)
                {
                    client.Document.Store("LockTesting:A", new KbDocument(new TestPayload()));
                    Console.WriteLine("LockTesting:A");
                }

                client.Transaction.Commit();
            }
        }

        static void InsertB()
        {
            using var client = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword(""), "TestB");
            client.QueryTimeout = TimeSpan.FromDays(10);

            while (true)
            {
                client.Transaction.Begin();

                for (int i = 0; i < 100; i++)
                {
                    client.Document.Store("LockTesting:B", new KbDocument(new TestPayload()));
                    Console.WriteLine("LockTesting:B");
                }

                client.Transaction.Commit();
            }
        }
    }
}
