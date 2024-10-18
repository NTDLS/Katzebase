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

module DDLExecutionBasicTests =
    open NTDLS.Katzebase.Parsers.Query

    //open NTDLS.Katzebase.Api.Payloads
    open NTDLS.Katzebase.Api.Types

    type SingleCount () =
        let mutable c = 0
        member this.Count 
            with get() = c
            and set(v) = c <- v

    let ``Execute "CREATE SCHEMA testSch"`` (outputOpt:ITestOutputHelper option) =
        _core.Query.SystemExecuteAndCommitNonQuery($"insert into {testSchemaDDL} (\r\nid: 123, value: '456'\r\n)") |> ignore
        _core.Query.SystemExecuteAndCommitNonQuery($"insert into {testSchemaDDL} (\r\nid: 321, value: '654'\r\n)") |> ignore
        _core.Transactions.Commit(preLogin)
        //let cnt = _core.Query.SystemExecuteQuery<SingleCount>(preLogin, "SELECT COUNT(*) FROM testSch", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
        //equals 1 (cnt |> Seq.item 0).Count
        let userParameters = new KbInsensitiveDictionary<KbVariable>()


        let countTest sql expectedCount = 
            try
                let queries = StaticParserBatch.Parse(sql, userParameters)
                let query = queries.Item 0
        
                let queryResultCollection = _core.Query.ExecuteQuery(preLogin, query)
                equals 1 queryResultCollection.Collection.Count

                let queryDocList = (queryResultCollection.Collection.Item 0).Rows
                equals 1 queryDocList.Count
                equals $"{expectedCount}" queryDocList[0].Values[0].me

                let sc =
                    _core.Query.SystemExecuteQueryAndCommit<SingleCount>(sql, Unchecked.defaultof<KbInsensitiveDictionary<string>>)
                    |> Seq.toArray
                    |> Array.item 0

                equals expectedCount sc.Count
            with
            | exn ->
                testPrint outputOpt "[By design] %s" exn.Message
                equals "Value should not be null. (Parameter 'textValue')" exn.InnerException.InnerException.Message

        testPrint outputOpt "count scalar"
        countTest $"SELECT COUNT(1) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count star"
        countTest $"SELECT COUNT(*) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count column id"
        countTest $"SELECT COUNT(id) as Count FROM {testSchemaDDL}" 2

        testPrint outputOpt "count column not existed"
        countTest $"SELECT COUNT(not_existed_column) as Count FROM {testSchemaDDL}" 0

        testPrint outputOpt "[PASSED] Execute \"CREATE SCHEMA testSch\"" 

    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Execute "CREATE SCHEMA testSch"`` () =
            ``Execute "CREATE SCHEMA testSch"`` (Some output)

