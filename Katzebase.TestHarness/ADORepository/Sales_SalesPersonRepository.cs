using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_SalesPersonRepository
    {
        public void Export_Sales_SalesPerson()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:SalesPerson"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesPerson");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesPerson", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
                            int indexOfTerritoryID = dataReader.GetOrdinal("TerritoryID");
                            int indexOfSalesQuota = dataReader.GetOrdinal("SalesQuota");
                            int indexOfBonus = dataReader.GetOrdinal("Bonus");
                            int indexOfCommissionPct = dataReader.GetOrdinal("CommissionPct");
                            int indexOfSalesYTD = dataReader.GetOrdinal("SalesYTD");
                            int indexOfSalesLastYear = dataReader.GetOrdinal("SalesLastYear");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:SalesPerson: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:SalesPerson", new Document(new Models.Sales_SalesPerson
                                    {
                                        BusinessEntityID = dataReader.GetInt32(indexOfBusinessEntityID),
                                        TerritoryID = dataReader.GetNullableInt32(indexOfTerritoryID),
                                        SalesQuota = dataReader.GetNullableDecimal(indexOfSalesQuota),
                                        Bonus = dataReader.GetDecimal(indexOfBonus),
                                        CommissionPct = dataReader.GetDecimal(indexOfCommissionPct),
                                        SalesYTD = dataReader.GetDecimal(indexOfSalesYTD),
                                        SalesLastYear = dataReader.GetDecimal(indexOfSalesLastYear),
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

