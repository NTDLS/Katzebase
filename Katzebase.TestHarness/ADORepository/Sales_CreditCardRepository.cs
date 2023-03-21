using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_CreditCardRepository
    {
        public void Export_Sales_CreditCard()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:CreditCard"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:CreditCard");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.CreditCard", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfCreditCardID = dataReader.GetOrdinal("CreditCardID");
                            int indexOfCardType = dataReader.GetOrdinal("CardType");
                            int indexOfCardNumber = dataReader.GetOrdinal("CardNumber");
                            int indexOfExpMonth = dataReader.GetOrdinal("ExpMonth");
                            int indexOfExpYear = dataReader.GetOrdinal("ExpYear");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:CreditCard: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:CreditCard", new Document(new Models.Sales_CreditCard
                                    {
                                        CreditCardID = dataReader.GetInt32(indexOfCreditCardID),
                                        CardType = dataReader.GetString(indexOfCardType),
                                        CardNumber = dataReader.GetString(indexOfCardNumber),
                                        ExpMonth = dataReader.GetByte(indexOfExpMonth),
                                        ExpYear = dataReader.GetInt16(indexOfExpYear),
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

