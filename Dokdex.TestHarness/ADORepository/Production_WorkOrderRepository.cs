using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Dokdex.Library.Client;
using Dokdex.Library.Payloads;

namespace Dokdex.TestHarness.ADORepository
{
	public partial class Production_WorkOrderRepository
	{        
		public void Export_Production_WorkOrder()
		{
            DokdexClient client = new DokdexClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Production:WorkOrder"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:WorkOrder");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Production.WorkOrder", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfWorkOrderID = dataReader.GetOrdinal("WorkOrderID");
						    int indexOfProductID = dataReader.GetOrdinal("ProductID");
						    int indexOfOrderQty = dataReader.GetOrdinal("OrderQty");
						    int indexOfStockedQty = dataReader.GetOrdinal("StockedQty");
						    int indexOfScrappedQty = dataReader.GetOrdinal("ScrappedQty");
						    int indexOfStartDate = dataReader.GetOrdinal("StartDate");
						    int indexOfEndDate = dataReader.GetOrdinal("EndDate");
						    int indexOfDueDate = dataReader.GetOrdinal("DueDate");
						    int indexOfScrapReasonID = dataReader.GetOrdinal("ScrapReasonID");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Production:WorkOrder: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Production:WorkOrder", new Document(new Models.Production_WorkOrder
									{
											WorkOrderID= dataReader.GetInt32(indexOfWorkOrderID),
											ProductID= dataReader.GetInt32(indexOfProductID),
											OrderQty= dataReader.GetInt32(indexOfOrderQty),
											StockedQty= dataReader.GetInt32(indexOfStockedQty),
											ScrappedQty= dataReader.GetInt16(indexOfScrappedQty),
											StartDate= dataReader.GetDateTime(indexOfStartDate),
											EndDate= dataReader.GetNullableDateTime(indexOfEndDate),
											DueDate= dataReader.GetDateTime(indexOfDueDate),
											ScrapReasonID= dataReader.GetNullableInt16(indexOfScrapReasonID),
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

