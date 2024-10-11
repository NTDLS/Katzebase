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

module DMLExecutionBasicTests =
    open NTDLS.Katzebase.Api.Types
    open NTDLS.Katzebase.Engine.QueryProcessing.Functions
    open NTDLS.Katzebase.Parsers.Query.Fields    
    open NTDLS.Katzebase.Parsers.Query

    type ExprProc = StaticScalarExpressionProcessor

    type TwoColumnString () =
        member val COL1 = "" with get, set
        member val COL2 = "" with get, set

    type TwoColumnInt () =
        member val COL1 = 0 with get, set
        member val COL2 = 0 with get, set

    type TwoColumnDouble () =
        member val COL1 = 0.0 with get, set
        member val COL2 = 0.0 with get, set
    
    let plainInsert = $"""INSERT INTO {testSchemaDML} (COL1, COL2) VALUES (1,2), ("A", "B")"""

    let ``Execute "INSERT INTO testSch (COL1, COL2) VALUES (1,2), ("A", "B")"`` (outputOpt:ITestOutputHelper option) =
        let preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), "testUser", "testClient")
        let userParameters = new KbInsensitiveDictionary<KbVariable>()
        let preparedQueries = StaticParserBatch.Parse(plainInsert, userParameters)
        let preparedQuery = preparedQueries.Item 0
        
        equals [|"COL1"; "COL2"|] (preparedQuery.InsertFieldNames |> Seq.toArray)
        equals 2 preparedQuery.InsertFieldValues.Count

        let insert0 = preparedQuery.InsertFieldValues.Item 0
        let insert1 = preparedQuery.InsertFieldValues.Item 1

        equals 2 insert0.Count
        equals 2 insert1.Count

        let i0v0 = insert0.Item 0
        let i0v1 = insert0.Item 1
        let i1v0 = insert1.Item 0
        let i1v1 = insert1.Item 1

        match i0v0.Expression with
        | :? QueryFieldConstantNumeric as num -> 
            equals "$n_2$" (num.V<fstring, string>())
        | _ -> 
            () // Do nothing for unhandled cases

        match i0v1.Expression with
        | :? QueryFieldConstantNumeric as num -> 
            equals "$n_3$" (num.V<fstring, string>())
        | _ -> 
            () // Do nothing for unhandled cases

        match i1v0.Expression with
        | :? QueryFieldConstantString as str -> 
            equals "$s_0$" (str.V<fstring, string>())
        | _ -> 
            () // Do nothing for unhandled cases

        match i1v1.Expression with
        | :? QueryFieldConstantString as str -> 
            equals "$s_1$" (str.V<fstring, string>())
        | _ -> 
            () // Do nothing for unhandled cases
           
        let transactionReference = _core.Transactions.APIAcquire(preLogin)
        let fieldQueryCollection = QueryFieldCollection (preparedQuery.Batch)
        let auxiliaryFields = KbInsensitiveDictionary<fstring> ()
        let collapsed01 = 
            ExprProc.CollapseScalarQueryField(
                i0v1.Expression
                , transactionReference.Transaction
                , preparedQuery, fieldQueryCollection
                , auxiliaryFields)
        let collapsed11 = 
            ExprProc.CollapseScalarQueryField(
                i1v1.Expression
                , transactionReference.Transaction
                , preparedQuery, fieldQueryCollection
                , auxiliaryFields)

        equals "2" collapsed01.me
        equals "B" collapsed11.me

        let queryResultCollection = _core.Query.ExecuteQuery(preLogin, preparedQuery)
        _core.Transactions.Commit(preLogin)

        
        let rString = 
            _core.Query.ExecuteQuery<TwoColumnString>(preLogin, $"SELECT * FROM {testSchemaDML} ORDER BY COL1", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
            |> Seq.toArray

        equals 2 rString.Length
        equals "1" rString[0].COL1
        equals "B" rString[1].COL2

        let rInt = 
            _core.Query.ExecuteQuery<TwoColumnInt>(preLogin, $"SELECT * FROM {testSchemaDML} where COL1 = 1", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
            |> Seq.toArray

        equals 1 rInt.Length
        equals 1 rInt[0].COL1

        let rDouble = 
            _core.Query.ExecuteQuery<TwoColumnDouble>(preLogin, $"SELECT * FROM {testSchemaDML} where COL1 = 1", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
            |> Seq.toArray

        equals 1 rDouble.Length
        equals 1.0 rDouble[0].COL1

        try
            let rDouble = 
                _core.Query.ExecuteQuery<TwoColumnDouble>(preLogin, $"SELECT * FROM {testSchemaDML}", Unchecked.defaultof<KbInsensitiveDictionary<string>>)
                |> Seq.toArray

            ()
        with
        | exn ->
#if GENERIC_TDATA
            equals "The input string 'A' was not in a correct format." exn.Message
#else
            equals "Failed to convert field [COL1] value [A] to type [Double]." exn.Message
#endif
        testPrint outputOpt $"[PASSED] {plainInsert}"

    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Execute "INSERT INTO testSch (COL1, COL2) VALUES (1,2), ("A", "B")"`` () =
            ``Execute "INSERT INTO testSch (COL1, COL2) VALUES (1,2), ("A", "B")"`` (Some output)
