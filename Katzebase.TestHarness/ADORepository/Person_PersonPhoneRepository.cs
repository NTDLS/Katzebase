using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Person_PersonPhoneRepository
    {
        public void Export_Person_PersonPhone()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Person:PersonPhone"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Person:PersonPhone");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Person.PersonPhone", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
                            int indexOfPhoneNumber = dataReader.GetOrdinal("PhoneNumber");
                            int indexOfPhoneNumberTypeID = dataReader.GetOrdinal("PhoneNumberTypeID");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Person:PersonPhone: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Person:PersonPhone", new Document(new Models.Person_PersonPhone
                                    {
                                        BusinessEntityID = dataReader.GetInt32(indexOfBusinessEntityID),
                                        PhoneNumber = dataReader.GetString(indexOfPhoneNumber),
                                        PhoneNumberTypeID = dataReader.GetInt32(indexOfPhoneNumberTypeID),
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

