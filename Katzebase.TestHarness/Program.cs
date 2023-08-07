using Katzebase.PublicLibrary.Client;
using Katzebase.PublicLibrary.Payloads;
using System.Diagnostics;
using System.Reflection;

namespace Katzebase.TestHarness
{
    class Program
    {
        private static void ExportSQLServerDatabases()
        {
            var databasesNames = new string[]{
                    "StackOverflow",
                    //"WordList",
                    //"AdventureWorks",
                    //"TopNotchERP"
                };

            foreach (var databasesName in databasesNames)
            {
                (new Thread(() =>
                {
                    Exporter.ExportSQLServerDatabaseToKatzebase("localhost", databasesName, "http://localhost:6858/", false);
                })).Start();
            }
        }

        static void Main(string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Console.WriteLine($"{fileVersionInfo.FileDescription} v{fileVersionInfo.ProductVersion}");

            //(new Thread(() => { TestThread("TopNotchERP:Address"); })).Start();
            //(new Thread(() => { TestThread("AdventureWorks2012:dbo:AWBuildVersion"); })).Start();

            /*
            using (var client = new KbClient("http://localhost:6858/"))
            {
                client.Schema.DropIfExists("StackOverflow2010");
            }
            */

            ExportSQLServerDatabases();

            //TestSproc();

            #region Misc. Tests & stuff.

            //Handled:
            //string stmt = "SELECT ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE LocationId = 6	AND Shelf != 'R' AND (a = 1)";
            //string stmt = "SELECT ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE LocationId = 6	AND Shelf != 'R' AND (A = 10 AND B = 50)";
            //string stmt = "SELECT ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (LocationId = 6 AND Shelf != 'R' AND Quantity = 299) OR (LocationId = 6 AND Shelf != 'M' AND Quantity = 299 OR ProductId = 366) AND (BIN = 8 OR Bin = 11)";
            //string stmt = "SELECT ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (	LocationId = 6	AND Shelf != 'R'	AND Quantity = 299)OR(	LocationId = 6	AND Shelf != 'M'	AND Quantity = 299	OR ProductId = 366	AND	(		BIN = 8 OR Bin = 11 AND	(		Fan = 8 OR Apex = 11 ) ) AND Cake = 14 ) AND(	BIN = 99 OR Bin = 12)";
            //string stmt = "SELECT TOP 100 ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (LocationId = 6 AND Shelf != 'R' AND Quantity = 299) OR ((LocationId = 6 AND Shelf != 'M') AND Quantity = 299 OR ProductId = 366) AND (BIN = 8 OR Bin = 11)";
            //var preparedQuery = ParserEngine.ParseQuery(stmt);
            //return;


            //TestIndexCreationProductInventory();

            //using KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");
            //client.Query.ExecuteQuery("SELECT ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (	LocationId = 6	AND Shelf != 'R'	AND Quantity = 299)OR(	LocationId = 6	AND Shelf != 'M'	AND Quantity = 299	OR ProductId = 366	AND	(		BIN = 8 OR Bin = 11 AND	(		Fan = 8 OR Apex = 11 ) ) AND Cake = 14 ) AND(	BIN = 99 OR Bin = 12)");
            //client.Query.ExecuteQuery("SELECT TOP 100 ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (LocationId = 6 AND Shelf != 'M' AND Quantity = 299 OR ProductId = 366) AND (BIN = 8 OR Bin = 11)");
            //client.Query.ExecuteQuery("SELECT TOP 100 ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE LocationId = 6 AND Shelf != 'M' AND quantity = 299 AND productid = 366");
            //client.Query.ExecuteQuery("SELECT TOP 100 ProductID, LocationID, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE LocationId = 6 AND Shelf != 'M' AND quantity = 299");
            //client.Query.ExecuteQuery("SELECT TOP 100 ProductID, LocationID, Missing, Shelf, Bin, Quantity, rowguid, ModifiedDate FROM AdventureWorks2012:Production:ProductInventory WHERE (LocationId = 6 AND Shelf != 'R' AND Quantity = 299) OR ((LocationId = 6 AND Shelf != 'M') AND Quantity = 299 OR ProductId = 366) AND (BIN = 8 OR Bin = 11 OR Bin = 19)");
            //client.Query.ExecuteQuery("SELECT TOP 10 a.ProductID FROM AdventureWorks2012:Production:ProductInventory as a");

            //var result = client.Document.Sample("AdventureWorks2012:Production:Product", 10);

            //TestIndexCreationProductInventory();

            //Console.WriteLine(client.Query.ExplainQuery(query)?.Explanation);
            //client.Query.ExecuteQuery("SET TraceWaitTimes ON");
            //client.Query.ExecuteQuery(query);

            //TestCreateAllAdventureWorks2012Indexes();
            //TestServerStress();
            //TestCreateIndexAddDocuments();
            //TestAddDocumentsCreateIndex();
            //TestIndexDocumentDeletion();

            #endregion

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        static void TestSproc()
        {
            using var client = new KbClient("http://localhost:6858/");

            var procedure = new KbProcedure("AdventureWorks2012:Production:Product:UpdateProductByColorAndGetItsName");
            procedure.Parameters.Add("ProductColor", "Test-Color");
            var result = client.Procedure.Execute(procedure);

            if (result.Fields.Count > 0)
            {
                foreach (var field in result.Fields)
                {
                    Console.Write($"[{field.Name}] ");
                }
                Console.WriteLine();
            }

            if (result.Rows.Count > 0)
            {
                foreach (var row in result.Rows)
                {
                    foreach (var value in row.Values)
                    {
                        Console.Write($"'{value}' ");
                    }
                    Console.WriteLine();
                }
            }
        }

        static void TestAllAPIs()
        {
            using var client = new KbClient("http://localhost:6858/");

            client.Server.Ping();

            client.Schema.Create("TestAllAPIs");
            client.Schema.Create("TestAllAPIs:SubSchema");
            client.Schema.Exists("TestAllAPIs:SubSchema");
            client.Schema.Create("TestAllAPIs:SubSchema:Product");

            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '10000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '20000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '30000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '40000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("select * from TestAllAPIs:SubSchema:Product where Name = 'API Test Product'");
            client.Query.ExecuteQuery("delete from TestAllAPIs:SubSchema:Product where Name = 'API Test Product' and ProductId != 10000");
            client.Query.ExecuteNonQuery("delete from TestAllAPIs:SubSchema:Product where Name = 'API Test Product'");

            client.Schema.Indexes.List("TestAllAPIs:SubSchema");

            var ixSubSchemaProductId = new KbIndex("IX_SubSchema_ProductId");
            ixSubSchemaProductId.AddAttribute("ProductId");
            client.Schema.Indexes.Create("TestAllAPIs:SubSchema", ixSubSchemaProductId);

            var ixSubSchemaName = new KbIndex("IX_SubSchema_Name");
            ixSubSchemaName.AddAttribute("Name");
            client.Schema.Indexes.Create("TestAllAPIs:SubSchema", ixSubSchemaName);

            var ixSubSchemaProductIdName = new KbIndex("IX_SubSchema_ProductId_Name") { IsUnique = true };
            ixSubSchemaProductIdName.AddAttribute("ProductId");
            ixSubSchemaProductIdName.AddAttribute("Name");
            client.Schema.Indexes.Create("TestAllAPIs:SubSchema", ixSubSchemaProductIdName);

            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '10000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '20000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '30000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("insert into TestAllAPIs:SubSchema:Product(ProductId = '40000', Name = 'API Test Product')");
            client.Query.ExecuteQuery("select * from TestAllAPIs:SubSchema:Product where Name = 'API Test Product'");
            client.Query.ExecuteQuery("delete from TestAllAPIs:SubSchema:Product where Name = 'API Test Product' and ProductId != 10000");
            client.Query.ExecuteNonQuery("delete from TestAllAPIs:SubSchema:Product where Name = 'API Test Product'");

            client.Schema.Indexes.Rebuild("TestAllAPIs:SubSchema", "IX_SubSchema_ProductId_Name");

            client.Schema.Indexes.Drop("TestAllAPIs:SubSchema", "IX_SubSchema_ProductId");

            client.Schema.Indexes.Exists("TestAllAPIs:SubSchema", "IX_SubSchema_ProductId_Name");
            client.Schema.Indexes.Exists("TestAllAPIs:SubSchema", "IX_SubSchema_ProductId");

            client.Schema.Indexes.List("TestAllAPIs:SubSchema");
            client.Schema.Drop("TestAllAPIs");
        }

        #region TestQuery(text)

        private static KbQueryResultCollection TestExecuteQuery(string queryText)
        {
            using var client = new KbClient("http://localhost:6858/");
            return client.Query.ExecuteQuery(queryText);
        }

        #endregion

        #region Test Index Creation (Person)

        private static void TestIndexCreationProductInventory()
        {
            using var client = new KbClient("http://localhost:6858/");

            string? schemaPath = "AdventureWorks2012:Production:ProductSubcategory";

            client.Transaction.Begin();

            var index = new KbIndex()
            {
                Name = "IX_ProductSubcategory_ProductSubcategoryID",
                IsUnique = false
            };

            index.AddAttribute("ProductSubcategoryID");
            client.Schema.Indexes.Create(schemaPath, index);

            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        #region Test Index Creation (StateProvince)

        private static void TestIndexCreationStateProvince()
        {
            using var client = new KbClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string? schemaPath = "AdventureWorks2012:Person:StateProvince";

            client.Transaction.Begin();

            Console.WriteLine("Creating index: IX_Person_LastName_FirstName");
            var personIndex = new KbIndex()
            {
                Name = "IX_StateProvince_CountryRegionCode_Name",
                IsUnique = false
            };
            personIndex.AddAttribute("CountryRegionCode");
            personIndex.AddAttribute("Name");
            client.Schema.Indexes.Create(schemaPath, personIndex);

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        #region Test Index Creation (Person)

        private static void TestIndexCreationPerson()
        {
            using var client = new KbClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string? schemaPath = "AdventureWorks2012:Person:Person";

            client.Transaction.Begin();

            Console.WriteLine("Creating index: IX_Person_LastName_FirstName");
            var personIndex = new KbIndex()
            {
                Name = "IX_Person_LastName_FirstName",
                IsUnique = false
            };
            personIndex.AddAttribute("LastName");
            personIndex.AddAttribute("FirstName");
            client.Schema.Indexes.Create(schemaPath, personIndex);

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        #region TestIndexDocumentDeletion.
        private static void TestIndexDocumentDeletion()
        {
            using var client = new KbClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string? schemaPath = "Students:Indexing";

            if (client.Schema.Exists(schemaPath))
            {
                client.Schema.Drop(schemaPath);
            }

            client.Schema.Create(schemaPath);

            var studentNameIndex = new KbIndex()
            {
                Name = "StudentName",
                IsUnique = false
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            var studentIdIndex = new KbIndex()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            var homeRoomIndex = new KbIndex()
            {
                Name = "HomeRoom",
                IsUnique = false
            };
            homeRoomIndex.AddAttribute("HomeRoom");
            client.Schema.Indexes.Create(schemaPath, homeRoomIndex);

            client.Transaction.Begin();

            for (int i = 0; i < 1000; i++)
            {
                if (i > 0 && i % 100 == 0)
                {
                    Console.WriteLine("{0}", i);
                }
                if (i > 0 && i % 1000 == 0)
                {
                    Console.WriteLine("Comitting...");
                    client.Transaction.Commit();
                    client.Transaction.Begin();
                }

                var student = new
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new KbDocument(student));
            }

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();

            client.Transaction.Begin();

            var documents = client.Document.Catalog(schemaPath);
            foreach (var doc in documents.Collection)
            {
                if (doc.Id.ToString().StartsWith("0") == false)
                {
                    client.Document.DeleteById(schemaPath, doc.Id);
                }
            }

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        #region TestAddDocumentsCreateIndex.
        private static void TestAddDocumentsCreateIndex()
        {
            using var client = new KbClient("http://localhost:6858/");

            Console.WriteLine("Session Started: {0}", client.SessionId);
            string? schemaPath = "Students:Indexing";

            if (client.Schema.Exists(schemaPath))
            {
                client.Schema.Drop(schemaPath);
            }

            client.Schema.Create(schemaPath);

            client.Transaction.Begin();

            for (int i = 0; i < 10000; i++)
            {
                if (i > 0 && i % 100 == 0)
                {
                    Console.WriteLine("{0}", i);
                }
                if (i > 0 && i % 1000 == 0)
                {
                    Console.WriteLine("Comitting...");
                    client.Transaction.Commit();
                    client.Transaction.Begin();
                }

                var student = new
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new KbDocument(student));
            }

            client.Transaction.Commit();

            client.Transaction.Begin();

            Console.WriteLine("Creating index: StudentName");
            var studentNameIndex = new KbIndex()
            {
                Name = "StudentName",
                IsUnique = false
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            Console.WriteLine("Creating index: UniqueStudentId");
            var studentIdIndex = new KbIndex()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            Console.WriteLine("Creating index: HomeRoom");
            var homeRoomIndex = new KbIndex()
            {
                Name = "HomeRoom",
                IsUnique = false
            };
            homeRoomIndex.AddAttribute("HomeRoom");
            client.Schema.Indexes.Create(schemaPath, homeRoomIndex);

            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }
        #endregion

        #region TestCreateIndexAddDocuments.
        private static void TestCreateIndexAddDocuments()
        {
            using var client = new KbClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string? schemaPath = "Students:Indexing";

            if (client.Schema.Exists(schemaPath))
            {
                client.Schema.Drop(schemaPath);
            }

            client.Schema.Create(schemaPath);

            var studentNameIndex = new KbIndex()
            {
                Name = "StudentName",
                IsUnique = true
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            var studentIdIndex = new KbIndex()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            var homeRoomIndex = new KbIndex()
            {
                Name = "HomeRoom",
                IsUnique = false
            };
            homeRoomIndex.AddAttribute("HomeRoom");
            client.Schema.Indexes.Create(schemaPath, homeRoomIndex);

            client.Transaction.Begin();

            for (int i = 0; i < 10000; i++)
            {
                if (i > 0 && i % 100 == 0)
                {
                    Console.WriteLine("{0}", i);
                }
                if (i > 0 && i % 1000 == 0)
                {
                    Console.WriteLine("Comitting...");
                    client.Transaction.Commit();
                    client.Transaction.Begin();
                }

                var student = new
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new KbDocument(student));
            }

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();
        }

        #endregion

        #region TestServerStress.
        static void TestServerStress()
        {
            int threadCount = 0;

            for (int i = 0; i < threadCount; i++)
            {
                (new Thread(StressTestThreadProc)).Start();
            }
        }

        static void StressTestThreadProc()
        {
            using var client = new KbClient("http://localhost:6858/");

            Console.WriteLine("Session Started: {0}", client.SessionId);

            for (int test = 0; test < 10; test++)
            {
                try
                {
                    #region Create Schemas.
                    client.Schema.Create("Sales:Orders:Default");
                    client.Schema.Create("Sales:Regions:Default");
                    client.Schema.Create("Sales:People:Default");
                    client.Schema.Create("Sales:People:Salesmen");
                    client.Schema.Create("Sales:People:Customers");
                    client.Schema.Create("Sales:People:Suppliers");
                    client.Schema.Create("Sales:People:Contractors");
                    client.Schema.Create("Sales:Products:Default");
                    client.Schema.Create("Students:CurrentYear");
                    #endregion

                    #region Enum. Schemas.
                    //Console.WriteLine("Sales:People:");
                    var schemas = client.Schema.List("Sales:People");
                    foreach (var schema in schemas.Collection)
                    {
                        //Console.WriteLine("\tNS: " + schema.Name);
                    }
                    #endregion

                    #region Drop Schemas.
                    client.Schema.Drop("Sales:Products:Default");
                    client.Schema.Drop("Sales:Orders");
                    #endregion

                    #region Store Documents.

                    var student = new
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        GradeLevel = 11,
                        GPA = 3.7,
                        StudentId = Guid.NewGuid().ToString()
                    };

                    for (int i = 0; i < 100; i++)
                    {
                        client.Document.Store("Students:CurrentYear", new KbDocument(student));
                    }

                    #endregion

                    client.Transaction.Begin();

                    #region List/Delete Documents.
                    //Console.WriteLine("Students:CurrentYear");
                    var documents = client.Document.Catalog("Students:CurrentYear");
                    foreach (var doc in documents.Collection)
                    {
                        if (doc.Id.ToString().StartsWith("0") == false)
                        {
                            client.Document.DeleteById("Students:CurrentYear", doc.Id);
                        }
                        //Console.WriteLine("\tDoc: " + doc.Id);
                    }
                    #endregion

                    client.Transaction.Commit();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        #region TestCreateAllAdventureWorks2012Indexes

        static void TestCreateAllAdventureWorks2012Indexes()
        {
            using var client = new KbClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:dbo:AWBuildVersion", "PK_AWBuildVersion_SystemInformationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:dbo:AWBuildVersion PK_AWBuildVersion_SystemInformationID");
                var index = new KbIndex()
                {
                    Name = "PK_AWBuildVersion_SystemInformationID",
                    IsUnique = true
                };
                index.AddAttribute("SystemInformationID");
                client.Schema.Indexes.Create("AdventureWorks2012:dbo:AWBuildVersion", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:dbo:DatabaseLog", "PK_DatabaseLog_DatabaseLogID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:dbo:DatabaseLog PK_DatabaseLog_DatabaseLogID");
                var index = new KbIndex()
                {
                    Name = "PK_DatabaseLog_DatabaseLogID",
                    IsUnique = true
                };
                index.AddAttribute("DatabaseLogID");
                client.Schema.Indexes.Create("AdventureWorks2012:dbo:DatabaseLog", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:dbo:ErrorLog", "PK_ErrorLog_ErrorLogID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:dbo:ErrorLog PK_ErrorLog_ErrorLogID");
                var index = new KbIndex()
                {
                    Name = "PK_ErrorLog_ErrorLogID",
                    IsUnique = true
                };
                index.AddAttribute("ErrorLogID");
                client.Schema.Indexes.Create("AdventureWorks2012:dbo:ErrorLog", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Department", "PK_Department_DepartmentID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Department PK_Department_DepartmentID");
                var index = new KbIndex()
                {
                    Name = "PK_Department_DepartmentID",
                    IsUnique = true
                };
                index.AddAttribute("DepartmentID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Department", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Department", "AK_Department_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Department AK_Department_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Department_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Department", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Employee", "PK_Employee_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Employee PK_Employee_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_Employee_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Employee", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Employee", "AK_Employee_LoginID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Employee AK_Employee_LoginID");
                var index = new KbIndex()
                {
                    Name = "AK_Employee_LoginID",
                    IsUnique = true
                };
                index.AddAttribute("LoginID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Employee", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Employee", "AK_Employee_NationalIDNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Employee AK_Employee_NationalIDNumber");
                var index = new KbIndex()
                {
                    Name = "AK_Employee_NationalIDNumber",
                    IsUnique = true
                };
                index.AddAttribute("NationalIDNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Employee", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Employee", "AK_Employee_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Employee AK_Employee_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Employee_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Employee", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", "PK_EmployeeDepartmentHistory_BusinessEntityID_StartDate_DepartmentID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:EmployeeDepartmentHistory PK_EmployeeDepartmentHistory_BusinessEntityID_StartDate_DepartmentID");
                var index = new KbIndex()
                {
                    Name = "PK_EmployeeDepartmentHistory_BusinessEntityID_StartDate_DepartmentID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("DepartmentID");
                index.AddAttribute("ShiftID");
                index.AddAttribute("StartDate");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", "IX_EmployeeDepartmentHistory_DepartmentID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:EmployeeDepartmentHistory IX_EmployeeDepartmentHistory_DepartmentID");
                var index = new KbIndex()
                {
                    Name = "IX_EmployeeDepartmentHistory_DepartmentID",
                    IsUnique = false
                };
                index.AddAttribute("DepartmentID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", "IX_EmployeeDepartmentHistory_ShiftID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:EmployeeDepartmentHistory IX_EmployeeDepartmentHistory_ShiftID");
                var index = new KbIndex()
                {
                    Name = "IX_EmployeeDepartmentHistory_ShiftID",
                    IsUnique = false
                };
                index.AddAttribute("ShiftID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:EmployeeDepartmentHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:EmployeePayHistory", "PK_EmployeePayHistory_BusinessEntityID_RateChangeDate") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:EmployeePayHistory PK_EmployeePayHistory_BusinessEntityID_RateChangeDate");
                var index = new KbIndex()
                {
                    Name = "PK_EmployeePayHistory_BusinessEntityID_RateChangeDate",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("RateChangeDate");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:EmployeePayHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:JobCandidate", "PK_JobCandidate_JobCandidateID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:JobCandidate PK_JobCandidate_JobCandidateID");
                var index = new KbIndex()
                {
                    Name = "PK_JobCandidate_JobCandidateID",
                    IsUnique = true
                };
                index.AddAttribute("JobCandidateID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:JobCandidate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:JobCandidate", "IX_JobCandidate_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:JobCandidate IX_JobCandidate_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "IX_JobCandidate_BusinessEntityID",
                    IsUnique = false
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:JobCandidate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Shift", "PK_Shift_ShiftID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Shift PK_Shift_ShiftID");
                var index = new KbIndex()
                {
                    Name = "PK_Shift_ShiftID",
                    IsUnique = true
                };
                index.AddAttribute("ShiftID");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Shift", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Shift", "AK_Shift_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Shift AK_Shift_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Shift_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Shift", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:HumanResources:Shift", "AK_Shift_StartTime_EndTime") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:HumanResources:Shift AK_Shift_StartTime_EndTime");
                var index = new KbIndex()
                {
                    Name = "AK_Shift_StartTime_EndTime",
                    IsUnique = true
                };
                index.AddAttribute("StartTime");
                index.AddAttribute("EndTime");
                client.Schema.Indexes.Create("AdventureWorks2012:HumanResources:Shift", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Address", "PK_Address_AddressID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Address PK_Address_AddressID");
                var index = new KbIndex()
                {
                    Name = "PK_Address_AddressID",
                    IsUnique = true
                };
                index.AddAttribute("AddressID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Address", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Address", "AK_Address_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Address AK_Address_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Address_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Address", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Address", "IX_Address_AddressLine1_AddressLine2_City_StateProvinceID_PostalCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Address IX_Address_AddressLine1_AddressLine2_City_StateProvinceID_PostalCode");
                var index = new KbIndex()
                {
                    Name = "IX_Address_AddressLine1_AddressLine2_City_StateProvinceID_PostalCode",
                    IsUnique = true
                };
                index.AddAttribute("AddressLine1");
                index.AddAttribute("AddressLine2");
                index.AddAttribute("City");
                index.AddAttribute("StateProvinceID");
                index.AddAttribute("PostalCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Address", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Address", "IX_Address_StateProvinceID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Address IX_Address_StateProvinceID");
                var index = new KbIndex()
                {
                    Name = "IX_Address_StateProvinceID",
                    IsUnique = false
                };
                index.AddAttribute("StateProvinceID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Address", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:AddressType", "PK_AddressType_AddressTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:AddressType PK_AddressType_AddressTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_AddressType_AddressTypeID",
                    IsUnique = true
                };
                index.AddAttribute("AddressTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:AddressType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:AddressType", "AK_AddressType_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:AddressType AK_AddressType_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_AddressType_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:AddressType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:AddressType", "AK_AddressType_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:AddressType AK_AddressType_Name");
                var index = new KbIndex()
                {
                    Name = "AK_AddressType_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:AddressType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntity", "PK_BusinessEntity_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntity PK_BusinessEntity_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_BusinessEntity_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntity", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntity", "AK_BusinessEntity_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntity AK_BusinessEntity_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_BusinessEntity_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntity", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityAddress", "PK_BusinessEntityAddress_BusinessEntityID_AddressID_AddressTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityAddress PK_BusinessEntityAddress_BusinessEntityID_AddressID_AddressTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_BusinessEntityAddress_BusinessEntityID_AddressID_AddressTypeID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("AddressID");
                index.AddAttribute("AddressTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityAddress", "AK_BusinessEntityAddress_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityAddress AK_BusinessEntityAddress_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_BusinessEntityAddress_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityAddress", "IX_BusinessEntityAddress_AddressID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityAddress IX_BusinessEntityAddress_AddressID");
                var index = new KbIndex()
                {
                    Name = "IX_BusinessEntityAddress_AddressID",
                    IsUnique = false
                };
                index.AddAttribute("AddressID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityAddress", "IX_BusinessEntityAddress_AddressTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityAddress IX_BusinessEntityAddress_AddressTypeID");
                var index = new KbIndex()
                {
                    Name = "IX_BusinessEntityAddress_AddressTypeID",
                    IsUnique = false
                };
                index.AddAttribute("AddressTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityContact", "PK_BusinessEntityContact_BusinessEntityID_PersonID_ContactTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityContact PK_BusinessEntityContact_BusinessEntityID_PersonID_ContactTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_BusinessEntityContact_BusinessEntityID_PersonID_ContactTypeID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("PersonID");
                index.AddAttribute("ContactTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityContact", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityContact", "AK_BusinessEntityContact_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityContact AK_BusinessEntityContact_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_BusinessEntityContact_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityContact", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityContact", "IX_BusinessEntityContact_PersonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityContact IX_BusinessEntityContact_PersonID");
                var index = new KbIndex()
                {
                    Name = "IX_BusinessEntityContact_PersonID",
                    IsUnique = false
                };
                index.AddAttribute("PersonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityContact", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:BusinessEntityContact", "IX_BusinessEntityContact_ContactTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:BusinessEntityContact IX_BusinessEntityContact_ContactTypeID");
                var index = new KbIndex()
                {
                    Name = "IX_BusinessEntityContact_ContactTypeID",
                    IsUnique = false
                };
                index.AddAttribute("ContactTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:BusinessEntityContact", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:ContactType", "PK_ContactType_ContactTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:ContactType PK_ContactType_ContactTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_ContactType_ContactTypeID",
                    IsUnique = true
                };
                index.AddAttribute("ContactTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:ContactType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:ContactType", "AK_ContactType_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:ContactType AK_ContactType_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ContactType_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:ContactType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:CountryRegion", "PK_CountryRegion_CountryRegionCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:CountryRegion PK_CountryRegion_CountryRegionCode");
                var index = new KbIndex()
                {
                    Name = "PK_CountryRegion_CountryRegionCode",
                    IsUnique = true
                };
                index.AddAttribute("CountryRegionCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:CountryRegion", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:CountryRegion", "AK_CountryRegion_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:CountryRegion AK_CountryRegion_Name");
                var index = new KbIndex()
                {
                    Name = "AK_CountryRegion_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:CountryRegion", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:EmailAddress", "PK_EmailAddress_BusinessEntityID_EmailAddressID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:EmailAddress PK_EmailAddress_BusinessEntityID_EmailAddressID");
                var index = new KbIndex()
                {
                    Name = "PK_EmailAddress_BusinessEntityID_EmailAddressID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("EmailAddressID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:EmailAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:EmailAddress", "IX_EmailAddress_EmailAddress") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:EmailAddress IX_EmailAddress_EmailAddress");
                var index = new KbIndex()
                {
                    Name = "IX_EmailAddress_EmailAddress",
                    IsUnique = false
                };
                index.AddAttribute("EmailAddress");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:EmailAddress", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Password", "PK_Password_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Password PK_Password_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_Password_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Password", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "PK_Person_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person PK_Person_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_Person_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "IX_Person_LastName_FirstName_MiddleName") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person IX_Person_LastName_FirstName_MiddleName");
                var index = new KbIndex()
                {
                    Name = "IX_Person_LastName_FirstName_MiddleName",
                    IsUnique = false
                };
                index.AddAttribute("LastName");
                index.AddAttribute("FirstName");
                index.AddAttribute("MiddleName");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "AK_Person_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person AK_Person_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Person_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "PXML_Person_AddContact") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person PXML_Person_AddContact");
                var index = new KbIndex()
                {
                    Name = "PXML_Person_AddContact",
                    IsUnique = false
                };
                index.AddAttribute("AdditionalContactInfo");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "PXML_Person_Demographics") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person PXML_Person_Demographics");
                var index = new KbIndex()
                {
                    Name = "PXML_Person_Demographics",
                    IsUnique = false
                };
                index.AddAttribute("Demographics");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "XMLPATH_Person_Demographics") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person XMLPATH_Person_Demographics");
                var index = new KbIndex()
                {
                    Name = "XMLPATH_Person_Demographics",
                    IsUnique = false
                };
                index.AddAttribute("Demographics");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "XMLPROPERTY_Person_Demographics") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person XMLPROPERTY_Person_Demographics");
                var index = new KbIndex()
                {
                    Name = "XMLPROPERTY_Person_Demographics",
                    IsUnique = false
                };
                index.AddAttribute("Demographics");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:Person", "XMLVALUE_Person_Demographics") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:Person XMLVALUE_Person_Demographics");
                var index = new KbIndex()
                {
                    Name = "XMLVALUE_Person_Demographics",
                    IsUnique = false
                };
                index.AddAttribute("Demographics");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:Person", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:PersonPhone", "PK_PersonPhone_BusinessEntityID_PhoneNumber_PhoneNumberTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:PersonPhone PK_PersonPhone_BusinessEntityID_PhoneNumber_PhoneNumberTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_PersonPhone_BusinessEntityID_PhoneNumber_PhoneNumberTypeID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("PhoneNumber");
                index.AddAttribute("PhoneNumberTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:PersonPhone", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:PersonPhone", "IX_PersonPhone_PhoneNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:PersonPhone IX_PersonPhone_PhoneNumber");
                var index = new KbIndex()
                {
                    Name = "IX_PersonPhone_PhoneNumber",
                    IsUnique = false
                };
                index.AddAttribute("PhoneNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:PersonPhone", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:PhoneNumberType", "PK_PhoneNumberType_PhoneNumberTypeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:PhoneNumberType PK_PhoneNumberType_PhoneNumberTypeID");
                var index = new KbIndex()
                {
                    Name = "PK_PhoneNumberType_PhoneNumberTypeID",
                    IsUnique = true
                };
                index.AddAttribute("PhoneNumberTypeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:PhoneNumberType", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:StateProvince", "PK_StateProvince_StateProvinceID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:StateProvince PK_StateProvince_StateProvinceID");
                var index = new KbIndex()
                {
                    Name = "PK_StateProvince_StateProvinceID",
                    IsUnique = true
                };
                index.AddAttribute("StateProvinceID");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:StateProvince", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:StateProvince", "AK_StateProvince_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:StateProvince AK_StateProvince_Name");
                var index = new KbIndex()
                {
                    Name = "AK_StateProvince_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:StateProvince", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:StateProvince", "AK_StateProvince_StateProvinceCode_CountryRegionCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:StateProvince AK_StateProvince_StateProvinceCode_CountryRegionCode");
                var index = new KbIndex()
                {
                    Name = "AK_StateProvince_StateProvinceCode_CountryRegionCode",
                    IsUnique = true
                };
                index.AddAttribute("StateProvinceCode");
                index.AddAttribute("CountryRegionCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:StateProvince", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Person:StateProvince", "AK_StateProvince_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Person:StateProvince AK_StateProvince_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_StateProvince_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Person:StateProvince", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:BillOfMaterials", "AK_BillOfMaterials_ProductAssemblyID_ComponentID_StartDate") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:BillOfMaterials AK_BillOfMaterials_ProductAssemblyID_ComponentID_StartDate");
                var index = new KbIndex()
                {
                    Name = "AK_BillOfMaterials_ProductAssemblyID_ComponentID_StartDate",
                    IsUnique = true
                };
                index.AddAttribute("ProductAssemblyID");
                index.AddAttribute("ComponentID");
                index.AddAttribute("StartDate");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:BillOfMaterials", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:BillOfMaterials", "PK_BillOfMaterials_BillOfMaterialsID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:BillOfMaterials PK_BillOfMaterials_BillOfMaterialsID");
                var index = new KbIndex()
                {
                    Name = "PK_BillOfMaterials_BillOfMaterialsID",
                    IsUnique = true
                };
                index.AddAttribute("BillOfMaterialsID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:BillOfMaterials", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:BillOfMaterials", "IX_BillOfMaterials_UnitMeasureCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:BillOfMaterials IX_BillOfMaterials_UnitMeasureCode");
                var index = new KbIndex()
                {
                    Name = "IX_BillOfMaterials_UnitMeasureCode",
                    IsUnique = false
                };
                index.AddAttribute("UnitMeasureCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:BillOfMaterials", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Culture", "PK_Culture_CultureID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Culture PK_Culture_CultureID");
                var index = new KbIndex()
                {
                    Name = "PK_Culture_CultureID",
                    IsUnique = true
                };
                index.AddAttribute("CultureID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Culture", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Culture", "AK_Culture_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Culture AK_Culture_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Culture_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Culture", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Document", "PK_Document") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Document PK_Document");
                var index = new KbIndex()
                {
                    Name = "PK_Document",
                    IsUnique = true
                };
                index.AddAttribute("Id");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Document", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Document", "UQ__Document__F73921F7BAEE5BF6") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Document UQ__Document__F73921F7BAEE5BF6");
                var index = new KbIndex()
                {
                    Name = "UQ__Document__F73921F7BAEE5BF6",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Document", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Document", "AK_Document_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Document AK_Document_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Document_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Document", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Document", "IX_Document_FileName_Revision") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Document IX_Document_FileName_Revision");
                var index = new KbIndex()
                {
                    Name = "IX_Document_FileName_Revision",
                    IsUnique = false
                };
                index.AddAttribute("FileName");
                index.AddAttribute("Revision");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Document", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Illustration", "PK_Illustration_IllustrationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Illustration PK_Illustration_IllustrationID");
                var index = new KbIndex()
                {
                    Name = "PK_Illustration_IllustrationID",
                    IsUnique = true
                };
                index.AddAttribute("IllustrationID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Illustration", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Location", "PK_Location_LocationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Location PK_Location_LocationID");
                var index = new KbIndex()
                {
                    Name = "PK_Location_LocationID",
                    IsUnique = true
                };
                index.AddAttribute("LocationID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Location", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Location", "AK_Location_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Location AK_Location_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Location_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Location", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Product", "PK_Product_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Product PK_Product_ProductID");
                var index = new KbIndex()
                {
                    Name = "PK_Product_ProductID",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Product", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Product", "AK_Product_ProductNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Product AK_Product_ProductNumber");
                var index = new KbIndex()
                {
                    Name = "AK_Product_ProductNumber",
                    IsUnique = true
                };
                index.AddAttribute("ProductNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Product", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Product", "AK_Product_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Product AK_Product_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Product_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Product", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:Product", "AK_Product_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:Product AK_Product_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Product_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:Product", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductCategory", "PK_ProductCategory_ProductCategoryID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductCategory PK_ProductCategory_ProductCategoryID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductCategory_ProductCategoryID",
                    IsUnique = true
                };
                index.AddAttribute("ProductCategoryID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductCategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductCategory", "AK_ProductCategory_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductCategory AK_ProductCategory_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ProductCategory_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductCategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductCategory", "AK_ProductCategory_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductCategory AK_ProductCategory_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_ProductCategory_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductCategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductCostHistory", "PK_ProductCostHistory_ProductID_StartDate") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductCostHistory PK_ProductCostHistory_ProductID_StartDate");
                var index = new KbIndex()
                {
                    Name = "PK_ProductCostHistory_ProductID_StartDate",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("StartDate");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductCostHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductDescription", "PK_ProductDescription_ProductDescriptionID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductDescription PK_ProductDescription_ProductDescriptionID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductDescription_ProductDescriptionID",
                    IsUnique = true
                };
                index.AddAttribute("ProductDescriptionID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductDescription", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductDescription", "AK_ProductDescription_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductDescription AK_ProductDescription_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_ProductDescription_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductDescription", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductDocument", "PK_ProductDocument") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductDocument PK_ProductDocument");
                var index = new KbIndex()
                {
                    Name = "PK_ProductDocument",
                    IsUnique = true
                };
                index.AddAttribute("Id");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductDocument", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductInventory", "PK_ProductInventory_ProductID_LocationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductInventory PK_ProductInventory_ProductID_LocationID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductInventory_ProductID_LocationID",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("LocationID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductInventory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductListPriceHistory", "PK_ProductListPriceHistory_ProductID_StartDate") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductListPriceHistory PK_ProductListPriceHistory_ProductID_StartDate");
                var index = new KbIndex()
                {
                    Name = "PK_ProductListPriceHistory_ProductID_StartDate",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("StartDate");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductListPriceHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModel", "PK_ProductModel_ProductModelID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModel PK_ProductModel_ProductModelID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductModel_ProductModelID",
                    IsUnique = true
                };
                index.AddAttribute("ProductModelID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModel", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModel", "AK_ProductModel_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModel AK_ProductModel_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ProductModel_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModel", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModel", "AK_ProductModel_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModel AK_ProductModel_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_ProductModel_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModel", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModel", "PXML_ProductModel_CatalogDescription") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModel PXML_ProductModel_CatalogDescription");
                var index = new KbIndex()
                {
                    Name = "PXML_ProductModel_CatalogDescription",
                    IsUnique = false
                };
                index.AddAttribute("CatalogDescription");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModel", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModel", "PXML_ProductModel_Instructions") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModel PXML_ProductModel_Instructions");
                var index = new KbIndex()
                {
                    Name = "PXML_ProductModel_Instructions",
                    IsUnique = false
                };
                index.AddAttribute("Instructions");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModel", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModelIllustration", "PK_ProductModelIllustration_ProductModelID_IllustrationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModelIllustration PK_ProductModelIllustration_ProductModelID_IllustrationID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductModelIllustration_ProductModelID_IllustrationID",
                    IsUnique = true
                };
                index.AddAttribute("ProductModelID");
                index.AddAttribute("IllustrationID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModelIllustration", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductModelProductDescriptionCulture", "PK_ProductModelProductDescriptionCulture_ProductModelID_ProductDescriptionID_CultureID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductModelProductDescriptionCulture PK_ProductModelProductDescriptionCulture_ProductModelID_ProductDescriptionID_CultureID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductModelProductDescriptionCulture_ProductModelID_ProductDescriptionID_CultureID",
                    IsUnique = true
                };
                index.AddAttribute("ProductModelID");
                index.AddAttribute("ProductDescriptionID");
                index.AddAttribute("CultureID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductModelProductDescriptionCulture", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductPhoto", "PK_ProductPhoto_ProductPhotoID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductPhoto PK_ProductPhoto_ProductPhotoID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductPhoto_ProductPhotoID",
                    IsUnique = true
                };
                index.AddAttribute("ProductPhotoID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductPhoto", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductProductPhoto", "PK_ProductProductPhoto_ProductID_ProductPhotoID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductProductPhoto PK_ProductProductPhoto_ProductID_ProductPhotoID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductProductPhoto_ProductID_ProductPhotoID",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("ProductPhotoID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductProductPhoto", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductReview", "PK_ProductReview_ProductReviewID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductReview PK_ProductReview_ProductReviewID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductReview_ProductReviewID",
                    IsUnique = true
                };
                index.AddAttribute("ProductReviewID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductReview", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductReview", "IX_ProductReview_ProductID_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductReview IX_ProductReview_ProductID_Name");
                var index = new KbIndex()
                {
                    Name = "IX_ProductReview_ProductID_Name",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("ReviewerName");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductReview", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductSubcategory", "PK_ProductSubcategory_ProductSubcategoryID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductSubcategory PK_ProductSubcategory_ProductSubcategoryID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductSubcategory_ProductSubcategoryID",
                    IsUnique = true
                };
                index.AddAttribute("ProductSubcategoryID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductSubcategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductSubcategory", "AK_ProductSubcategory_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductSubcategory AK_ProductSubcategory_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ProductSubcategory_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductSubcategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ProductSubcategory", "AK_ProductSubcategory_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ProductSubcategory AK_ProductSubcategory_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_ProductSubcategory_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ProductSubcategory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ScrapReason", "PK_ScrapReason_ScrapReasonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ScrapReason PK_ScrapReason_ScrapReasonID");
                var index = new KbIndex()
                {
                    Name = "PK_ScrapReason_ScrapReasonID",
                    IsUnique = true
                };
                index.AddAttribute("ScrapReasonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ScrapReason", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:ScrapReason", "AK_ScrapReason_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:ScrapReason AK_ScrapReason_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ScrapReason_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:ScrapReason", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistory", "PK_TransactionHistory_TransactionID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistory PK_TransactionHistory_TransactionID");
                var index = new KbIndex()
                {
                    Name = "PK_TransactionHistory_TransactionID",
                    IsUnique = true
                };
                index.AddAttribute("TransactionID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistory", "IX_TransactionHistory_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistory IX_TransactionHistory_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_TransactionHistory_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistory", "IX_TransactionHistory_ReferenceOrderID_ReferenceOrderLineID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistory IX_TransactionHistory_ReferenceOrderID_ReferenceOrderLineID");
                var index = new KbIndex()
                {
                    Name = "IX_TransactionHistory_ReferenceOrderID_ReferenceOrderLineID",
                    IsUnique = false
                };
                index.AddAttribute("ReferenceOrderID");
                index.AddAttribute("ReferenceOrderLineID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistoryArchive", "PK_TransactionHistoryArchive_TransactionID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistoryArchive PK_TransactionHistoryArchive_TransactionID");
                var index = new KbIndex()
                {
                    Name = "PK_TransactionHistoryArchive_TransactionID",
                    IsUnique = true
                };
                index.AddAttribute("TransactionID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistoryArchive", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistoryArchive", "IX_TransactionHistoryArchive_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistoryArchive IX_TransactionHistoryArchive_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_TransactionHistoryArchive_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistoryArchive", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:TransactionHistoryArchive", "IX_TransactionHistoryArchive_ReferenceOrderID_ReferenceOrderLineID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:TransactionHistoryArchive IX_TransactionHistoryArchive_ReferenceOrderID_ReferenceOrderLineID");
                var index = new KbIndex()
                {
                    Name = "IX_TransactionHistoryArchive_ReferenceOrderID_ReferenceOrderLineID",
                    IsUnique = false
                };
                index.AddAttribute("ReferenceOrderID");
                index.AddAttribute("ReferenceOrderLineID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:TransactionHistoryArchive", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:UnitMeasure", "PK_UnitMeasure_UnitMeasureCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:UnitMeasure PK_UnitMeasure_UnitMeasureCode");
                var index = new KbIndex()
                {
                    Name = "PK_UnitMeasure_UnitMeasureCode",
                    IsUnique = true
                };
                index.AddAttribute("UnitMeasureCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:UnitMeasure", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:UnitMeasure", "AK_UnitMeasure_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:UnitMeasure AK_UnitMeasure_Name");
                var index = new KbIndex()
                {
                    Name = "AK_UnitMeasure_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:UnitMeasure", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:WorkOrder", "PK_WorkOrder_WorkOrderID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:WorkOrder PK_WorkOrder_WorkOrderID");
                var index = new KbIndex()
                {
                    Name = "PK_WorkOrder_WorkOrderID",
                    IsUnique = true
                };
                index.AddAttribute("WorkOrderID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:WorkOrder", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:WorkOrder", "IX_WorkOrder_ScrapReasonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:WorkOrder IX_WorkOrder_ScrapReasonID");
                var index = new KbIndex()
                {
                    Name = "IX_WorkOrder_ScrapReasonID",
                    IsUnique = false
                };
                index.AddAttribute("ScrapReasonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:WorkOrder", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:WorkOrder", "IX_WorkOrder_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:WorkOrder IX_WorkOrder_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_WorkOrder_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:WorkOrder", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:WorkOrderRouting", "PK_WorkOrderRouting_WorkOrderID_ProductID_OperationSequence") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:WorkOrderRouting PK_WorkOrderRouting_WorkOrderID_ProductID_OperationSequence");
                var index = new KbIndex()
                {
                    Name = "PK_WorkOrderRouting_WorkOrderID_ProductID_OperationSequence",
                    IsUnique = true
                };
                index.AddAttribute("WorkOrderID");
                index.AddAttribute("ProductID");
                index.AddAttribute("OperationSequence");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:WorkOrderRouting", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Production:WorkOrderRouting", "IX_WorkOrderRouting_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Production:WorkOrderRouting IX_WorkOrderRouting_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_WorkOrderRouting_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Production:WorkOrderRouting", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ProductVendor", "PK_ProductVendor_ProductID_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ProductVendor PK_ProductVendor_ProductID_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_ProductVendor_ProductID_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("ProductID");
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ProductVendor", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ProductVendor", "IX_ProductVendor_UnitMeasureCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ProductVendor IX_ProductVendor_UnitMeasureCode");
                var index = new KbIndex()
                {
                    Name = "IX_ProductVendor_UnitMeasureCode",
                    IsUnique = false
                };
                index.AddAttribute("UnitMeasureCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ProductVendor", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ProductVendor", "IX_ProductVendor_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ProductVendor IX_ProductVendor_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "IX_ProductVendor_BusinessEntityID",
                    IsUnique = false
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ProductVendor", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:PurchaseOrderDetail", "PK_PurchaseOrderDetail_PurchaseOrderID_PurchaseOrderDetailID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:PurchaseOrderDetail PK_PurchaseOrderDetail_PurchaseOrderID_PurchaseOrderDetailID");
                var index = new KbIndex()
                {
                    Name = "PK_PurchaseOrderDetail_PurchaseOrderID_PurchaseOrderDetailID",
                    IsUnique = true
                };
                index.AddAttribute("PurchaseOrderID");
                index.AddAttribute("PurchaseOrderDetailID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:PurchaseOrderDetail", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:PurchaseOrderDetail", "IX_PurchaseOrderDetail_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:PurchaseOrderDetail IX_PurchaseOrderDetail_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_PurchaseOrderDetail_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:PurchaseOrderDetail", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:PurchaseOrderHeader", "PK_PurchaseOrderHeader_PurchaseOrderID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:PurchaseOrderHeader PK_PurchaseOrderHeader_PurchaseOrderID");
                var index = new KbIndex()
                {
                    Name = "PK_PurchaseOrderHeader_PurchaseOrderID",
                    IsUnique = true
                };
                index.AddAttribute("PurchaseOrderID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:PurchaseOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:PurchaseOrderHeader", "IX_PurchaseOrderHeader_VendorID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:PurchaseOrderHeader IX_PurchaseOrderHeader_VendorID");
                var index = new KbIndex()
                {
                    Name = "IX_PurchaseOrderHeader_VendorID",
                    IsUnique = false
                };
                index.AddAttribute("VendorID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:PurchaseOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:PurchaseOrderHeader", "IX_PurchaseOrderHeader_EmployeeID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:PurchaseOrderHeader IX_PurchaseOrderHeader_EmployeeID");
                var index = new KbIndex()
                {
                    Name = "IX_PurchaseOrderHeader_EmployeeID",
                    IsUnique = false
                };
                index.AddAttribute("EmployeeID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:PurchaseOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ShipMethod", "PK_ShipMethod_ShipMethodID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ShipMethod PK_ShipMethod_ShipMethodID");
                var index = new KbIndex()
                {
                    Name = "PK_ShipMethod_ShipMethodID",
                    IsUnique = true
                };
                index.AddAttribute("ShipMethodID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ShipMethod", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ShipMethod", "AK_ShipMethod_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ShipMethod AK_ShipMethod_Name");
                var index = new KbIndex()
                {
                    Name = "AK_ShipMethod_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ShipMethod", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:ShipMethod", "AK_ShipMethod_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:ShipMethod AK_ShipMethod_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_ShipMethod_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:ShipMethod", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:Vendor", "PK_Vendor_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:Vendor PK_Vendor_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_Vendor_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:Vendor", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Purchasing:Vendor", "AK_Vendor_AccountNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Purchasing:Vendor AK_Vendor_AccountNumber");
                var index = new KbIndex()
                {
                    Name = "AK_Vendor_AccountNumber",
                    IsUnique = true
                };
                index.AddAttribute("AccountNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Purchasing:Vendor", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CountryRegionCurrency", "PK_CountryRegionCurrency_CountryRegionCode_CurrencyCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CountryRegionCurrency PK_CountryRegionCurrency_CountryRegionCode_CurrencyCode");
                var index = new KbIndex()
                {
                    Name = "PK_CountryRegionCurrency_CountryRegionCode_CurrencyCode",
                    IsUnique = true
                };
                index.AddAttribute("CountryRegionCode");
                index.AddAttribute("CurrencyCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CountryRegionCurrency", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CountryRegionCurrency", "IX_CountryRegionCurrency_CurrencyCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CountryRegionCurrency IX_CountryRegionCurrency_CurrencyCode");
                var index = new KbIndex()
                {
                    Name = "IX_CountryRegionCurrency_CurrencyCode",
                    IsUnique = false
                };
                index.AddAttribute("CurrencyCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CountryRegionCurrency", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CreditCard", "PK_CreditCard_CreditCardID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CreditCard PK_CreditCard_CreditCardID");
                var index = new KbIndex()
                {
                    Name = "PK_CreditCard_CreditCardID",
                    IsUnique = true
                };
                index.AddAttribute("CreditCardID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CreditCard", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CreditCard", "AK_CreditCard_CardNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CreditCard AK_CreditCard_CardNumber");
                var index = new KbIndex()
                {
                    Name = "AK_CreditCard_CardNumber",
                    IsUnique = true
                };
                index.AddAttribute("CardNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CreditCard", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Currency", "PK_Currency_CurrencyCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Currency PK_Currency_CurrencyCode");
                var index = new KbIndex()
                {
                    Name = "PK_Currency_CurrencyCode",
                    IsUnique = true
                };
                index.AddAttribute("CurrencyCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Currency", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Currency", "AK_Currency_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Currency AK_Currency_Name");
                var index = new KbIndex()
                {
                    Name = "AK_Currency_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Currency", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CurrencyRate", "PK_CurrencyRate_CurrencyRateID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CurrencyRate PK_CurrencyRate_CurrencyRateID");
                var index = new KbIndex()
                {
                    Name = "PK_CurrencyRate_CurrencyRateID",
                    IsUnique = true
                };
                index.AddAttribute("CurrencyRateID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CurrencyRate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:CurrencyRate", "AK_CurrencyRate_CurrencyRateDate_FromCurrencyCode_ToCurrencyCode") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:CurrencyRate AK_CurrencyRate_CurrencyRateDate_FromCurrencyCode_ToCurrencyCode");
                var index = new KbIndex()
                {
                    Name = "AK_CurrencyRate_CurrencyRateDate_FromCurrencyCode_ToCurrencyCode",
                    IsUnique = true
                };
                index.AddAttribute("CurrencyRateDate");
                index.AddAttribute("FromCurrencyCode");
                index.AddAttribute("ToCurrencyCode");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:CurrencyRate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Customer", "PK_Customer_CustomerID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Customer PK_Customer_CustomerID");
                var index = new KbIndex()
                {
                    Name = "PK_Customer_CustomerID",
                    IsUnique = true
                };
                index.AddAttribute("CustomerID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Customer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Customer", "AK_Customer_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Customer AK_Customer_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Customer_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Customer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Customer", "AK_Customer_AccountNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Customer AK_Customer_AccountNumber");
                var index = new KbIndex()
                {
                    Name = "AK_Customer_AccountNumber",
                    IsUnique = true
                };
                index.AddAttribute("AccountNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Customer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Customer", "IX_Customer_TerritoryID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Customer IX_Customer_TerritoryID");
                var index = new KbIndex()
                {
                    Name = "IX_Customer_TerritoryID",
                    IsUnique = false
                };
                index.AddAttribute("TerritoryID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Customer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:PersonCreditCard", "PK_PersonCreditCard_BusinessEntityID_CreditCardID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:PersonCreditCard PK_PersonCreditCard_BusinessEntityID_CreditCardID");
                var index = new KbIndex()
                {
                    Name = "PK_PersonCreditCard_BusinessEntityID_CreditCardID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("CreditCardID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:PersonCreditCard", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderDetail", "PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderDetail PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesOrderDetail_SalesOrderID_SalesOrderDetailID",
                    IsUnique = true
                };
                index.AddAttribute("SalesOrderID");
                index.AddAttribute("SalesOrderDetailID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderDetail", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderDetail", "AK_SalesOrderDetail_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderDetail AK_SalesOrderDetail_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesOrderDetail_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderDetail", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderDetail", "IX_SalesOrderDetail_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderDetail IX_SalesOrderDetail_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_SalesOrderDetail_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderDetail", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeader", "PK_SalesOrderHeader_SalesOrderID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeader PK_SalesOrderHeader_SalesOrderID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesOrderHeader_SalesOrderID",
                    IsUnique = true
                };
                index.AddAttribute("SalesOrderID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeader", "AK_SalesOrderHeader_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeader AK_SalesOrderHeader_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesOrderHeader_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeader", "AK_SalesOrderHeader_SalesOrderNumber") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeader AK_SalesOrderHeader_SalesOrderNumber");
                var index = new KbIndex()
                {
                    Name = "AK_SalesOrderHeader_SalesOrderNumber",
                    IsUnique = true
                };
                index.AddAttribute("SalesOrderNumber");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeader", "IX_SalesOrderHeader_CustomerID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeader IX_SalesOrderHeader_CustomerID");
                var index = new KbIndex()
                {
                    Name = "IX_SalesOrderHeader_CustomerID",
                    IsUnique = false
                };
                index.AddAttribute("CustomerID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeader", "IX_SalesOrderHeader_SalesPersonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeader IX_SalesOrderHeader_SalesPersonID");
                var index = new KbIndex()
                {
                    Name = "IX_SalesOrderHeader_SalesPersonID",
                    IsUnique = false
                };
                index.AddAttribute("SalesPersonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeader", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason", "PK_SalesOrderHeaderSalesReason_SalesOrderID_SalesReasonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesOrderHeaderSalesReason PK_SalesOrderHeaderSalesReason_SalesOrderID_SalesReasonID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesOrderHeaderSalesReason_SalesOrderID_SalesReasonID",
                    IsUnique = true
                };
                index.AddAttribute("SalesOrderID");
                index.AddAttribute("SalesReasonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesOrderHeaderSalesReason", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesPerson", "PK_SalesPerson_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesPerson PK_SalesPerson_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesPerson_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesPerson", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesPerson", "AK_SalesPerson_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesPerson AK_SalesPerson_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesPerson_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesPerson", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesPersonQuotaHistory", "PK_SalesPersonQuotaHistory_BusinessEntityID_QuotaDate") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesPersonQuotaHistory PK_SalesPersonQuotaHistory_BusinessEntityID_QuotaDate");
                var index = new KbIndex()
                {
                    Name = "PK_SalesPersonQuotaHistory_BusinessEntityID_QuotaDate",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("QuotaDate");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesPersonQuotaHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesPersonQuotaHistory", "AK_SalesPersonQuotaHistory_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesPersonQuotaHistory AK_SalesPersonQuotaHistory_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesPersonQuotaHistory_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesPersonQuotaHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesReason", "PK_SalesReason_SalesReasonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesReason PK_SalesReason_SalesReasonID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesReason_SalesReasonID",
                    IsUnique = true
                };
                index.AddAttribute("SalesReasonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesReason", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTaxRate", "PK_SalesTaxRate_SalesTaxRateID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTaxRate PK_SalesTaxRate_SalesTaxRateID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesTaxRate_SalesTaxRateID",
                    IsUnique = true
                };
                index.AddAttribute("SalesTaxRateID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTaxRate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTaxRate", "AK_SalesTaxRate_StateProvinceID_TaxType") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTaxRate AK_SalesTaxRate_StateProvinceID_TaxType");
                var index = new KbIndex()
                {
                    Name = "AK_SalesTaxRate_StateProvinceID_TaxType",
                    IsUnique = true
                };
                index.AddAttribute("StateProvinceID");
                index.AddAttribute("TaxType");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTaxRate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTaxRate", "AK_SalesTaxRate_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTaxRate AK_SalesTaxRate_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesTaxRate_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTaxRate", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTerritory", "PK_SalesTerritory_TerritoryID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTerritory PK_SalesTerritory_TerritoryID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesTerritory_TerritoryID",
                    IsUnique = true
                };
                index.AddAttribute("TerritoryID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTerritory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTerritory", "AK_SalesTerritory_Name") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTerritory AK_SalesTerritory_Name");
                var index = new KbIndex()
                {
                    Name = "AK_SalesTerritory_Name",
                    IsUnique = true
                };
                index.AddAttribute("Name");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTerritory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTerritory", "AK_SalesTerritory_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTerritory AK_SalesTerritory_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesTerritory_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTerritory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTerritoryHistory", "PK_SalesTerritoryHistory_BusinessEntityID_StartDate_TerritoryID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTerritoryHistory PK_SalesTerritoryHistory_BusinessEntityID_StartDate_TerritoryID");
                var index = new KbIndex()
                {
                    Name = "PK_SalesTerritoryHistory_BusinessEntityID_StartDate_TerritoryID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                index.AddAttribute("TerritoryID");
                index.AddAttribute("StartDate");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTerritoryHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SalesTerritoryHistory", "AK_SalesTerritoryHistory_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SalesTerritoryHistory AK_SalesTerritoryHistory_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SalesTerritoryHistory_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SalesTerritoryHistory", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:ShoppingCartItem", "PK_ShoppingCartItem_ShoppingCartItemID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:ShoppingCartItem PK_ShoppingCartItem_ShoppingCartItemID");
                var index = new KbIndex()
                {
                    Name = "PK_ShoppingCartItem_ShoppingCartItemID",
                    IsUnique = true
                };
                index.AddAttribute("ShoppingCartItemID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:ShoppingCartItem", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:ShoppingCartItem", "IX_ShoppingCartItem_ShoppingCartID_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:ShoppingCartItem IX_ShoppingCartItem_ShoppingCartID_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_ShoppingCartItem_ShoppingCartID_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ShoppingCartID");
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:ShoppingCartItem", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SpecialOffer", "PK_SpecialOffer_SpecialOfferID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SpecialOffer PK_SpecialOffer_SpecialOfferID");
                var index = new KbIndex()
                {
                    Name = "PK_SpecialOffer_SpecialOfferID",
                    IsUnique = true
                };
                index.AddAttribute("SpecialOfferID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SpecialOffer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SpecialOffer", "AK_SpecialOffer_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SpecialOffer AK_SpecialOffer_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SpecialOffer_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SpecialOffer", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SpecialOfferProduct", "PK_SpecialOfferProduct_SpecialOfferID_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SpecialOfferProduct PK_SpecialOfferProduct_SpecialOfferID_ProductID");
                var index = new KbIndex()
                {
                    Name = "PK_SpecialOfferProduct_SpecialOfferID_ProductID",
                    IsUnique = true
                };
                index.AddAttribute("SpecialOfferID");
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SpecialOfferProduct", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SpecialOfferProduct", "AK_SpecialOfferProduct_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SpecialOfferProduct AK_SpecialOfferProduct_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_SpecialOfferProduct_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SpecialOfferProduct", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:SpecialOfferProduct", "IX_SpecialOfferProduct_ProductID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:SpecialOfferProduct IX_SpecialOfferProduct_ProductID");
                var index = new KbIndex()
                {
                    Name = "IX_SpecialOfferProduct_ProductID",
                    IsUnique = false
                };
                index.AddAttribute("ProductID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:SpecialOfferProduct", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Store", "PK_Store_BusinessEntityID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Store PK_Store_BusinessEntityID");
                var index = new KbIndex()
                {
                    Name = "PK_Store_BusinessEntityID",
                    IsUnique = true
                };
                index.AddAttribute("BusinessEntityID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Store", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Store", "AK_Store_rowguid") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Store AK_Store_rowguid");
                var index = new KbIndex()
                {
                    Name = "AK_Store_rowguid",
                    IsUnique = true
                };
                index.AddAttribute("rowguid");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Store", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Store", "IX_Store_SalesPersonID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Store IX_Store_SalesPersonID");
                var index = new KbIndex()
                {
                    Name = "IX_Store_SalesPersonID",
                    IsUnique = false
                };
                index.AddAttribute("SalesPersonID");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Store", index);
            }
            client.Transaction.Commit();
            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:Sales:Store", "PXML_Store_Demographics") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:Sales:Store PXML_Store_Demographics");
                var index = new KbIndex()
                {
                    Name = "PXML_Store_Demographics",
                    IsUnique = false
                };
                index.AddAttribute("Demographics");
                client.Schema.Indexes.Create("AdventureWorks2012:Sales:Store", index);
            }
            client.Transaction.Commit();

            Console.WriteLine("Session Completed: {0}", client.SessionId);
        }

        #endregion

        private static readonly Random random = new();
        public static string? RandomString(int length)
        {
            const string? chars = "ABCDEFG";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
