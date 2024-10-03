using NTDLS.DelegateThreadPooling;
using NTDLS.Katzebase.Client;
using System.Diagnostics;

namespace QueryTest
{
    internal static class Program
    {
        const string _serverHost = "127.0.0.1";
        const int _serverPort = 6858;
        const string _username = "admin";
        const string _password = "";

        static void Main()
        {
            try
            {
                //using var client = new KbClient(_serverHost, _serverPort, _username, KbClient.HashPassword(_password));

                var dtp = new DelegateThreadPool();

                var childQueue = dtp.CreateChildQueue<Guid>();


                for (int item = 0; item < 10000; item++)
                {
                    childQueue.Enqueue(Guid.NewGuid(), (param) =>
                    {
                        for (int workload = 0; workload < 10000; workload++)
                        {
                            foreach (var c in param.ToString())
                            {
                            }
                        }
                    });
                }

                childQueue.WaitForCompletion();

                foreach (var thread in dtp.Threads)
                {
                    Console.WriteLine($"Thread: {thread.ManagedThread.ManagedThreadId}: {thread.NativeThread?.TotalProcessorTime.TotalMilliseconds:n0} ");
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
