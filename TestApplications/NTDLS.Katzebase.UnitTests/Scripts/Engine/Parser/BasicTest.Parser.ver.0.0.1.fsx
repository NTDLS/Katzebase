#if INTERACTIVE
#load "../../UnitTestsSharedResource.ver.0.0.1.fsx"
#load "../../Tests.ver.0.0.1.fsx"
#r "nuget: DecimalMath.DecimalEx, 1.0.2"
#r "nuget: Xunit"
#endif

open Shared
open Tests
open Xunit
open Xunit.Abstractions
open System.Collections.Generic


module ParserBasicTests =
    open NTDLS.Katzebase.Engine.Parsers
    open NTDLS.Katzebase.Client

    let ``Parse "SELECT * FROM MASTER:ACCOUNT"`` (outputOpt:ITestOutputHelper option) =
            let userParameters = null
            let preparedQueries = StaticQueryParser.ParseBatch(_core, "SELECT * FROM MASTER:ACCOUNT", userParameters.ToUserParametersInsensitiveDictionary())
            equals 1 preparedQueries.Count 
            testPrint outputOpt "[PASSED] SELECT * FROM MASTER:ACCOUNT"


    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Parse "SELECT * FROM MASTER:ACCOUNT"`` () =
            ``Parse "SELECT * FROM MASTER:ACCOUNT"`` (Some output)