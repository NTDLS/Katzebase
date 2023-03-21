using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Sales_SalesTaxRateRepository
	{        
		public void Export_Sales_SalesTaxRate()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Sales:SalesTaxRate"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesTaxRate");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesTaxRate", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfSalesTaxRateID = dataReader.GetOrdinal("SalesTaxRateID");
						    int indexOfStateProvinceID = dataReader.GetOrdinal("StateProvinceID");
						    int indexOfTaxType = dataReader.GetOrdinal("TaxType");
						    int indexOfTaxRate = dataReader.GetOrdinal("TaxRate");
						    int indexOfName = dataReader.GetOrdinal("Name");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Sales:SalesTaxRate: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Sales:SalesTaxRate", new Document(new Models.Sales_SalesTaxRate
									{
											SalesTaxRateID= dataReader.GetInt32(indexOfSalesTaxRateID),
											StateProvinceID= dataReader.GetInt32(indexOfStateProvinceID),
											TaxType= dataReader.GetByte(indexOfTaxType),
											TaxRate= dataReader.GetDecimal(indexOfTaxRate),
											Name= dataReader.GetString(indexOfName),
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

