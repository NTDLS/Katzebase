namespace NTDLS.Katzebase.UnitTests

open Shared
open BasicTest.Parser.ver.``0``.``0``.``1``
open BasicTest.IO.ver.``0``.``0``.``1``.IOTests

module KatzebaseTests =
    ParserBasicTests.``Parse "SELECT * FROM MASTER:ACCOUNT"`` None    
    printfn "Done!"        