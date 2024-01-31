using NTDLS.Katzebase.Client;
using NTDLS.Katzebase.Client.Payloads;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO.Compression;
using System.Text;

namespace TestHarness
{
    public static partial class SqlServerExporter
    {
        private static List<double> PerfStasMetricsForAvg = new();
        private static int PerfStasRowsExported = 0;
        private static DateTime LastPerfStatDateTime = DateTime.MinValue;
        private static object PerfStasLockObj = new object();

        public static void ExportSQLServerDatabaseToKatzebase(string sqlServer, string sqlServerDatabase, string katzeBaseServerHost, int katzeBaseServerPort, bool omitSQLSchemaName)
        {
            using (var connection = new SqlConnection($"Server={sqlServer};Database={sqlServerDatabase};Trusted_Connection=True;"))
            {
                connection.Open();

                string tSQL = string.Empty;

                if (omitSQLSchemaName)
                {
                    tSQL = "select '[' + name + ']' as ObjectName from sys.tables where type = 'u' order by OBJECT_SCHEMA_NAME(object_id) + '.' + name";
                }
                else
                {
                    tSQL = "select '[' + OBJECT_SCHEMA_NAME(object_id) + '].[' + name + ']' as ObjectName from sys.tables where type = 'u' order by OBJECT_SCHEMA_NAME(object_id) + '.' + name";
                }

                using (var command = new SqlCommand(tSQL, connection))
                {
                    using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                    {
                        while (dataReader.Read() /*&& rowCount++ < 10000*/)
                        {
                            //ExportSQLServerTableToFile(sqlServer, sqlServerDatabase, $"{dataReader["ObjectName"]}", "../../../Outputdata.gz");
                            ExportSQLServerTableToKatzebase(sqlServer, sqlServerDatabase, $"{dataReader["ObjectName"]}", katzeBaseServerHost, katzeBaseServerPort);
                        }
                    }
                }

                connection.Close();
            }
        }

        public static void ExportSQLServerTableToFile(string sqlServer, string sqlServerDatabase, string sqlServerTable, string fileName)
        {
            using (var connection = new SqlConnection($"Server={sqlServer};Database={sqlServerDatabase};Trusted_Connection=True;"))
            {
                connection.Open();

                var rows = new List<Dictionary<string, string>>();

                try
                {
                    using (var command = new SqlCommand($"SELECT top 100000 * FROM {sqlServerTable}", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            while (dataReader.Read())
                            {
                                var row = new Dictionary<string, string>();

                                for (int iField = 0; iField < dataReader.FieldCount; iField++)
                                {
                                    var dataType = dataReader.GetFieldType(iField);
                                    if (dataType != null)
                                    {
                                        row.Add(dataReader.GetName(iField), dataReader[iField]?.ToString()?.Trim() ?? "");
                                    }
                                }
                                rows.Add(row);
                            }
                        }
                    }

                    var dataJson = Compress(Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(rows)));

                    File.WriteAllBytes(fileName, dataJson);

                    byte[] Compress(byte[] bytes)
                    {
                        using var msi = new MemoryStream(bytes);
                        using var mso = new MemoryStream();
                        using (var gs = new GZipStream(mso, CompressionLevel.SmallestSize))
                        {
                            msi.CopyTo(gs);
                        }
                        return mso.ToArray();
                    }

                    connection.Close();
                }
                catch
                {
                    //TODO: add error handling/logging
                    throw;
                }
            }
        }

        public static void ExportSQLServerTableToKatzebase(string sqlServer, string sqlServerDatabase, string sqlServerTable, string katzeBaseServerHost, int katzeBaseServerPort)
        {
            int rowsPerTransaction = 10000;

            if (System.Diagnostics.Debugger.IsAttached)
            {
                rowsPerTransaction = 100;
            }

            using var client = new KbClient(katzeBaseServerHost, katzeBaseServerPort);

            string kbSchema = $"{sqlServerDatabase}:{sqlServerTable.Replace("[", "").Replace("]", "").Replace("dbo.", "").Replace('.', ':')}";

            if (client.Schema.Exists(kbSchema))
            {
                return;
            }

            client.Schema.Create(kbSchema);

            client.Transaction.Begin();

            using (SqlConnection connection = new SqlConnection($"Server={sqlServer};Database={sqlServerDatabase};Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (var command = new SqlCommand($"SELECT * FROM {sqlServerTable}", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int rowCount = 0;

                            while (dataReader.Read())
                            {
                                var dbObject = new ExpandoObject() as IDictionary<string, object>;

                                for (int iField = 0; iField < dataReader.FieldCount; iField++)
                                {
                                    var dataType = dataReader.GetFieldType(iField);
                                    if (dataType != null)
                                    {
                                        dbObject.Add(dataReader.GetName(iField), dataReader[iField]?.ToString()?.Trim() ?? "");
                                    }
                                }

                                if ((DateTime.Now - LastPerfStatDateTime).TotalMilliseconds > 1000)
                                {
                                    lock (PerfStasLockObj)
                                    {
                                        double totalMilliseconds = (DateTime.Now - LastPerfStatDateTime).TotalMilliseconds;
                                        if (totalMilliseconds > 1000) //Make sure we got the lock.
                                        {
                                            double rps = PerfStasRowsExported / (totalMilliseconds / 1000);
                                            PerfStasMetricsForAvg.Add(rps);
                                            Console.Write($" Current RPS: {rps:n2}/s, Avg RPS: {PerfStasMetricsForAvg.Average(o => o):n2}, Total Rows: {rowCount:n0}   \r");

                                            while (PerfStasMetricsForAvg.Count > 25)
                                            {
                                                PerfStasMetricsForAvg.Remove(PerfStasMetricsForAvg.First());
                                            }

                                            LastPerfStatDateTime = DateTime.Now;
                                            PerfStasRowsExported = 0;
                                        }
                                    }
                                }

                                if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                                {
                                    //Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store(kbSchema, new KbDocument(dbObject));
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }

                                PerfStasRowsExported++;
                                rowCount++;
                            }
                        }
                    }
                    connection.Close();
                }
                catch
                {
                    //TODO: add error handling/logging
                    throw;
                }

                client.Transaction.Commit();
            }
        }

        public static void ExportSQLServerTableToKatzebase(string sqlServer, string sqlServerDatabase, string sqlServerTable, string katzeBaseServerHost, int katzeBaseServerPort, string targetSchema)
        {
            try
            {
                int rowsPerTransaction = 10000;

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    rowsPerTransaction = 100;
                }

                targetSchema = targetSchema.Replace("[TABLE_NAME]", sqlServerTable);

                using var client = new KbClient(katzeBaseServerHost, katzeBaseServerPort);

                client.Schema.CreateRecursive(targetSchema);

                client.Transaction.Begin();

                using (SqlConnection connection = new SqlConnection($"Server={sqlServer};Database={sqlServerDatabase};Trusted_Connection=True;"))
                {
                    connection.Open();

                    try
                    {
                        using (var command = new SqlCommand($"SELECT * FROM {sqlServerTable}", connection))
                        {
                            command.CommandTimeout = 10000;
                            command.CommandType = System.Data.CommandType.Text;

                            using (var dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                            {
                                int rowCount = 0;

                                while (dataReader.Read())
                                {
                                    var dbObject = new ExpandoObject() as IDictionary<string, object>;

                                    for (int iField = 0; iField < dataReader.FieldCount; iField++)
                                    {
                                        var dataType = dataReader.GetFieldType(iField);
                                        if (dataType != null)
                                        {
                                            dbObject.Add(dataReader.GetName(iField), dataReader[iField]?.ToString()?.Trim() ?? "");
                                        }
                                    }

                                    if ((DateTime.Now - LastPerfStatDateTime).TotalMilliseconds > 1000)
                                    {
                                        lock (PerfStasLockObj)
                                        {
                                            double totalMilliseconds = (DateTime.Now - LastPerfStatDateTime).TotalMilliseconds;
                                            if (totalMilliseconds > 1000) //Make sure we got the lock.
                                            {
                                                double rps = PerfStasRowsExported / (totalMilliseconds / 1000);
                                                PerfStasMetricsForAvg.Add(rps);
                                                Console.Write($" Current RPS: {rps:n2}/s, Avg RPS: {PerfStasMetricsForAvg.Average(o => o):n2}, Total Rows: {rowCount:n0}   \r");

                                                while (PerfStasMetricsForAvg.Count > 25)
                                                {
                                                    PerfStasMetricsForAvg.Remove(PerfStasMetricsForAvg.First());
                                                }

                                                LastPerfStatDateTime = DateTime.Now;
                                                PerfStasRowsExported = 0;
                                            }
                                        }
                                    }

                                    if (rowCount > 0 && (rowCount % rowsPerTransaction) == 0)
                                    {
                                        //Console.WriteLine("Comitting...");
                                        client.Transaction.Commit();
                                        client.Transaction.Begin();
                                    }

                                    try
                                    {
                                        client.Document.Store(targetSchema, new KbDocument(dbObject));
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }

                                    PerfStasRowsExported++;
                                    rowCount++;
                                }
                            }
                        }
                        connection.Close();
                    }
                    catch
                    {
                        //TODO: add error handling/logging
                        throw;
                    }

                    client.Transaction.Commit();
                }
            }
            catch
            {
            }
        }

    }
}

