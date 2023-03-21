using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Production_WorkOrderRoutingRepository
	{        
		public void Export_Production_WorkOrderRouting()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Production:WorkOrderRouting"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:WorkOrderRouting");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Production.WorkOrderRouting", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfWorkOrderID = dataReader.GetOrdinal("WorkOrderID");
						    int indexOfProductID = dataReader.GetOrdinal("ProductID");
						    int indexOfOperationSequence = dataReader.GetOrdinal("OperationSequence");
						    int indexOfLocationID = dataReader.GetOrdinal("LocationID");
						    int indexOfScheduledStartDate = dataReader.GetOrdinal("ScheduledStartDate");
						    int indexOfScheduledEndDate = dataReader.GetOrdinal("ScheduledEndDate");
						    int indexOfActualStartDate = dataReader.GetOrdinal("ActualStartDate");
						    int indexOfActualEndDate = dataReader.GetOrdinal("ActualEndDate");
						    int indexOfActualResourceHrs = dataReader.GetOrdinal("ActualResourceHrs");
						    int indexOfPlannedCost = dataReader.GetOrdinal("PlannedCost");
						    int indexOfActualCost = dataReader.GetOrdinal("ActualCost");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Production:WorkOrderRouting: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Production:WorkOrderRouting", new Document(new Models.Production_WorkOrderRouting
									{
											WorkOrderID= dataReader.GetInt32(indexOfWorkOrderID),
											ProductID= dataReader.GetInt32(indexOfProductID),
											OperationSequence= dataReader.GetInt16(indexOfOperationSequence),
											LocationID= dataReader.GetInt16(indexOfLocationID),
											ScheduledStartDate= dataReader.GetDateTime(indexOfScheduledStartDate),
											ScheduledEndDate= dataReader.GetDateTime(indexOfScheduledEndDate),
											ActualStartDate= dataReader.GetNullableDateTime(indexOfActualStartDate),
											ActualEndDate= dataReader.GetNullableDateTime(indexOfActualEndDate),
											ActualResourceHrs= dataReader.GetNullableDecimal(indexOfActualResourceHrs),
											PlannedCost= dataReader.GetDecimal(indexOfPlannedCost),
											ActualCost= dataReader.GetNullableDecimal(indexOfActualCost),
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

