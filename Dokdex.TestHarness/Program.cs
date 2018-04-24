using Dokdex.Library.Client;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Dokdex.Library.Payloads;
using System.Linq;

namespace Dokdex.TestHarness
{
    class Program
    {
        static void Main(string[] args)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            Console.WriteLine("{0} v{1}", fileVersionInfo.FileDescription, fileVersionInfo.ProductVersion);

            //Exporter.ExportAll();
            //TestCreateAllAdventureWorks2012Indexes();
            //TestServerStress();

            //TestCreateIndexAddDocuments();
            //TestAddDocumentsCreateIndex();
            //TestIndexDocumentDeletion();

            DokdexClient client = new DokdexClient("http://localhost:6858/");
            string query = "SELECT TOP 100 ProductID, Name, ModifiedDate FROM :AdventureWorks2012:Production:Product WHERE SafetyStockLevel = 1000 Color = 'Silver'";
            client.Query.Execute(query);

            Console.ReadLine();
        }

        #region TestIndexDocumentDeletion.
        private static void TestIndexDocumentDeletion()
        {
            DokdexClient client = new DokdexClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string schemaPath = "Students:Indexing";

            if (client.Schema.Exists(schemaPath))
            {
                client.Schema.Drop(schemaPath);
            }

            client.Schema.Create(schemaPath);

            Index studentNameIndex = new Index()
            {
                Name = "StudentName",
                IsUnique = false
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            Index studentIdIndex = new Index()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            Index homeRoomIndex = new Index()
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

                StudentRecord student = new StudentRecord()
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new Document(student));
            }

            Console.WriteLine("Comitting transaction.");
            client.Transaction.Commit();

            client.Transaction.Begin();

            var documents = client.Document.Catalog(schemaPath);
            foreach (var doc in documents)
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
            DokdexClient client = new DokdexClient("http://localhost:6858/");

            Console.WriteLine("Session Started: {0}", client.SessionId);
            string schemaPath = "Students:Indexing";

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

                StudentRecord student = new StudentRecord()
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new Document(student));
            }

            client.Transaction.Commit();

            client.Transaction.Begin();

            Console.WriteLine("Creating index: StudentName");
            Index studentNameIndex = new Index()
            {
                Name = "StudentName",
                IsUnique = false
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            Console.WriteLine("Creating index: UniqueStudentId");
            Index studentIdIndex = new Index()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            Console.WriteLine("Creating index: HomeRoom");
            Index homeRoomIndex = new Index()
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
            DokdexClient client = new DokdexClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            string schemaPath = "Students:Indexing";

            if (client.Schema.Exists(schemaPath))
            {
                client.Schema.Drop(schemaPath);
            }

            client.Schema.Create(schemaPath);

            Index studentNameIndex = new Index()
            {
                Name = "StudentName",
                IsUnique = true
            };
            studentNameIndex.AddAttribute("FirstName");
            studentNameIndex.AddAttribute("LastName");
            client.Schema.Indexes.Create(schemaPath, studentNameIndex);

            Index studentIdIndex = new Index()
            {
                Name = "UniqueStudentId",
                IsUnique = true
            };
            studentIdIndex.AddAttribute("StudentId");
            client.Schema.Indexes.Create(schemaPath, studentIdIndex);

            Index homeRoomIndex = new Index()
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

                StudentRecord student = new StudentRecord()
                {
                    FirstName = RandomString(2),
                    LastName = RandomString(2),
                    GradeLevel = 11,
                    GPA = 3.7,
                    HomeRoom = random.Next(1, 10),
                    StudentId = Guid.NewGuid().ToString()
                };

                client.Document.Store(schemaPath, new Document(student));
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
            DokdexClient client = new DokdexClient("http://localhost:6858/");

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
                    foreach (var schema in schemas.List)
                    {
                        //Console.WriteLine("\tNS: " + schema.Name);
                    }
                    #endregion

                    #region Drop Schemas.
                    client.Schema.Drop("Sales:Products:Default");
                    client.Schema.Drop("Sales:Orders");
                    #endregion
                    
                    #region Store Documents.

                    StudentRecord student = new StudentRecord()
                    {
                        FirstName = "John",
                        LastName = "Doe",
                        GradeLevel = 11,
                        GPA = 3.7,
                        StudentId = Guid.NewGuid().ToString()
                    };

                    for (int i = 0; i < 100; i++)
                    {
                        client.Document.Store("Students:CurrentYear", new Document(student));
                    }

                    #endregion

                    client.Transaction.Begin();

                    #region List/Delete Documents.
                    //Console.WriteLine("Students:CurrentYear");
                    var documents = client.Document.Catalog("Students:CurrentYear");
                    foreach (var doc in documents)
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
            DokdexClient client = new DokdexClient("http://localhost:6858/");
            Console.WriteLine("Session Started: {0}", client.SessionId);

            client.Transaction.Begin();
            if (client.Schema.Indexes.Exists("AdventureWorks2012:dbo:AWBuildVersion", "PK_AWBuildVersion_SystemInformationID") == false)
            {
                Console.WriteLine("Creating index: AdventureWorks2012:dbo:AWBuildVersion PK_AWBuildVersion_SystemInformationID");
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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
                Index index = new Index()
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

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFG";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
