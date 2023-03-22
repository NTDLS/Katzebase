using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class dbo_AWBuildVersionRepository
    {
        public void Export_dbo_AWBuildVersion()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:dbo:AWBuildVersion"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:dbo:AWBuildVersion");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM dbo.AWBuildVersion", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfSystemInformationID = dataReader.GetOrdinal("SystemInformationID");
                            int indexOfDatabase_Version = dataReader.GetOrdinal("Database Version");
                            int indexOfVersionDate = dataReader.GetOrdinal("VersionDate");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:dbo:AWBuildVersion: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:dbo:AWBuildVersion", new KbDocument(new Models.dbo_AWBuildVersion
                                    {
                                        SystemInformationID = dataReader.GetByte(indexOfSystemInformationID),
                                        Database_Version = dataReader.GetString(indexOfDatabase_Version),
                                        VersionDate = dataReader.GetDateTime(indexOfVersionDate),
                                        ModifiedDate = dataReader.GetDateTime(indexOfModifiedDate),
                                    }));
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

