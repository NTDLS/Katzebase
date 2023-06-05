using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Person_AddressRepository
    {
        public void Export_Person_Address()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Person:Address"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Person:Address");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Person.Address", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfAddressID = dataReader.GetOrdinal("AddressID");
                            int indexOfAddressLine1 = dataReader.GetOrdinal("AddressLine1");
                            int indexOfAddressLine2 = dataReader.GetOrdinal("AddressLine2");
                            int indexOfCity = dataReader.GetOrdinal("City");
                            int indexOfStateProvinceID = dataReader.GetOrdinal("StateProvinceID");
                            int indexOfPostalCode = dataReader.GetOrdinal("PostalCode");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Person:Address: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Person:Address", new KbDocument(new Models.Person_Address
                                    {
                                        AddressID = dataReader.GetInt32(indexOfAddressID),
                                        AddressLine1 = dataReader.GetString(indexOfAddressLine1),
                                        AddressLine2 = dataReader.GetNullableString(indexOfAddressLine2),
                                        City = dataReader.GetString(indexOfCity),
                                        StateProvinceID = dataReader.GetInt32(indexOfStateProvinceID),
                                        PostalCode = dataReader.GetString(indexOfPostalCode),
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

