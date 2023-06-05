using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class Production_ProductRepository
    {
        public void Export_Production_Product()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:Production:Product"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:Production:Product");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM Production.Product", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfProductID = dataReader.GetOrdinal("ProductID");
                            int indexOfName = dataReader.GetOrdinal("Name");
                            int indexOfProductNumber = dataReader.GetOrdinal("ProductNumber");
                            int indexOfMakeFlag = dataReader.GetOrdinal("MakeFlag");
                            int indexOfFinishedGoodsFlag = dataReader.GetOrdinal("FinishedGoodsFlag");
                            int indexOfColor = dataReader.GetOrdinal("Color");
                            int indexOfSafetyStockLevel = dataReader.GetOrdinal("SafetyStockLevel");
                            int indexOfReorderPoint = dataReader.GetOrdinal("ReorderPoint");
                            int indexOfStandardCost = dataReader.GetOrdinal("StandardCost");
                            int indexOfListPrice = dataReader.GetOrdinal("ListPrice");
                            int indexOfSize = dataReader.GetOrdinal("Size");
                            int indexOfSizeUnitMeasureCode = dataReader.GetOrdinal("SizeUnitMeasureCode");
                            int indexOfWeightUnitMeasureCode = dataReader.GetOrdinal("WeightUnitMeasureCode");
                            int indexOfWeight = dataReader.GetOrdinal("Weight");
                            int indexOfDaysToManufacture = dataReader.GetOrdinal("DaysToManufacture");
                            int indexOfProductLine = dataReader.GetOrdinal("ProductLine");
                            int indexOfClass = dataReader.GetOrdinal("Class");
                            int indexOfStyle = dataReader.GetOrdinal("Style");
                            int indexOfProductSubcategoryID = dataReader.GetOrdinal("ProductSubcategoryID");
                            int indexOfProductModelID = dataReader.GetOrdinal("ProductModelID");
                            int indexOfSellStartDate = dataReader.GetOrdinal("SellStartDate");
                            int indexOfSellEndDate = dataReader.GetOrdinal("SellEndDate");
                            int indexOfDiscontinuedDate = dataReader.GetOrdinal("DiscontinuedDate");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:Production:Product: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:Production:Product", new KbDocument(new Models.Production_Product
                                    {
                                        ProductID = dataReader.GetInt32(indexOfProductID),
                                        Name = dataReader.GetString(indexOfName),
                                        ProductNumber = dataReader.GetString(indexOfProductNumber),
                                        MakeFlag = dataReader.GetBoolean(indexOfMakeFlag),
                                        FinishedGoodsFlag = dataReader.GetBoolean(indexOfFinishedGoodsFlag),
                                        Color = dataReader.GetNullableString(indexOfColor),
                                        SafetyStockLevel = dataReader.GetInt16(indexOfSafetyStockLevel),
                                        ReorderPoint = dataReader.GetInt16(indexOfReorderPoint),
                                        StandardCost = dataReader.GetDecimal(indexOfStandardCost),
                                        ListPrice = dataReader.GetDecimal(indexOfListPrice),
                                        Size = dataReader.GetNullableString(indexOfSize),
                                        SizeUnitMeasureCode = dataReader.GetNullableString(indexOfSizeUnitMeasureCode),
                                        WeightUnitMeasureCode = dataReader.GetNullableString(indexOfWeightUnitMeasureCode),
                                        Weight = dataReader.GetNullableDecimal(indexOfWeight),
                                        DaysToManufacture = dataReader.GetInt32(indexOfDaysToManufacture),
                                        ProductLine = dataReader.GetNullableString(indexOfProductLine),
                                        Class = dataReader.GetNullableString(indexOfClass),
                                        Style = dataReader.GetNullableString(indexOfStyle),
                                        ProductSubcategoryID = dataReader.GetNullableInt32(indexOfProductSubcategoryID),
                                        ProductModelID = dataReader.GetNullableInt32(indexOfProductModelID),
                                        SellStartDate = dataReader.GetDateTime(indexOfSellStartDate),
                                        SellEndDate = dataReader.GetNullableDateTime(indexOfSellEndDate),
                                        DiscontinuedDate = dataReader.GetNullableDateTime(indexOfDiscontinuedDate),
                                        rowguid = dataReader.GetGuid(indexOfrowguid),
                                        ModifiedDate = dataReader.GetDateTime(indexOfModifiedDate),
                                    }));
                                }
                                catch (Exception ex)
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

