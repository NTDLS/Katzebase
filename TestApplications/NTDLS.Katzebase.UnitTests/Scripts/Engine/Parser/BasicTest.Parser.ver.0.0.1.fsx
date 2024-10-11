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
    open NTDLS.Katzebase.Api
    open NTDLS.Katzebase.Api.Exceptions
    open NTDLS.Katzebase.Api.Types
    open NTDLS.Katzebase.Parsers
    open NTDLS.Katzebase.Parsers.Query.Fields
    open NTDLS.Katzebase.Parsers.Query

    let ``Parse "SELECT * FROM MASTER:ACCOUNT"`` (outputOpt:ITestOutputHelper option) =
        let userParameters = null
        let preparedQueries = StaticParserBatch.Parse("SELECT * FROM MASTER:ACCOUNT", _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary())
        
        equals 1 preparedQueries.Count 

        let pq0 = preparedQueries[0]

        equals "master:account" (pq0.Schemas.Item 0).Name
        equals Constants.QueryType.Select pq0.QueryType
        equals 0 pq0.Conditions.Collection.Count

        testPrint outputOpt "[PASSED] SELECT * FROM MASTER:ACCOUNT"

    let ``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"`` (outputOpt:ITestOutputHelper option) =
        try
            let userParameters = null
            let _ = StaticParserBatch.Parse("SELECT * FROM MASTER:ACCOUNT_WHERE Username = @Username AND PasswordHash = @PasswordHash", _core.GlobalConstants, userParameters.ToUserParametersInsensitiveDictionary())
            ()
        with
        | :? KbParserException as pe ->
            equals "Variable not defined: [@Username]." pe.Message

        //[TODO-Test] will add another test case to verify ToUserParametersInsensitiveDictionary
        let userParameters = new KbInsensitiveDictionary<KbVariable>()
        userParameters.Add("@Username", new KbVariable("testUser", KbConstants.KbBasicDataType.String))
        userParameters.Add("@PasswordHash", new KbVariable("testPassword", KbConstants.KbBasicDataType.String))
        let preparedQueries = StaticParserBatch.Parse("SELECT * FROM MASTER:ACCOUNT WHERE Username = @Username AND PasswordHash = @PasswordHash", _core.GlobalConstants, userParameters)

        equals 1 preparedQueries.Count 

        let pq0 = preparedQueries[0]
        equals "master:account" (pq0.Schemas.Item 0).Name
        equals Constants.QueryType.Select pq0.QueryType
        equals 1 pq0.Conditions.Collection.Count
        equals 4 pq0.Conditions.FieldCollection.Count

        let cf0 = pq0.Conditions.FieldCollection.Item 0
        let cf1 = pq0.Conditions.FieldCollection.Item 1
        let cf2 = pq0.Conditions.FieldCollection.Item 2
        let cf3 = pq0.Conditions.FieldCollection.Item 3

        match cf0.Expression with
        | :? QueryFieldDocumentIdentifier as qfdi ->
            equals "Username" qfdi.FieldName
        | _ ->
            testPrint outputOpt "Field 0 type incorrect."
            equals "QueryFieldDocumentIdentifier" (cf0.Expression.GetType().Name)

        match cf1.Expression with
        | :? QueryFieldConstantString as qfcs ->
            equals "$s_0$" qfcs.Value
        | _ ->
            testPrint outputOpt "Field 1 type incorrect."
            equals "QueryFieldConstantString" (cf1.Expression.GetType().Name)

        match cf2.Expression with
        | :? QueryFieldDocumentIdentifier as qfdi ->
            equals "PasswordHash" qfdi.FieldName
        | _ ->
            testPrint outputOpt "Field 2 type incorrect."
            equals "QueryFieldDocumentIdentifier" (cf2.Expression.GetType().Name)

        match cf3.Expression with
        | :? QueryFieldConstantString as qfcs ->
            equals "$s_1$" qfcs.Value
        | _ ->
            testPrint outputOpt "Field 3 type incorrect."
            equals "QueryFieldConstantString" (cf3.Expression.GetType().Name)

        testPrint outputOpt "[PASSED] SELECT * FROM MASTER:ACCOUNT WHERE Username = @Username AND PasswordHash = @PasswordHash"

    let ``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ?Username AND PasswordHash = ?PasswordHash"`` =
        ``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"``

    type CommonTests (output:ITestOutputHelper) =
        [<Fact>]
        member this.``Parse "SELECT * FROM MASTER:ACCOUNT"`` () =
            ``Parse "SELECT * FROM MASTER:ACCOUNT"`` (Some output)

        [<Fact>]
        member this.``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"`` () =
            ``[Condition] Parse "SELECT * FROM MASTER:ACCOUNT WHERE Username = ¢IUsername AND PasswordHash = ¢IPasswordHash"`` (Some output)