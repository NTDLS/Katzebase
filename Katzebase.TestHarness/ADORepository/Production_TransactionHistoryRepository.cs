using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Production_TransactionHistoryRepository
    {
        public void Export_Production_TransactionHistory()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Production:TransactionHistory"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:TransactionHistory");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Production.TransactionHistory", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfTransactionID = dataReader.GetOrdinal("TransactionID");
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfReferenceOrderID = dataReader.GetOrdinal("ReferenceOrderID");
                            int indexOfReferenceOrderLineID = dataReader.GetOrdinal("ReferenceOrderLineID");
                            int indexOfTransactionDate = dataReader.GetOrdinal("TransactionDate");
                            int indexOfTransactionType = dataReader.GetOrdinal("TransactionType");
                            int indexOfQuantity = dataReader.GetOrdinal("Quantity");
                            int indexOfActualCost = dataReader.GetOrdinal("ActualCost");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Production:TransactionHistory: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Production:TransactionHistory", new KbDocument(new Models.Production_TransactionHistory
                                    {
                                        TransactionID = dataReader.GetInt32(indexOfTransactionID),
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        ReferenceOrderID = dataReader.GetInt32(indexOfReferenceOrderID),
                                        ReferenceOrderLineID = dataReader.GetInt32(indexOfReferenceOrderLineID),
                                        TransactionDate = dataReader.GetDateTime(indexOfTransactionDate),
                                        TransactionType = dataReader.GetString(indexOfTransactionType),
                                        Quantity = dataReader.GetInt32(indexOfQuantity),
                                        ActualCost = dataReader.GetDecimal(indexOfActualCost),
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

