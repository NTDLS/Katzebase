using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Production_ProductDocumentRepository
    {
        public void Export_Production_ProductDocument()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Production:ProductDocument"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:ProductDocument");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Production.ProductDocument", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
                            int indexOfDocumentId = dataReader.GetOrdinal("DocumentNode");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Production:ProductDocument: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Production:ProductDocument", new KbDocument(new Models.Production_ProductDocument
                                    {
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        ModifiedDate = dataReader.GetDateTime(indexOfModifiedDate),
                                        DocumentId = dataReader.GetNullableInt32(indexOfDocumentId),
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

