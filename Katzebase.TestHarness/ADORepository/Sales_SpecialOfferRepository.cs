using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_SpecialOfferRepository
    {
        public void Export_Sales_SpecialOffer()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:SpecialOffer"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SpecialOffer");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SpecialOffer", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfSpecialOfferID = dataReader.GetOrdinal("SpecialOfferID");
                            int indexOfDescription = dataReader.GetOrdinal("Description");
                            int indexOfDiscountPct = dataReader.GetOrdinal("DiscountPct");
                            int indexOfType = dataReader.GetOrdinal("Type");
                            int indexOfCategory = dataReader.GetOrdinal("Category");
                            int indexOfStartDate = dataReader.GetOrdinal("StartDate");
                            int indexOfEndDate = dataReader.GetOrdinal("EndDate");
                            int indexOfMinQty = dataReader.GetOrdinal("MinQty");
                            int indexOfMaxQty = dataReader.GetOrdinal("MaxQty");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:SpecialOffer: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:SpecialOffer", new KbDocument(new Models.Sales_SpecialOffer
                                    {
                                        SpecialOfferID = dataReader.GetInt32(indexOfSpecialOfferID),
                                        Description = dataReader.GetString(indexOfDescription),
                                        DiscountPct = dataReader.GetDecimal(indexOfDiscountPct),
                                        Type = dataReader.GetString(indexOfType),
                                        Category = dataReader.GetString(indexOfCategory),
                                        StartDate = dataReader.GetDateTime(indexOfStartDate),
                                        EndDate = dataReader.GetDateTime(indexOfEndDate),
                                        MinQty = dataReader.GetInt32(indexOfMinQty),
                                        MaxQty = dataReader.GetNullableInt32(indexOfMaxQty),
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

