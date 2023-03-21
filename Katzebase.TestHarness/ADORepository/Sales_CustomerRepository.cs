using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Sales_CustomerRepository
	{        
		public void Export_Sales_Customer()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Sales:Customer"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:Customer");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.Customer", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfCustomerID = dataReader.GetOrdinal("CustomerID");
						    int indexOfPersonID = dataReader.GetOrdinal("PersonID");
						    int indexOfStoreID = dataReader.GetOrdinal("StoreID");
						    int indexOfTerritoryID = dataReader.GetOrdinal("TerritoryID");
						    int indexOfAccountNumber = dataReader.GetOrdinal("AccountNumber");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Sales:Customer: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Sales:Customer", new Document(new Models.Sales_Customer
									{
											CustomerID= dataReader.GetInt32(indexOfCustomerID),
											PersonID= dataReader.GetNullableInt32(indexOfPersonID),
											StoreID= dataReader.GetNullableInt32(indexOfStoreID),
											TerritoryID= dataReader.GetNullableInt32(indexOfTerritoryID),
											AccountNumber= dataReader.GetString(indexOfAccountNumber),
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

