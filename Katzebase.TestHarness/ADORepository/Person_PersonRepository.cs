using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Person_PersonRepository
    {
        public void Export_Person_Person()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Person:Person"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Person:Person");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Person.Person", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
                            int indexOfPersonType = dataReader.GetOrdinal("PersonType");
                            int indexOfNameStyle = dataReader.GetOrdinal("NameStyle");
                            int indexOfTitle = dataReader.GetOrdinal("Title");
                            int indexOfFirstName = dataReader.GetOrdinal("FirstName");
                            int indexOfMiddleName = dataReader.GetOrdinal("MiddleName");
                            int indexOfLastName = dataReader.GetOrdinal("LastName");
                            int indexOfSuffix = dataReader.GetOrdinal("Suffix");
                            int indexOfEmailPromotion = dataReader.GetOrdinal("EmailPromotion");
                            int indexOfAdditionalContactInfo = dataReader.GetOrdinal("AdditionalContactInfo");
                            int indexOfDemographics = dataReader.GetOrdinal("Demographics");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Person:Person: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Person:Person", new KbDocument(new Models.Person_Person
                                    {
                                        BusinessEntityID = dataReader.GetInt32(indexOfBusinessEntityID),
                                        PersonType = dataReader.GetString(indexOfPersonType),
                                        NameStyle = dataReader.GetBoolean(indexOfNameStyle),
                                        Title = dataReader.GetNullableString(indexOfTitle),
                                        FirstName = dataReader.GetString(indexOfFirstName),
                                        MiddleName = dataReader.GetNullableString(indexOfMiddleName),
                                        LastName = dataReader.GetString(indexOfLastName),
                                        Suffix = dataReader.GetNullableString(indexOfSuffix),
                                        EmailPromotion = dataReader.GetInt32(indexOfEmailPromotion),
                                        AdditionalContactInfo = dataReader.GetNullableString(indexOfAdditionalContactInfo),
                                        Demographics = dataReader.GetNullableString(indexOfDemographics),
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

