using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_CurrencyRateRepository
    {
        public void Export_Sales_CurrencyRate()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:CurrencyRate"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:CurrencyRate");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.CurrencyRate", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfCurrencyRateID = dataReader.GetOrdinal("CurrencyRateID");
                            int indexOfCurrencyRateDate = dataReader.GetOrdinal("CurrencyRateDate");
                            int indexOfFromCurrencyCode = dataReader.GetOrdinal("FromCurrencyCode");
                            int indexOfToCurrencyCode = dataReader.GetOrdinal("ToCurrencyCode");
                            int indexOfAverageRate = dataReader.GetOrdinal("AverageRate");
                            int indexOfEndOfDayRate = dataReader.GetOrdinal("EndOfDayRate");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:CurrencyRate: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:CurrencyRate", new KbDocument(new Models.Sales_CurrencyRate
                                    {
                                        CurrencyRateID = dataReader.GetInt32(indexOfCurrencyRateID),
                                        CurrencyRateDate = dataReader.GetDateTime(indexOfCurrencyRateDate),
                                        FromCurrencyCode = dataReader.GetString(indexOfFromCurrencyCode),
                                        ToCurrencyCode = dataReader.GetString(indexOfToCurrencyCode),
                                        AverageRate = dataReader.GetDecimal(indexOfAverageRate),
                                        EndOfDayRate = dataReader.GetDecimal(indexOfEndOfDayRate),
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

