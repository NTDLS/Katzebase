using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class dbo_ErrorLogRepository
    {
        public void Export_dbo_ErrorLog()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:dbo:ErrorLog"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:dbo:ErrorLog");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM dbo.ErrorLog", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfErrorLogID = dataReader.GetOrdinal("ErrorLogID");
                            int indexOfErrorTime = dataReader.GetOrdinal("ErrorTime");
                            int indexOfUserName = dataReader.GetOrdinal("UserName");
                            int indexOfErrorNumber = dataReader.GetOrdinal("ErrorNumber");
                            int indexOfErrorSeverity = dataReader.GetOrdinal("ErrorSeverity");
                            int indexOfErrorState = dataReader.GetOrdinal("ErrorState");
                            int indexOfErrorProcedure = dataReader.GetOrdinal("ErrorProcedure");
                            int indexOfErrorLine = dataReader.GetOrdinal("ErrorLine");
                            int indexOfErrorMessage = dataReader.GetOrdinal("ErrorMessage");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:dbo:ErrorLog: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:dbo:ErrorLog", new Document(new Models.dbo_ErrorLog
                                    {
                                        ErrorLogID = dataReader.GetInt32(indexOfErrorLogID),
                                        ErrorTime = dataReader.GetDateTime(indexOfErrorTime),
                                        UserName = dataReader.GetString(indexOfUserName),
                                        ErrorNumber = dataReader.GetInt32(indexOfErrorNumber),
                                        ErrorSeverity = dataReader.GetNullableInt32(indexOfErrorSeverity),
                                        ErrorState = dataReader.GetNullableInt32(indexOfErrorState),
                                        ErrorProcedure = dataReader.GetNullableString(indexOfErrorProcedure),
                                        ErrorLine = dataReader.GetNullableInt32(indexOfErrorLine),
                                        ErrorMessage = dataReader.GetString(indexOfErrorMessage),
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

