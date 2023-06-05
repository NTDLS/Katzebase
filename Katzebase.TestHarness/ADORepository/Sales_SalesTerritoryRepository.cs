using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_SalesTerritoryRepository
    {
        public void Export_Sales_SalesTerritory()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:SalesTerritory"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesTerritory");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesTerritory", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfTerritoryID = dataReader.GetOrdinal("TerritoryID");
                            int indexOfName = dataReader.GetOrdinal("Name");
                            int indexOfCountryRegionCode = dataReader.GetOrdinal("CountryRegionCode");
                            int indexOfGroup = dataReader.GetOrdinal("Group");
                            int indexOfSalesYTD = dataReader.GetOrdinal("SalesYTD");
                            int indexOfSalesLastYear = dataReader.GetOrdinal("SalesLastYear");
                            int indexOfCostYTD = dataReader.GetOrdinal("CostYTD");
                            int indexOfCostLastYear = dataReader.GetOrdinal("CostLastYear");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:SalesTerritory: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:SalesTerritory", new KbDocument(new Models.Sales_SalesTerritory
                                    {
                                        TerritoryID = dataReader.GetInt32(indexOfTerritoryID),
                                        Name = dataReader.GetString(indexOfName),
                                        CountryRegionCode = dataReader.GetString(indexOfCountryRegionCode),
                                        Group = dataReader.GetString(indexOfGroup),
                                        SalesYTD = dataReader.GetDecimal(indexOfSalesYTD),
                                        SalesLastYear = dataReader.GetDecimal(indexOfSalesLastYear),
                                        CostYTD = dataReader.GetDecimal(indexOfCostYTD),
                                        CostLastYear = dataReader.GetDecimal(indexOfCostLastYear),
                                        rowguid = dataReader.GetGuid(indexOfrowguid),
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

