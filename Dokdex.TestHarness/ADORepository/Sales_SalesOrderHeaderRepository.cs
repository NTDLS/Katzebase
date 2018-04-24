using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Dokdex.Library.Client;
using Dokdex.Library.Payloads;

namespace Dokdex.TestHarness.ADORepository
{
	public partial class Sales_SalesOrderHeaderRepository
	{        
		public void Export_Sales_SalesOrderHeader()
		{
            DokdexClient client = new DokdexClient("http://localhost:6858/");

            if(client.Schema.Exists("AdventureWorks2012:Sales:SalesOrderHeader"))
			{
				return;
			}

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Sales:SalesOrderHeader");

			using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
			{
				connection.Open();

				try
				{
					using (SqlCommand command = new SqlCommand("SELECT * FROM Sales.SalesOrderHeader", connection))
					{
						command.CommandTimeout = 10000;
						command.CommandType = System.Data.CommandType.Text;

						using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
						{
                            int indexOfSalesOrderID = dataReader.GetOrdinal("SalesOrderID");
						    int indexOfRevisionNumber = dataReader.GetOrdinal("RevisionNumber");
						    int indexOfOrderDate = dataReader.GetOrdinal("OrderDate");
						    int indexOfDueDate = dataReader.GetOrdinal("DueDate");
						    int indexOfShipDate = dataReader.GetOrdinal("ShipDate");
						    int indexOfStatus = dataReader.GetOrdinal("Status");
						    int indexOfOnlineOrderFlag = dataReader.GetOrdinal("OnlineOrderFlag");
						    int indexOfSalesOrderNumber = dataReader.GetOrdinal("SalesOrderNumber");
						    int indexOfPurchaseOrderNumber = dataReader.GetOrdinal("PurchaseOrderNumber");
						    int indexOfAccountNumber = dataReader.GetOrdinal("AccountNumber");
						    int indexOfCustomerID = dataReader.GetOrdinal("CustomerID");
						    int indexOfSalesPersonID = dataReader.GetOrdinal("SalesPersonID");
						    int indexOfTerritoryID = dataReader.GetOrdinal("TerritoryID");
						    int indexOfBillToAddressID = dataReader.GetOrdinal("BillToAddressID");
						    int indexOfShipToAddressID = dataReader.GetOrdinal("ShipToAddressID");
						    int indexOfShipMethodID = dataReader.GetOrdinal("ShipMethodID");
						    int indexOfCreditCardID = dataReader.GetOrdinal("CreditCardID");
						    int indexOfCreditCardApprovalCode = dataReader.GetOrdinal("CreditCardApprovalCode");
						    int indexOfCurrencyRateID = dataReader.GetOrdinal("CurrencyRateID");
						    int indexOfSubTotal = dataReader.GetOrdinal("SubTotal");
						    int indexOfTaxAmt = dataReader.GetOrdinal("TaxAmt");
						    int indexOfFreight = dataReader.GetOrdinal("Freight");
						    int indexOfTotalDue = dataReader.GetOrdinal("TotalDue");
						    int indexOfComment = dataReader.GetOrdinal("Comment");
						    int indexOfrowguid = dataReader.GetOrdinal("rowguid");
						    int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");
						    
							int rowCount = 0;


							while (dataReader.Read() /*&& rowCount++ < 10000*/)
							{
								if(rowCount > 0 && (rowCount % 100) == 0)
								{
									Console.WriteLine("AdventureWorks2012:Sales:SalesOrderHeader: {0}", rowCount);
								}

								if(rowCount > 0 && (rowCount % 1000) == 0)
								{
									Console.WriteLine("Comitting...");
									client.Transaction.Commit();
									client.Transaction.Begin();
								}

								try
								{
									client.Document.Store("AdventureWorks2012:Sales:SalesOrderHeader", new Document(new Models.Sales_SalesOrderHeader
									{
											SalesOrderID= dataReader.GetInt32(indexOfSalesOrderID),
											RevisionNumber= dataReader.GetByte(indexOfRevisionNumber),
											OrderDate= dataReader.GetDateTime(indexOfOrderDate),
											DueDate= dataReader.GetDateTime(indexOfDueDate),
											ShipDate= dataReader.GetNullableDateTime(indexOfShipDate),
											Status= dataReader.GetByte(indexOfStatus),
											OnlineOrderFlag= dataReader.GetBoolean(indexOfOnlineOrderFlag),
											SalesOrderNumber= dataReader.GetString(indexOfSalesOrderNumber),
											PurchaseOrderNumber= dataReader.GetNullableString(indexOfPurchaseOrderNumber),
											AccountNumber= dataReader.GetNullableString(indexOfAccountNumber),
											CustomerID= dataReader.GetInt32(indexOfCustomerID),
											SalesPersonID= dataReader.GetNullableInt32(indexOfSalesPersonID),
											TerritoryID= dataReader.GetNullableInt32(indexOfTerritoryID),
											BillToAddressID= dataReader.GetInt32(indexOfBillToAddressID),
											ShipToAddressID= dataReader.GetInt32(indexOfShipToAddressID),
											ShipMethodID= dataReader.GetInt32(indexOfShipMethodID),
											CreditCardID= dataReader.GetNullableInt32(indexOfCreditCardID),
											CreditCardApprovalCode= dataReader.GetNullableString(indexOfCreditCardApprovalCode),
											CurrencyRateID= dataReader.GetNullableInt32(indexOfCurrencyRateID),
											SubTotal= dataReader.GetDecimal(indexOfSubTotal),
											TaxAmt= dataReader.GetDecimal(indexOfTaxAmt),
											Freight= dataReader.GetDecimal(indexOfFreight),
											TotalDue= dataReader.GetDecimal(indexOfTotalDue),
											Comment= dataReader.GetNullableString(indexOfComment),
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

