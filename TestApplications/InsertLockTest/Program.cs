using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Payloads;

namespace InsertLockTest
{
    internal class Program
    {
        static void Main()
        {
            CreateSchemaDeadlock();
        }

        #region CreateSchemaDeadlock.

        static void CreateSchemaDeadlock()
        {
            using (var client = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword("")))
            {
                client.QueryTimeout = TimeSpan.FromDays(10);

                client.Transaction.Begin();
                client.Schema.CreateRecursive("LockTesting:A");
                client.Schema.CreateRecursive("LockTesting:B");
                client.Transaction.Commit();
            }

            using var clientA = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword(""), "TestA");
            clientA.QueryTimeout = TimeSpan.FromDays(10);

            using var clientB = new KbClient("127.0.0.1", 6858, "admin", KbClient.HashPassword(""), "TestB");
            clientB.QueryTimeout = TimeSpan.FromDays(10);

            clientA.Schema.Create("Test");

            Thread.Sleep(200);

            while (true)
            {
                Threading.StartThread(clientA, CreateSchemaDeadlockA);
                Threading.StartThread(clientB, CreateSchemaDeadlockB);

                Thread.Sleep(2500);
            }
        }

        static void CreateSchemaDeadlockA(KbClient client)
        {
            try
            {
                client.Schema.Create("Test:TestA");
            }
            catch
            {
            }
        }

        static void CreateSchemaDeadlockB(KbClient client)
        {
            try
            {
                client.Schema.Create("Test:TestB");
            }
            catch
            {
            }

        }

        #endregion

        #region LockTesting.

        static void LockTesting()
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

        #endregion
    }
}
