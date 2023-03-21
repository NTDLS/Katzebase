using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class HumanResources_ShiftRepository
    {
        public void Export_HumanResources_Shift()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:HumanResources:Shift"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:HumanResources:Shift");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM HumanResources.Shift", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfShiftID = dataReader.GetOrdinal("ShiftID");
                            int indexOfName = dataReader.GetOrdinal("Name");
                            int indexOfStartTime = dataReader.GetOrdinal("StartTime");
                            int indexOfEndTime = dataReader.GetOrdinal("EndTime");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:HumanResources:Shift: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:HumanResources:Shift", new Document(new Models.HumanResources_Shift
                                    {
                                        ShiftID = dataReader.GetByte(indexOfShiftID),
                                        Name = dataReader.GetString(indexOfName),
                                        StartTime = dataReader.GetTimeSpan(indexOfStartTime),
                                        EndTime = dataReader.GetTimeSpan(indexOfEndTime),
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

