#if INTERACTIVE
#load "../../UnitTestsSharedResource.ver.0.0.1.fsx"
#r "nuget: DecimalMath.DecimalEx, 1.0.2"
#r "nuget: Xunit"
#endif

open Shared
open Xunit
open Xunit.Abstractions
open System.Collections.Generic


module ParserBasicTests =
    open NTDLS.Katzebase.Engine.Parsers
    open NTDLS.Katzebase.Client




    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Parse "SELECT * FROM MASTER:ACCOUNT"`` () =
            let userParameters = null
            let preparedQueries = StaticQueryParser.ParseBatch(_core, "SELECT * FROM MASTER:ACCOUNT", userParameters.ToUserParametersInsensitiveDictionary())
            equals 1 preparedQueries.Count 