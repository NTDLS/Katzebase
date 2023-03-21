using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Production_ProductPhotoRepository
	{        
		public void Export_Production_ProductPhoto()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Production:ProductPhoto"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:ProductPhoto");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Production.ProductPhoto", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfProductPhotoID = dataReader.GetOrdinal("ProductPhotoID");
						    int indexOfThumbNailPhoto = dataReader.GetOrdinal("ThumbNailPhoto");
						    int indexOfThumbnailPhotoFileName = dataReader.GetOrdinal("ThumbnailPhotoFileName");
						    int indexOfLargePhoto = dataReader.GetOrdinal("LargePhoto");
						    int indexOfLargePhotoFileName = dataReader.GetOrdinal("LargePhotoFileName");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Production:ProductPhoto: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Production:ProductPhoto", new Document(new Models.Production_ProductPhoto
									{
											ProductPhotoID= dataReader.GetInt32(indexOfProductPhotoID),
											ThumbNailPhoto= dataReader.GetNullableByteArray(indexOfThumbNailPhoto),
											ThumbnailPhotoFileName= dataReader.GetNullableString(indexOfThumbnailPhotoFileName),
											LargePhoto= dataReader.GetNullableByteArray(indexOfLargePhoto),
											LargePhotoFileName= dataReader.GetNullableString(indexOfLargePhotoFileName),
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

