using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_SalesOrderDetailRepository
    {
        public void Export_Sales_SalesOrderDetail()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:SalesOrderDetail"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesOrderDetail");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesOrderDetail", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfSalesOrderID = dataReader.GetOrdinal("SalesOrderID");
                            int indexOfSalesOrderDetailID = dataReader.GetOrdinal("SalesOrderDetailID");
                            int indexOfCarrierTrackingNumber = dataReader.GetOrdinal("CarrierTrackingNumber");
                            int indexOfOrderQty = dataReader.GetOrdinal("OrderQty");
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfSpecialOfferID = dataReader.GetOrdinal("SpecialOfferID");
                            int indexOfUnitPrice = dataReader.GetOrdinal("UnitPrice");
                            int indexOfUnitPriceDiscount = dataReader.GetOrdinal("UnitPriceDiscount");
                            int indexOfLineTotal = dataReader.GetOrdinal("LineTotal");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:SalesOrderDetail: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:SalesOrderDetail", new Document(new Models.Sales_SalesOrderDetail
                                    {
                                        SalesOrderID = dataReader.GetInt32(indexOfSalesOrderID),
                                        SalesOrderDetailID = dataReader.GetInt32(indexOfSalesOrderDetailID),
                                        CarrierTrackingNumber = dataReader.GetNullableString(indexOfCarrierTrackingNumber),
                                        OrderQty = dataReader.GetInt16(indexOfOrderQty),
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        SpecialOfferID = dataReader.GetInt32(indexOfSpecialOfferID),
                                        UnitPrice = dataReader.GetDecimal(indexOfUnitPrice),
                                        UnitPriceDiscount = dataReader.GetDecimal(indexOfUnitPriceDiscount),
                                        LineTotal = dataReader.GetDecimal(indexOfLineTotal),
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

