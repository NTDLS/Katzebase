using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Person_StateProvinceRepository
	{        
		public void Export_Person_StateProvince()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Person:StateProvince"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Person:StateProvince");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Person.StateProvince", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfStateProvinceID = dataReader.GetOrdinal("StateProvinceID");
						    int indexOfStateProvinceCode = dataReader.GetOrdinal("StateProvinceCode");
						    int indexOfCountryRegionCode = dataReader.GetOrdinal("CountryRegionCode");
						    int indexOfIsOnlyStateProvinceFlag = dataReader.GetOrdinal("IsOnlyStateProvinceFlag");
						    int indexOfName = dataReader.GetOrdinal("Name");
						    int indexOfTerritoryID = dataReader.GetOrdinal("TerritoryID");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Person:StateProvince: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Person:StateProvince", new Document(new Models.Person_StateProvince
									{
											StateProvinceID= dataReader.GetInt32(indexOfStateProvinceID),
											StateProvinceCode= dataReader.GetString(indexOfStateProvinceCode),
											CountryRegionCode= dataReader.GetString(indexOfCountryRegionCode),
											IsOnlyStateProvinceFlag= dataReader.GetBoolean(indexOfIsOnlyStateProvinceFlag),
											Name= dataReader.GetString(indexOfName),
											TerritoryID= dataReader.GetInt32(indexOfTerritoryID),
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

