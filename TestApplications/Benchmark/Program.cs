using NTDLS.Helpers;
using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Api.Payloads;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Benchmark
{
    internal class Program
    {
        static DateTime StartDateTime = DateTime.Now;
        static string OutputFileName = @$"..\..\..\Output\{StartDateTime.ToString().Replace("/", "_").Replace(":", "_").Replace("\\", "_")}.txt";
        const int _iterationsPerTest = 10;
        const string _ScriptsPath = @"..\..\..\Scripts";
        const string _DataPath = @"..\..\..\Data";
        const string _ServicePath = @"..\..\..\..\..\NTDLS.Katzebase.Server\bin\Release\net8.0\NTDLS.Katzebase.Server.exe";
        const string _serverHost = "127.0.0.1";
        const int _serverPort = 6858;

        static void Main()
        {
            File.AppendAllText(OutputFileName, "StartDate\tTest\tIteration\tDurationMs\tPeakMemory\tCpuTime\r\n");

            Console.WriteLine("Creating payloads:");
            CreatePayloadData();

            Console.WriteLine("Executing scripts:");
            ExecuteBenchmark_Scripts();

            Console.WriteLine("Executing inserts:");
            ExecuteBenchmark_Inserts();

            Console.WriteLine("Executing updates:");
            //TODO: implement.

            Console.WriteLine("Executing deletes:");
            //TODO: implement.
        }

        private static void ExecuteBenchmark_Inserts()
        {
            ExecuteBenchmark_Inserts("Payload.gz", "Benchmarking:Insertion_tx10", 10000, 10);
            ExecuteBenchmark_Inserts("Payload.gz", "Benchmarking:Payload_Insertion_tx100", 10000, 100);
            ExecuteBenchmark_Inserts("Payload.gz", "Benchmarking:Insertion_tx1000", 10000, 1000);
        }

        private static void ExecuteBenchmark_Inserts(string fileName, string schemaName, int maxCount, int rowsPerTransaction)
        {
            Console.WriteLine($"ExecuteBenchmark_Inserts: {schemaName}");

            var process = StartService();
            using (var client = new KbClient(_serverHost, _serverPort, "admin", KbClient.HashPassword("")))
            {
                client.Schema.DropIfExists(schemaName);
                client.Schema.Create(schemaName);

                var bytes = DecompressToString(File.ReadAllBytes(Path.Combine(_DataPath, fileName)));
                var payloadRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(bytes).EnsureNotNull();

                double previousTotalProcessorTime = 0;
                for (int i = 0; i < _iterationsPerTest; i++)
                {
                    var startTime = DateTime.Now;

                    int rowCount = 0;

                    client.Transaction.Begin();

                    foreach (var row in payloadRows)
                    {
                        if (rowCount > maxCount)
                        {
                            break;
                        }

                        if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                        {
                            client.Transaction.Commit();
                            client.Transaction.Begin();
                        }

                        try
                        {
                            client.Document.Store(schemaName, new KbDocument(row));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        rowCount++;
                    }

                    client.Transaction.Commit();

                    double thisTotalProcessorTime = process.TotalProcessorTime.TotalSeconds;
                    double deltaTotalProcessorTime = thisTotalProcessorTime - previousTotalProcessorTime;
                    previousTotalProcessorTime = thisTotalProcessorTime;

                    WriteMetrics(schemaName, i, (DateTime.Now - startTime).TotalMilliseconds, process.PeakWorkingSet64, deltaTotalProcessorTime);
                }
            }
            Thread.Sleep(1000);
            process.Kill();
        }

        private static void ExecuteBenchmark_Scripts()
        {
            var scriptFiles = GetBenchmarkScripts();

            foreach (var scriptFile in scriptFiles)
            {
                var process = StartService();
                using (var client = new KbClient(_serverHost, _serverPort, "admin", KbClient.HashPassword("")))
                {
                    var queryText = File.ReadAllText(scriptFile);

                    double previousTotalProcessorTime = 0;
                    for (int i = 0; i < _iterationsPerTest; i++)
                    {
                        Console.WriteLine($"ExecuteBenchmark_Scripts: {Path.GetFileNameWithoutExtension(scriptFile)}->{i}");

                        var result = client.Query.Fetch(queryText);

                        double thisTotalProcessorTime = process.TotalProcessorTime.TotalSeconds;
                        double deltaTotalProcessorTime = thisTotalProcessorTime - previousTotalProcessorTime;
                        previousTotalProcessorTime = thisTotalProcessorTime;

                        WriteMetrics(Path.GetFileNameWithoutExtension(scriptFile), i, result.Duration, process.PeakWorkingSet64, deltaTotalProcessorTime);
                    }
                }
                Thread.Sleep(1000);
                process.Kill();
            }
        }

        private static void CreatePayloadData()
        {
            var process = StartService();
            using (var client = new KbClient(_serverHost, _serverPort, "admin", KbClient.HashPassword("")))
            {
                client.Schema.DropIfExists("Benchmarking");
                client.Schema.Create("Benchmarking:Payload_1000");
                client.Schema.Create("Benchmarking:Payload_10000");
                client.Schema.Create("Benchmarking:Payload_100000");

                var bytes = DecompressToString(File.ReadAllBytes(Path.Combine(_DataPath, "Payload.gz")));
                var payloadRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(bytes).EnsureNotNull();

                int rowCount = 0;
                int rowsPerTransaction = 1234;

                client.Transaction.Begin();
                foreach (var row in payloadRows)
                {
                    if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                    {
                        client.Transaction.Commit();
                        Console.Write($"Committing {rowsPerTransaction:n0} rows, total: {rowCount:n0} ({((rowCount / (double)payloadRows.Count) * 100.0):n2}%)...\r");
                        client.Transaction.Begin();
                    }

                    try
                    {
                        if (rowCount <= 1000)
                        {
                            client.Document.Store("Benchmarking:Payload_1000", new KbDocument(row));
                        }
                        if (rowCount <= 10000)
                        {
                            client.Document.Store("Benchmarking:Payload_10000", new KbDocument(row));
                        }
                        if (rowCount <= 100000)
                        {
                            client.Document.Store("Benchmarking:Payload_100000", new KbDocument(row));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    rowCount++;

                    if (rowCount > 100000)
                    {
                        break;
                    }
                }
                Console.Write($"Committing {rowsPerTransaction:n0} rows, total: {rowCount:n0} ({((rowCount / (double)payloadRows.Count) * 100.0):n2}%)...\r");
                client.Transaction.Commit();
            }
            Console.WriteLine();
            Console.WriteLine("Payloads creation complete." );

            Thread.Sleep(1000);
            process.Kill();
        }

        private static void InsertPayloadData(string fileName, string schemaName, int maxCount)
        {
            var process = StartService();
            using (var client = new KbClient(_serverHost, _serverPort, "admin", KbClient.HashPassword("")))
            {
                client.Schema.DropIfExists(schemaName);
                client.Schema.Create(schemaName);

                var bytes = DecompressToString(File.ReadAllBytes(Path.Combine(_DataPath, fileName)));
                var payloadRows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(bytes).EnsureNotNull();

                for (int i = 0; i < _iterationsPerTest; i++)
                {
                    int rowCount = 0;
                    int rowsPerTransaction = 1000;

                    client.Transaction.Begin();
                    foreach (var row in payloadRows)
                    {
                        if (rowCount > maxCount)
                        {
                            break;
                        }

                        if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                        {
                            client.Transaction.Commit();
                            client.Transaction.Begin();
                        }

                        try
                        {
                            client.Document.Store(schemaName, new KbDocument(row));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        rowCount++;
                    }
                    client.Transaction.Commit();
                }
            }
            Thread.Sleep(1000);
            process.Kill();
        }

        private static List<string> GetBenchmarkScripts()
        {
            return Directory.EnumerateFiles(_ScriptsPath, "*.kbs").ToList();
        }

        private static Process StartService()
        {
            var process = new Process();
            process.StartInfo.FileName = _ServicePath;
            process.StartInfo.UseShellExecute = true;  // Use shell to start the process
            process.StartInfo.CreateNoWindow = false;  // Ensures a new window is created
            process.Start();
            return process;
        }

        public static byte[] Decompress(byte[] bytes)
        {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return mso.ToArray();
        }

        public static string DecompressToString(byte[] bytes) => Encoding.UTF8.GetString(Decompress(bytes));

        static void WriteMetrics(string name, int iteration, double durationMs, double peakMemory, double cpuTime)
        {
            File.AppendAllText(OutputFileName, $"{StartDateTime}\t{name}\t{iteration}\t{durationMs:n2}\t{(peakMemory / 1024.0 / 1024.0):n2}\t{cpuTime:n2}\r\n");
            Console.WriteLine($"{name}\t{iteration}\t{durationMs:n2}\t{(peakMemory / 1024.0 / 1024.0):n2}\t{cpuTime:n2}");
        }
    }
}
