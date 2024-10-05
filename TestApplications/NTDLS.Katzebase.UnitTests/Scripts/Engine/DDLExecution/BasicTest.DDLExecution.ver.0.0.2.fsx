#if INTERACTIVE
#load "../../UnitTestsSharedResource.ver.0.0.1.fsx"
#load "../../Tests.ver.0.0.1.fsx"
#r "nuget: DecimalMath.DecimalEx, 1.0.2"
#r "nuget: Xunit"
#r "nuget: NCalc"
#endif

#if GENERIC_TDATA

#else
open NTDLS.Katzebase.Api.Payloads
#endif

open Shared
open Tests
open Xunit
open Xunit.Abstractions
open System
open System.Collections.Generic

module DDLExecutionBasicTests =
    open NTDLS.Katzebase.Parsers
    
    //open NTDLS.Katzebase.Api.Payloads
    open NTDLS.Katzebase.Api.Types

    type SingleCount () =
        let mutable c = 0
        member this.Count 
            with get() = c
            and set(v) = c <- v

    let ``Execute "CREATE SCHEMA testSch"`` (outputOpt:ITestOutputHelper option) =
        
        let preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), "testUser", "testClient")
        
        _core.Query.ExecuteNonQuery(preLogin, $"insert into {testSchemaDDL} (\r\nid = 123, value = '456'\r\n)")
        _core.Query.ExecuteNonQuery(preLogin, $"insert into {testSchemaDDL} (\r\nid = 321, value = '654'\r\n)")
        _core.Transactions.Commit(preLogin)
        //let cnt = _core.Query.ExecuteQuery<SingleCount>(preLogin, "SELECT COUNT(*) FROM testSch", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
        //equals 1 (cnt |> Seq.item 0).Count
        let userParameters = new KbInsensitiveDictionary<KbConstant>()


        let countTest sql expectedCount = 
            try
                let preparedQueries = StaticQueryParser.ParseBatch(sql, userParameters)
                let preparedQuery = preparedQueries.Item 0
        
                let queryResultCollection = _core.Query.ExecuteQuery(preLogin, preparedQuery)
                equals 1 queryResultCollection.Collection.Count

                let queryDocList = ((queryResultCollection.Collection.Item 0) :?> KbQueryDocumentListResult).Rows
                equals 1 queryDocList.Count
                equals $"{expectedCount}" queryDocList[0].Values[0].me

                let sc =
                    _core.Query.ExecuteQuery<SingleCount>(preLogin, sql, Unchecked.defaultof<KbInsensitiveDictionary<string>>)
                    |> Seq.toArray
                    |> Array.item 0

                equals expectedCount sc.Count
            with
            | exn ->
                testPrint outputOpt "[By design] %s" exn.InnerException.InnerException.Message
                equals "Value should not be null. (Parameter 'textValue')" exn.InnerException.InnerException.Message

        testPrint outputOpt "count scalar"
        countTest $"SELECT COUNT(1) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count star"
        countTest $"SELECT COUNT(*) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count column id"
        countTest $"SELECT COUNT(id) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count column not existed"
        countTest $"SELECT COUNT(not_existed_column) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "[PASSED] Execute \"CREATE SCHEMA testSch\"" 

    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Execute "CREATE SCHEMA testSch"`` () =
            ``Execute "CREATE SCHEMA testSch"`` (Some output)

