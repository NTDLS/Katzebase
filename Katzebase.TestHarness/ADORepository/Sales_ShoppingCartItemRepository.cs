using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_ShoppingCartItemRepository
    {
        public void Export_Sales_ShoppingCartItem()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:ShoppingCartItem"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:ShoppingCartItem");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.ShoppingCartItem", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfShoppingCartItemID = dataReader.GetOrdinal("ShoppingCartItemID");
                            int indexOfShoppingCartID = dataReader.GetOrdinal("ShoppingCartID");
                            int indexOfQuantity = dataReader.GetOrdinal("Quantity");
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfDateCreated = dataReader.GetOrdinal("DateCreated");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:ShoppingCartItem: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:ShoppingCartItem", new KbDocument(new Models.Sales_ShoppingCartItem
                                    {
                                        ShoppingCartItemID = dataReader.GetInt32(indexOfShoppingCartItemID),
                                        ShoppingCartID = dataReader.GetString(indexOfShoppingCartID),
                                        Quantity = dataReader.GetInt32(indexOfQuantity),
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        DateCreated = dataReader.GetDateTime(indexOfDateCreated),
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

