namespace NTDLS.Katzebase.UnitTests

open Shared
open BasicTest.Parser.ver.``0``.``0``.``1``
open BasicTest.IO.ver.``0``.``0``.``1``.IOTests
open BasicTest.DDLExecution.ver.``0``.``0``.``2``
open BasicTest.DMLExecution.ver.``0``.``0``.``2``

module KatzebaseTests =
    if true then
        ParserBasicTests.``Parse "SELECT * FROM MASTER:ACCOUNT"`` None 
        ParserBasicTests.``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ?Username AND PasswordHash = ?PasswordHash"`` None
        DDLExecutionBasicTests.``Execute "CREATE SCHEMA testSch"`` None
    DMLExecutionBasicTests.``Execute "INSERT INTO testSch (COL1, COL2) VALUES (1,2), ("A", "B")"`` None
    printfn "Done!"        