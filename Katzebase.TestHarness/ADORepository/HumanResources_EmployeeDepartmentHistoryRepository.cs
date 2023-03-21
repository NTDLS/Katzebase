using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class HumanResources_EmployeeDepartmentHistoryRepository
	{        
		public void Export_HumanResources_EmployeeDepartmentHistory()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM HumanResources.EmployeeDepartmentHistory", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
						    int indexOfDepartmentID = dataReader.GetOrdinal("DepartmentID");
						    int indexOfShiftID = dataReader.GetOrdinal("ShiftID");
						    int indexOfStartDate = dataReader.GetOrdinal("StartDate");
						    int indexOfEndDate = dataReader.GetOrdinal("EndDate");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", new Document(new Models.HumanResources_EmployeeDepartmentHistory
									{
											BusinessEntityID= dataReader.GetInt32(indexOfBusinessEntityID),
											DepartmentID= dataReader.GetInt16(indexOfDepartmentID),
											ShiftID= dataReader.GetByte(indexOfShiftID),
											StartDate= dataReader.GetDateTime(indexOfStartDate),
											EndDate= dataReader.GetNullableDateTime(indexOfEndDate),
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

