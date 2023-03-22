using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Sales_SalesOrderHeaderSalesReasonRepository
    {
        public void Export_Sales_SalesOrderHeaderSalesReason()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesOrderHeaderSalesReason", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfSalesOrderID = dataReader.GetOrdinal("SalesOrderID");
                            int indexOfSalesReasonID = dataReader.GetOrdinal("SalesReasonID");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason", new KbDocument(new Models.Sales_SalesOrderHeaderSalesReason
                                    {
                                        SalesOrderID = dataReader.GetInt32(indexOfSalesOrderID),
                                        SalesReasonID = dataReader.GetInt32(indexOfSalesReasonID),
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

