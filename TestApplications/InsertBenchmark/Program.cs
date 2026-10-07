using NTDLS.Katzebase.Api;
using NTDLS.Katzebase.Engine;
using NTDLS.Katzebase.Shared;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using System.Globalization;

namespace InsertBenchmark
{
    /// <summary>
    /// Measures single-row INSERT performance through the client API.
    ///
    /// Scenarios:
    ///   1) One row per INSERT, no explicit transaction (every statement runs in its own implicit transaction).
    ///   2) One row per INSERT, inside an explicit transaction that is committed every [--commit-every] rows.
    ///   3) One document per Document.Store() call, inside an explicit transaction committed every [--commit-every] rows.
    ///   4) Document.StoreMany() batches of [--batch-size] documents, each batch in its own (implicit) transaction.
    ///
    /// By default an engine and message server are hosted in-process against a fresh, temporary data directory,
    /// so results are repeatable and no running server is required. Use --server to benchmark an external server.
    ///
    /// Usage:
    ///   InsertBenchmark [--rows 10000] [--commit-every 1000] [--batch-size 1000] [--warmup 200] [--no-indexes]
    ///                   [--server host:port] [--user admin] [--password ""]
    ///                   [--data-path path] [--port 6869] [--keep-data] [--results file.tsv] [--label text]
    /// </summary>
    internal class Program
    {
        private const string RootSchema = "InsertBenchmark";

        private class Options
        {
            public int Rows { get; set; } = 10_000;
            public int CommitEvery { get; set; } = 1_000;
            public int BatchSize { get; set; } = 1_000;
            public int Warmup { get; set; } = 200;
            public bool CreateIndexes { get; set; } = true;
            public string? Server { get; set; }
            public string User { get; set; } = "admin";
            public string Password { get; set; } = string.Empty;
            public string DataPath { get; set; } = Path.Combine(Path.GetTempPath(), "KatzebaseInsertBenchmark");
            public int Port { get; set; } = 6869;
            public bool KeepData { get; set; }
            public string? ResultsFile { get; set; }
            public string Label { get; set; } = string.Empty;
        }

        private class ScenarioResult(string name, int rows, TimeSpan elapsed, List<double> insertMicroseconds, List<double> commitMicroseconds, string callName = "INSERT")
        {
            public string Name { get; } = name;
            /// <summary>
            /// What a single timed call in InsertMicroseconds represents (one INSERT, one Store() or one StoreMany() batch).
            /// </summary>
            public string CallName { get; } = callName;
            public int Rows { get; } = rows;
            public TimeSpan Elapsed { get; } = elapsed;
            public List<double> InsertMicroseconds { get; } = insertMicroseconds;
            public List<double> CommitMicroseconds { get; } = commitMicroseconds;
            public double RowsPerSecond => Rows / Elapsed.TotalSeconds;
        }

        static int Main(string[] args)
        {
            Options options;
            try
            {
                options = ParseArguments(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            EngineCore? engine = null;
            RmServer? messageServer = null;
            string host = "127.0.0.1";
            int port = options.Port;

            try
            {
                if (options.Server == null)
                {
                    (engine, messageServer) = StartEmbeddedServer(options);
                }
                else
                {
                    var parts = options.Server.Split(':');
                    host = parts[0];
                    port = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 6858;
                }

                PrintHeader(options, host, port);

                using var client = new KbClient(host, port, options.User, KbClient.HashPassword(options.Password), "InsertBenchmark");
                client.QueryTimeout = TimeSpan.FromHours(1);

                PrepareSchemas(client, options);

                if (options.Warmup > 0)
                {
                    //Warm up the JIT, connection, caches and RocksDB instances so they don't skew the first scenario.
                    RunWithoutTransaction(client, $"{RootSchema}:WarmupNoTransaction", options.Warmup);
                    RunWithTransaction(client, $"{RootSchema}:WarmupTransaction", options.Warmup, options.CommitEvery);
                    RunDocumentStore(client, $"{RootSchema}:WarmupDocumentStore", options.Warmup, options.CommitEvery);
                    RunDocumentStoreMany(client, $"{RootSchema}:WarmupDocumentStoreMany", options.Warmup, options.BatchSize);
                }

                var results = new List<ScenarioResult>
                {
                    RunWithoutTransaction(client, $"{RootSchema}:NoTransaction", options.Rows),
                    RunWithTransaction(client, $"{RootSchema}:Transaction", options.Rows, options.CommitEvery),
                    RunDocumentStore(client, $"{RootSchema}:DocumentStore", options.Rows, options.CommitEvery),
                    RunDocumentStoreMany(client, $"{RootSchema}:DocumentStoreMany", options.Rows, options.BatchSize)
                };

                VerifyRowCount(client, $"{RootSchema}:NoTransaction", options.Rows);
                VerifyRowCount(client, $"{RootSchema}:Transaction", options.Rows);
                VerifyRowCount(client, $"{RootSchema}:DocumentStore", options.Rows);
                VerifyRowCount(client, $"{RootSchema}:DocumentStoreMany", options.Rows);

                PrintResults(results);

                if (options.ResultsFile != null)
                {
                    AppendResults(options, results);
                }

                if (options.Server != null && options.KeepData == false)
                {
                    client.Schema.DropIfExists(RootSchema);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Benchmark failed: {ex.GetBaseException().Message}");
                return 2;
            }
            finally
            {
                messageServer?.Stop();
                engine?.Stop();

                if (engine != null && options.KeepData == false)
                {
                    try { Directory.Delete(options.DataPath, true); } catch { }
                }
            }
        }

        #region Scenarios.

        /// <summary>
        /// One INSERT per row, each in its own implicit transaction.
        /// </summary>
        static ScenarioResult RunWithoutTransaction(KbClient client, string schema, int rows)
        {
            var insertTimes = new List<double>(rows);

            var total = Stopwatch.StartNew();
            for (int i = 0; i < rows; i++)
            {
                var statement = MakeInsert(schema, i);

                long start = Stopwatch.GetTimestamp();
                client.Query.ExecuteNonQuery(statement);
                insertTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);
            }
            total.Stop();

            return new ScenarioResult("Single-row INSERT, no explicit transaction", rows, total.Elapsed, insertTimes, []);
        }

        /// <summary>
        /// One INSERT per row inside an explicit transaction, committed every [commitEvery] rows.
        /// </summary>
        static ScenarioResult RunWithTransaction(KbClient client, string schema, int rows, int commitEvery)
        {
            var insertTimes = new List<double>(rows);
            var commitTimes = new List<double>(rows / commitEvery + 1);

            var total = Stopwatch.StartNew();
            client.Transaction.Begin();
            int rowsInTransaction = 0;

            for (int i = 0; i < rows; i++)
            {
                var statement = MakeInsert(schema, i);

                long start = Stopwatch.GetTimestamp();
                client.Query.ExecuteNonQuery(statement);
                insertTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);

                if (++rowsInTransaction == commitEvery)
                {
                    start = Stopwatch.GetTimestamp();
                    client.Transaction.Commit();
                    commitTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);

                    rowsInTransaction = 0;
                    if (i < rows - 1)
                    {
                        client.Transaction.Begin();
                    }
                }
            }

            if (rowsInTransaction > 0)
            {
                long start = Stopwatch.GetTimestamp();
                client.Transaction.Commit();
                commitTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);
            }
            total.Stop();

            return new ScenarioResult($"Single-row INSERT, explicit transaction (commit every {commitEvery:N0})",
                rows, total.Elapsed, insertTimes, commitTimes);
        }

        /// <summary>
        /// One document per Document.Store() call (no SQL parsing) inside an explicit transaction committed every [commitEvery]
        /// rows. This is how NTDLS.Katzebase.SQLServerMigration imported data before it switched to Document.StoreMany().
        /// </summary>
        static ScenarioResult RunDocumentStore(KbClient client, string schema, int rows, int commitEvery)
        {
            var storeTimes = new List<double>(rows);
            var commitTimes = new List<double>(rows / commitEvery + 1);

            var total = Stopwatch.StartNew();
            client.Transaction.Begin();
            int rowsInTransaction = 0;

            for (int i = 0; i < rows; i++)
            {
                var document = MakeDocument(i);

                long start = Stopwatch.GetTimestamp();
                client.Document.Store(schema, document);
                storeTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);

                if (++rowsInTransaction == commitEvery)
                {
                    start = Stopwatch.GetTimestamp();
                    client.Transaction.Commit();
                    commitTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);

                    rowsInTransaction = 0;
                    if (i < rows - 1)
                    {
                        client.Transaction.Begin();
                    }
                }
            }

            if (rowsInTransaction > 0)
            {
                long start = Stopwatch.GetTimestamp();
                client.Transaction.Commit();
                commitTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);
            }
            total.Stop();

            return new ScenarioResult($"Document.Store per row, explicit transaction (commit every {commitEvery:N0})",
                rows, total.Elapsed, storeTimes, commitTimes, "STORE");
        }

        /// <summary>
        /// Document.StoreMany() in batches of [batchSize], one round trip and one (implicit) transaction per batch.
        /// </summary>
        static ScenarioResult RunDocumentStoreMany(KbClient client, string schema, int rows, int batchSize)
        {
            var batchTimes = new List<double>(rows / batchSize + 1);
            var batch = new List<object>(batchSize);

            var total = Stopwatch.StartNew();
            for (int i = 0; i < rows; i++)
            {
                batch.Add(MakeDocument(i));

                if (batch.Count == batchSize || i == rows - 1)
                {
                    long start = Stopwatch.GetTimestamp();
                    client.Document.StoreMany(schema, batch);
                    batchTimes.Add(Stopwatch.GetElapsedTime(start).TotalMicroseconds);
                    batch.Clear();
                }
            }
            total.Stop();

            return new ScenarioResult($"Document.StoreMany, batches of {batchSize:N0}", rows, total.Elapsed, batchTimes, [], "BATCH");
        }

        static object MakeDocument(int i)
            => new { Code = $"C{i}", Category = $"cat{i % 100}", Sub = $"sub{i % 10}", Amount = i % 1000, Payload = $"payload {i} lorem ipsum dolor sit amet" };

        static string MakeInsert(string schema, int i)
            => $"INSERT INTO {schema}(Code, Category, Sub, Amount, Payload) VALUES('C{i}', 'cat{i % 100}', 'sub{i % 10}', {i % 1000}, 'payload {i} lorem ipsum dolor sit amet')";

        #endregion

        #region Setup.

        static (EngineCore, RmServer) StartEmbeddedServer(Options options)
        {
            if (Directory.Exists(options.DataPath))
            {
                Directory.Delete(options.DataPath, true);
            }

            var settings = new KatzebaseSettings
            {
                ListenPort = options.Port,
                DataRootPath = Path.Combine(options.DataPath, "Root"),
                TransactionDataPath = Path.Combine(options.DataPath, "Transaction"),
                LogDirectory = Path.Combine(options.DataPath, "Logs"),
            };

            var engine = new EngineCore(settings);

            var messageServer = new RmServer();
            messageServer.AddHandler(engine.Documents.APIHandlers);
            messageServer.AddHandler(engine.Indexes.APIHandlers);
            messageServer.AddHandler(engine.Query.APIHandlers);
            messageServer.AddHandler(engine.Schemas.APIHandlers);
            messageServer.AddHandler(engine.Sessions.APIHandlers);
            messageServer.AddHandler(engine.Transactions.APIHandlers);

            engine.Start();
            messageServer.Start(settings.ListenPort);

            return (engine, messageServer);
        }

        static void PrepareSchemas(KbClient client, Options options)
        {
            client.Schema.DropIfExists(RootSchema);
            client.Schema.Create(RootSchema);

            foreach (var schema in new[] { "WarmupNoTransaction", "WarmupTransaction", "WarmupDocumentStore", "WarmupDocumentStoreMany",
                "NoTransaction", "Transaction", "DocumentStore", "DocumentStoreMany" })
            {
                var fullName = $"{RootSchema}:{schema}";
                client.Schema.Create(fullName);

                if (options.CreateIndexes)
                {
                    client.Query.ExecuteNonQuery($"CREATE INDEX ix_Category (Category) ON {fullName}");
                    client.Query.ExecuteNonQuery($"CREATE INDEX ix_Category_Sub (Category, Sub) ON {fullName}");
                    client.Query.ExecuteNonQuery($"CREATE UniqueKey uk_Code (Code) ON {fullName}");
                }
            }
        }

        static void VerifyRowCount(KbClient client, string schema, int expected)
        {
            var actual = client.Query.Fetch($"SELECT Code FROM {schema}").Collection.Single().Rows.Count;
            if (actual != expected)
            {
                throw new Exception($"Expected {expected:N0} rows in [{schema}] but found {actual:N0}.");
            }
        }

        static Options ParseArguments(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Missing value for [{args[i]}].");

                switch (args[i].ToLowerInvariant())
                {
                    case "--rows": options.Rows = int.Parse(Value(), CultureInfo.InvariantCulture); break;
                    case "--commit-every": options.CommitEvery = int.Parse(Value(), CultureInfo.InvariantCulture); break;
                    case "--batch-size": options.BatchSize = int.Parse(Value(), CultureInfo.InvariantCulture); break;
                    case "--warmup": options.Warmup = int.Parse(Value(), CultureInfo.InvariantCulture); break;
                    case "--no-indexes": options.CreateIndexes = false; break;
                    case "--server": options.Server = Value(); break;
                    case "--user": options.User = Value(); break;
                    case "--password": options.Password = Value(); break;
                    case "--data-path": options.DataPath = Value(); break;
                    case "--port": options.Port = int.Parse(Value(), CultureInfo.InvariantCulture); break;
                    case "--keep-data": options.KeepData = true; break;
                    case "--results": options.ResultsFile = Value(); break;
                    case "--label": options.Label = Value(); break;
                    case "--help":
                    case "-h":
                    case "/?":
                        throw new ArgumentException(
                            "Usage: InsertBenchmark [--rows 10000] [--commit-every 1000] [--batch-size 1000] [--warmup 200] [--no-indexes]\n" +
                            "                       [--server host:port] [--user admin] [--password \"\"]\n" +
                            "                       [--data-path path] [--port 6869] [--keep-data] [--results file.tsv] [--label text]");
                    default:
                        throw new ArgumentException($"Unknown argument: [{args[i]}]. Use --help for usage.");
                }
            }

            if (options.Rows <= 0) throw new ArgumentException("--rows must be greater than zero.");
            if (options.CommitEvery <= 0) throw new ArgumentException("--commit-every must be greater than zero.");
            if (options.BatchSize <= 0) throw new ArgumentException("--batch-size must be greater than zero.");

            return options;
        }

        #endregion

        #region Reporting.

        static void PrintHeader(Options options, string host, int port)
        {
            Console.WriteLine("Katzebase insert benchmark");
            Console.WriteLine($"  Server       : {(options.Server == null ? $"embedded ({options.DataPath})" : $"{host}:{port}")}");
            Console.WriteLine($"  Rows         : {options.Rows:N0} per scenario");
            Console.WriteLine($"  Commit every : {options.CommitEvery:N0} rows (explicit transaction scenarios)");
            Console.WriteLine($"  Batch size   : {options.BatchSize:N0} documents (StoreMany scenario)");
            Console.WriteLine($"  Indexes      : {(options.CreateIndexes ? "ix_Category, ix_Category_Sub, uk_Code (unique)" : "none")}");
            Console.WriteLine($"  Build        : {(Debugger.IsAttached ? "debugger attached, " : "")}{GetBuildConfiguration()}");
            Console.WriteLine();
        }

        static void PrintResults(List<ScenarioResult> results)
        {
            foreach (var result in results)
            {
                Console.WriteLine(result.Name);
                Console.WriteLine($"  Total        : {result.Elapsed.TotalMilliseconds,12:N0} ms");
                Console.WriteLine($"  Throughput   : {result.RowsPerSecond,12:N0} rows/s");
                Console.WriteLine($"  {result.CallName,-6} (us)  : avg {result.InsertMicroseconds.Average(),8:N0}   p50 {Percentile(result.InsertMicroseconds, 50),8:N0}"
                    + $"   p95 {Percentile(result.InsertMicroseconds, 95),8:N0}   p99 {Percentile(result.InsertMicroseconds, 99),8:N0}   max {result.InsertMicroseconds.Max(),8:N0}");
                if (result.CommitMicroseconds.Count > 0)
                {
                    Console.WriteLine($"  COMMIT (us)  : avg {result.CommitMicroseconds.Average(),8:N0}   p50 {Percentile(result.CommitMicroseconds, 50),8:N0}"
                        + $"   max {result.CommitMicroseconds.Max(),8:N0}   ({result.CommitMicroseconds.Count:N0} commits)");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Appends one tab-separated line per scenario so that runs can be compared over time.
        /// </summary>
        static void AppendResults(Options options, List<ScenarioResult> results)
        {
            bool writeHeader = File.Exists(options.ResultsFile) == false;
            using var writer = new StreamWriter(options.ResultsFile!, append: true);

            if (writeHeader)
            {
                writer.WriteLine("timestamp\tlabel\tscenario\trows\tcommit_every\tindexes\ttotal_ms\trows_per_sec\tinsert_avg_us\tinsert_p50_us\tinsert_p95_us\tinsert_p99_us\tcommit_avg_us");
            }

            foreach (var result in results)
            {
                writer.WriteLine(string.Join('\t',
                    DateTime.Now.ToString("s", CultureInfo.InvariantCulture),
                    options.Label,
                    result.Name,
                    result.Rows,
                    options.CommitEvery,
                    options.CreateIndexes,
                    result.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture),
                    result.RowsPerSecond.ToString("F1", CultureInfo.InvariantCulture),
                    result.InsertMicroseconds.Average().ToString("F1", CultureInfo.InvariantCulture),
                    Percentile(result.InsertMicroseconds, 50).ToString("F1", CultureInfo.InvariantCulture),
                    Percentile(result.InsertMicroseconds, 95).ToString("F1", CultureInfo.InvariantCulture),
                    Percentile(result.InsertMicroseconds, 99).ToString("F1", CultureInfo.InvariantCulture),
                    (result.CommitMicroseconds.Count > 0 ? result.CommitMicroseconds.Average() : 0).ToString("F1", CultureInfo.InvariantCulture)));
            }
        }

        static double Percentile(List<double> values, double percentile)
        {
            var sorted = values.OrderBy(o => o).ToList();
            var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
            return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
        }

        static string GetBuildConfiguration()
        {
#if DEBUG
            return "Debug (use -c Release for meaningful numbers)";
#else
            return "Release";
#endif
        }

        #endregion
    }
}
