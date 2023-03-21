using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Production_ProductCostHistoryRepository
    {
        public void Export_Production_ProductCostHistory()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Production:ProductCostHistory"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:ProductCostHistory");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Production.ProductCostHistory", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfStartDate = dataReader.GetOrdinal("StartDate");
                            int indexOfEndDate = dataReader.GetOrdinal("EndDate");
                            int indexOfStandardCost = dataReader.GetOrdinal("StandardCost");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Production:ProductCostHistory: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Production:ProductCostHistory", new Document(new Models.Production_ProductCostHistory
                                    {
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        StartDate = dataReader.GetDateTime(indexOfStartDate),
                                        EndDate = dataReader.GetNullableDateTime(indexOfEndDate),
                                        StandardCost = dataReader.GetDecimal(indexOfStandardCost),
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

