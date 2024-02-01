using NTDLS.Katzebase.Server;
using Topshelf;
using Topshelf.ServiceConfigurators;

namespace NTDLS.Katzebase.Server
{
    public class Program
    {
        public class KatzebaseService
        {
            private SemaphoreSlim _semaphoreToRequestStop;
            private Thread _thread;

            public KatzebaseService(ServiceConfigurator< KatzebaseService> s )
            {
                _semaphoreToRequestStop = new SemaphoreSlim(0);
                _thread = new Thread(DoWork);
            }

            public void Start()
            {
                _thread.Start();
            }

            public void Stop()
            {
                _semaphoreToRequestStop.Release();
                _thread.Join();
            }

            private void DoWork()
            {
                try
                {
                    var apiService = new APIService();

                    apiService.Start();

                    while (true)
                    {
                        if (_semaphoreToRequestStop.Wait(500))
                        {
                            apiService.Stop();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occured while starting or the service: {ex.Message}");
                    return;
                }
            }
        }

        public static void Main()
        {
            HostFactory.Run(x =>
            {
                x.StartAutomatically(); // Start the service automatically

                x.EnableServiceRecovery(rc =>
                {
                    rc.RestartService(1); // restart the service after 1 minute
                });

                x.Service<KatzebaseService>(s =>
                {
                    s.ConstructUsing(hostSettings => new KatzebaseService(s));
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("Katzebase document-based database services.");
                x.SetDisplayName("Katzebase Service");
                x.SetServiceName("Katzebase");
            });
        }
    }
}
