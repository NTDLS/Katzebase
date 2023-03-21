using Katzebase.Library.Client;
using Katzebase.Library.Payloads;
using System;
using System.Data.SqlClient;

namespace Katzebase.TestHarness.ADORepository
{
    public partial class HumanResources_EmployeeRepository
    {
        public void Export_HumanResources_Employee()
        {
            KatzebaseClient client = new KatzebaseClient("http://localhost:6858/");

            if (client.Schema.Exists("AdventureWorks2012:HumanResources:Employee"))
            {
                return;
            }

            client.Transaction.Begin();

            client.Schema.Create("AdventureWorks2012:HumanResources:Employee");

            using (SqlConnection connection = new SqlConnection("Server=.;Database=AdventureWorks2012;Trusted_Connection=True;"))
            {
                connection.Open();

                try
                {
                    using (SqlCommand command = new SqlCommand("SELECT * FROM HumanResources.Employee", connection))
                    {
                        command.CommandTimeout = 10000;
                        command.CommandType = System.Data.CommandType.Text;

                        using (SqlDataReader dataReader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
                        {
                            int indexOfBusinessEntityID = dataReader.GetOrdinal("BusinessEntityID");
                            int indexOfNationalIDNumber = dataReader.GetOrdinal("NationalIDNumber");
                            int indexOfLoginID = dataReader.GetOrdinal("LoginID");
                            int indexOfJobTitle = dataReader.GetOrdinal("JobTitle");
                            int indexOfBirthDate = dataReader.GetOrdinal("BirthDate");
                            int indexOfMaritalStatus = dataReader.GetOrdinal("MaritalStatus");
                            int indexOfGender = dataReader.GetOrdinal("Gender");
                            int indexOfHireDate = dataReader.GetOrdinal("HireDate");
                            int indexOfSalariedFlag = dataReader.GetOrdinal("SalariedFlag");
                            int indexOfVacationHours = dataReader.GetOrdinal("VacationHours");
                            int indexOfSickLeaveHours = dataReader.GetOrdinal("SickLeaveHours");
                            int indexOfCurrentFlag = dataReader.GetOrdinal("CurrentFlag");
                            int indexOfrowguid = dataReader.GetOrdinal("rowguid");
                            int indexOfModifiedDate = dataReader.GetOrdinal("ModifiedDate");

                            int rowCount = 0;


                            while (dataReader.Read() /*&& rowCount++ < 10000*/)
                            {
                                if (rowCount > 0 && (rowCount % 100) == 0)
                                {
                                    Console.WriteLine("AdventureWorks2012:HumanResources:Employee: {0}", rowCount);
                                }

                                if (rowCount > 0 && (rowCount % 1000) == 0)
                                {
                                    Console.WriteLine("Comitting...");
                                    client.Transaction.Commit();
                                    client.Transaction.Begin();
                                }

                                try
                                {
                                    client.Document.Store("AdventureWorks2012:HumanResources:Employee", new KbDocument(new Models.HumanResources_Employee
                                    {
                                        BusinessEntityID = dataReader.GetInt32(indexOfBusinessEntityID),
                                        NationalIDNumber = dataReader.GetString(indexOfNationalIDNumber),
                                        LoginID = dataReader.GetString(indexOfLoginID),
                                        JobTitle = dataReader.GetString(indexOfJobTitle),
                                        BirthDate = dataReader.GetDateTime(indexOfBirthDate),
                                        MaritalStatus = dataReader.GetString(indexOfMaritalStatus),
                                        Gender = dataReader.GetString(indexOfGender),
                                        HireDate = dataReader.GetDateTime(indexOfHireDate),
                                        SalariedFlag = dataReader.GetBoolean(indexOfSalariedFlag),
                                        VacationHours = dataReader.GetInt16(indexOfVacationHours),
                                        SickLeaveHours = dataReader.GetInt16(indexOfSickLeaveHours),
                                        CurrentFlag = dataReader.GetBoolean(indexOfCurrentFlag),
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

