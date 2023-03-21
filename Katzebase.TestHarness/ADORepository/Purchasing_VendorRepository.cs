using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Purchasing_VendorRepository
    {
        public void Export_Purchasing_Vendor()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Purchasing:Vendor"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Purchasing:Vendor");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Purchasing.Vendor", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
                            int indexOfAccountNumber = dataReader.GetOrdinal("AccountNumber");
                            int indexOfName = dataReader.GetOrdinal("Name");
                            int indexOfCreditRating = dataReader.GetOrdinal("CreditRating");
                            int indexOfPreferredVendorStatus = dataReader.GetOrdinal("PreferredVendorStatus");
                            int indexOfActiveFlag = dataReader.GetOrdinal("ActiveFlag");
                            int indexOfPurchasingWebServiceURL = dataReader.GetOrdinal("PurchasingWebServiceURL");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Purchasing:Vendor: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Purchasing:Vendor", new Document(new Models.Purchasing_Vendor
                                    {
                                        BusinessEntityID = dataReader.GetInt32(indexOfBusinessEntityID),
                                        AccountNumber = dataReader.GetString(indexOfAccountNumber),
                                        Name = dataReader.GetString(indexOfName),
                                        CreditRating = dataReader.GetByte(indexOfCreditRating),
                                        PreferredVendorStatus = dataReader.GetBoolean(indexOfPreferredVendorStatus),
                                        ActiveFlag = dataReader.GetBoolean(indexOfActiveFlag),
                                        PurchasingWebServiceURL = dataReader.GetNullableString(indexOfPurchasingWebServiceURL),
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

