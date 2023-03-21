using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class dbo_DatabaseLogRepository
	{        
		public void Export_dbo_DatabaseLog()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:dbo:DatabaseLog"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:dbo:DatabaseLog");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM dbo.DatabaseLog", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfDatabaseLogID = dataReader.GetOrdinal("DatabaseLogID");
						    int indexOfPostTime = dataReader.GetOrdinal("PostTime");
						    int indexOfDatabaseUser = dataReader.GetOrdinal("DatabaseUser");
						    int indexOfEvent = dataReader.GetOrdinal("Event");
						    int indexOfSchema = dataReader.GetOrdinal("Schema");
						    int indexOfObject = dataReader.GetOrdinal("Object");
						    int indexOfTSQL = dataReader.GetOrdinal("TSQL");
						    int indexOfXmlEvent = dataReader.GetOrdinal("XmlEvent");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:dbo:DatabaseLog: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:dbo:DatabaseLog", new Document(new Models.dbo_DatabaseLog
									{
											DatabaseLogID= dataReader.GetInt32(indexOfDatabaseLogID),
											PostTime= dataReader.GetDateTime(indexOfPostTime),
											DatabaseUser= dataReader.GetString(indexOfDatabaseUser),
											Event= dataReader.GetString(indexOfEvent),
											Schema= dataReader.GetNullableString(indexOfSchema),
											Object= dataReader.GetNullableString(indexOfObject),
											TSQL= dataReader.GetString(indexOfTSQL),
											XmlEvent= dataReader.GetString(indexOfXmlEvent),
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

