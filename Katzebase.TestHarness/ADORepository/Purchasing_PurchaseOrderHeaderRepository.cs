using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Katzebase.Library.Client;
using Katzebase.Library.Payloads;

namespace Katzebase.TestHarness.ADORepository
{
	public partial class Purchasing_PurchaseOrderHeaderRepository
	{        
		public void Export_Purchasing_PurchaseOrderHeader()
		{
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Purchasing:PurchaseOrderHeader"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Purchasing:PurchaseOrderHeader");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Purchasing.PurchaseOrderHeader", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfPurchaseOrderID = dataReader.GetOrdinal("PurchaseOrderID");
						    int indexOfRevisionNumber = dataReader.GetOrdinal("RevisionNumber");
						    int indexOfStatus = dataReader.GetOrdinal("Status");
						    int indexOfEmployeeID = dataReader.GetOrdinal("EmployeeID");
						    int indexOfVendorID = dataReader.GetOrdinal("VendorID");
						    int indexOfShipMethodID = dataReader.GetOrdinal("ShipMethodID");
						    int indexOfOrderDate = dataReader.GetOrdinal("OrderDate");
						    int indexOfShipDate = dataReader.GetOrdinal("ShipDate");
						    int indexOfSubTotal = dataReader.GetOrdinal("SubTotal");
						    int indexOfTaxAmt = dataReader.GetOrdinal("TaxAmt");
						    int indexOfFreight = dataReader.GetOrdinal("Freight");
						    int indexOfTotalDue = dataReader.GetOrdinal("TotalDue");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Purchasing:PurchaseOrderHeader: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Purchasing:PurchaseOrderHeader", new Document(new Models.Purchasing_PurchaseOrderHeader
									{
											PurchaseOrderID= dataReader.GetInt32(indexOfPurchaseOrderID),
											RevisionNumber= dataReader.GetByte(indexOfRevisionNumber),
											Status= dataReader.GetByte(indexOfStatus),
											EmployeeID= dataReader.GetInt32(indexOfEmployeeID),
											VendorID= dataReader.GetInt32(indexOfVendorID),
											ShipMethodID= dataReader.GetInt32(indexOfShipMethodID),
											OrderDate= dataReader.GetDateTime(indexOfOrderDate),
											ShipDate= dataReader.GetNullableDateTime(indexOfShipDate),
											SubTotal= dataReader.GetDecimal(indexOfSubTotal),
											TaxAmt= dataReader.GetDecimal(indexOfTaxAmt),
											Freight= dataReader.GetDecimal(indexOfFreight),
											TotalDue= dataReader.GetDecimal(indexOfTotalDue),
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

