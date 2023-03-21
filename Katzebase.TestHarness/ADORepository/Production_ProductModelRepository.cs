using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Production_ProductModelRepository
	{        
		public void Export_Production_ProductModel()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Production:ProductModel"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:ProductModel");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Production.ProductModel", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfProductModelID = dataReader.GetOrdinal("ProductModelID");
						    int indexOfName = dataReader.GetOrdinal("Name");
						    int indexOfCatalogDescription = dataReader.GetOrdinal("CatalogDescription");
						    int indexOfInstructions = dataReader.GetOrdinal("Instructions");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Production:ProductModel: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Production:ProductModel", new Document(new Models.Production_ProductModel
									{
											ProductModelID= dataReader.GetInt32(indexOfProductModelID),
											Name= dataReader.GetString(indexOfName),
											CatalogDescription= dataReader.GetNullableString(indexOfCatalogDescription),
											Instructions= dataReader.GetNullableString(indexOfInstructions),
											rowguid= dataReader.GetGuid(indexOfrowguid),
											ModifiedDate= dataReader.GetDateTime(indexOfModifiedDate),
										}));
								}
								catch(Exception ex)
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

