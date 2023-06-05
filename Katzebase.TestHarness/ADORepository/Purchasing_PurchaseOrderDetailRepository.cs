using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Purchasing_PurchaseOrderDetailRepository
    {
        public void Export_Purchasing_PurchaseOrderDetail()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Purchasing:PurchaseOrderDetail"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Purchasing:PurchaseOrderDetail");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Purchasing.PurchaseOrderDetail", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfPurchaseOrderID = dataReader.GetOrdinal("PurchaseOrderID");
                            int indexOfPurchaseOrderDetailID = dataReader.GetOrdinal("PurchaseOrderDetailID");
                            int indexOfDueDate = dataReader.GetOrdinal("DueDate");
                            int indexOfOrderQty = dataReader.GetOrdinal("OrderQty");
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfUnitPrice = dataReader.GetOrdinal("UnitPrice");
                            int indexOfLineTotal = dataReader.GetOrdinal("LineTotal");
                            int indexOfReceivedQty = dataReader.GetOrdinal("ReceivedQty");
                            int indexOfRejectedQty = dataReader.GetOrdinal("RejectedQty");
                            int indexOfStockedQty = dataReader.GetOrdinal("StockedQty");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Purchasing:PurchaseOrderDetail: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Purchasing:PurchaseOrderDetail", new KbDocument(new Models.Purchasing_PurchaseOrderDetail
                                    {
                                        PurchaseOrderID = dataReader.GetInt32(indexOfPurchaseOrderID),
                                        PurchaseOrderDetailID = dataReader.GetInt32(indexOfPurchaseOrderDetailID),
                                        DueDate = dataReader.GetDateTime(indexOfDueDate),
                                        OrderQty = dataReader.GetInt16(indexOfOrderQty),
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        UnitPrice = dataReader.GetDecimal(indexOfUnitPrice),
                                        LineTotal = dataReader.GetDecimal(indexOfLineTotal),
                                        ReceivedQty = dataReader.GetDecimal(indexOfReceivedQty),
                                        RejectedQty = dataReader.GetDecimal(indexOfRejectedQty),
                                        StockedQty = dataReader.GetDecimal(indexOfStockedQty),
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

