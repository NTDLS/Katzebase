#if INTERACTIVE
#load "../../UnitTestsSharedResource.ver.0.0.1.fsx"
#load "../../Tests.ver.0.0.1.fsx"
#r "nuget: DecimalMath.DecimalEx, 1.0.2"
#r "nuget: Xunit"
#r "nuget: NCalc"
#endif

open Shared
open Tests
open Xunit
open Xunit.Abstractions
open System
open System.Collections.Generic

module DDLExecutionBasicTests =
    open NTDLS.Katzebase.Engine.Parsers
    
    open NTDLS.Katzebase.Client.Payloads
    open NTDLS.Katzebase.Client.Types

    type SingleCount () =
        member val Count = 0 with get, set

    let ``Execute "CREATE SCHEMA testSch"`` (outputOpt:ITestOutputHelper option) =
        let preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), "testUser", "testClient")
        _core.Query.ExecuteNonQuery(preLogin, "DROP SCHEMA testSch")
        _core.Query.ExecuteNonQuery(preLogin, "CREATE SCHEMA testSch")
        _core.Query.ExecuteNonQuery(preLogin, "insert into testSch (\r\nid = 123, value = '456'\r\n)")
        //let cnt = _core.Query.ExecuteQuery<SingleCount>(preLogin, "SELECT COUNT(*) FROM testSch", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
        //equals 1 (cnt |> Seq.item 0).Count
        let userParameters = new KbInsensitiveDictionary<KbConstant>()
        let preparedQueries = StaticQueryParser.ParseBatch(_core, "SELECT COUNT(*) FROM testSch", userParameters)
        let preparedQuery = preparedQueries.Item 0
        
        let queryResultCollection = _core.Query.ExecuteQuery(preLogin, preparedQuery)
        equals 1 queryResultCollection.RowCount
        //_core.Query.ExecuteQuery<int>(preLogin, "SELECT COUNT(*) FROM MASTER:ACCOUNT", Unchecked.defaultof<KbInsensitiveDictionary<string>>)

    //type CommonTests (output:ITestOutputHelper) =
    //    [<Fact>]
    //    member this.``Parse "SELECT * FROM MASTER:ACCOUNT"`` () =
    //        ``Parse "SELECT * FROM MASTER:ACCOUNT"`` (Some output)

    //    [<Fact>]
    //    member this.``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"`` () =
    //        ``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"`` (Some output)
