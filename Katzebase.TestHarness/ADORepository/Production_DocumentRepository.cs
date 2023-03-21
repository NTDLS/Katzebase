using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Production_DocumentRepository
	{        
		public void Export_Production_Document()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Production:Document"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:Document");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Production.Document", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfId = dataReader.GetOrdinal("Id");
						    int indexOfTitle = dataReader.GetOrdinal("Title");
						    int indexOfOwner = dataReader.GetOrdinal("Owner");
						    int indexOfFolderFlag = dataReader.GetOrdinal("FolderFlag");
						    int indexOfFileName = dataReader.GetOrdinal("FileName");
						    int indexOfFileExtension = dataReader.GetOrdinal("FileExtension");
						    int indexOfRevision = dataReader.GetOrdinal("Revision");
						    int indexOfChangeNumber = dataReader.GetOrdinal("ChangeNumber");
						    int indexOfStatus = dataReader.GetOrdinal("Status");
						    int indexOfDocumentSummary = dataReader.GetOrdinal("DocumentSummary");
						    int indexOfDocument = dataReader.GetOrdinal("Document");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Production:Document: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Production:Document", new Document(new Models.Production_Document
									{
											Id= dataReader.GetInt32(indexOfId),
											Title= dataReader.GetString(indexOfTitle),
											Owner= dataReader.GetInt32(indexOfOwner),
											FolderFlag= dataReader.GetBoolean(indexOfFolderFlag),
											FileName= dataReader.GetString(indexOfFileName),
											FileExtension= dataReader.GetString(indexOfFileExtension),
											Revision= dataReader.GetString(indexOfRevision),
											ChangeNumber= dataReader.GetInt32(indexOfChangeNumber),
											Status= dataReader.GetByte(indexOfStatus),
											DocumentSummary= dataReader.GetNullableString(indexOfDocumentSummary),
											Document= dataReader.GetNullableByteArray(indexOfDocument),
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

