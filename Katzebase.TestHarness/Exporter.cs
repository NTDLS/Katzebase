using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;
using System.Dynamic;

namespace Katzebase.TestHarness
{
    public static partial class Exporter
    {
        public static void ExportSQLServerDatabaseToKatzebase(string sqlServer, string sqlServerDatabase, string katzeBaseServerAdddress, bool omitSQLSchemaName)
        {
            using (SqlConnection connection = new SqlConnection($"Server={sqlServer};Database={sqlServerDatabase};Trusted_Connection=True;"))
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
                            ExportSQLServerTableToKatzebase(sqlServer, sqlServerDatabase, $"{dataReader["ObjectName"]}", katzeBaseServerAdddress);
                        }
                    }
                }

                connection.Close();
            }
        }

        public static void ExportSQLServerTableToKatzebase(string sqlServer, string sqlServerDatabase, string sqlServerTable, string katzeBaseServerAdddress)
        {
            var client = new KatzebaseClient(katzeBaseServerAdddress);

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

                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine($"{kbSchema}: {rowCount}");
                                }

                                if (rowCount > 0 && (rowCount % 10000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
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
    }
}

