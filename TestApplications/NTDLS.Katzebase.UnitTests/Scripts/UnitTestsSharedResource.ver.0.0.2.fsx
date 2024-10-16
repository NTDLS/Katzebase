module Shared
#if INTERACTIVE
#r @"nuget: Newtonsoft.Json, 13.0.3"
#r @"nuget: NTDLS.DelegateThreadPooling, 1.4.8"
#r @"nuget: NTDLS.FastMemoryCache, 1.7.5"
#r @"nuget: NTDLS.Katzebase.Api, 1.7.8"
//#r @"nuget: NTDLS.Katzebase.Api.dev, 1.7.8.1"
//#r @"G:\coldfar_py\NTDLS.Katzebase.Api\bin\Debug\net8.0\NTDLS.Katzebase.Api.dll"

#r @"nuget: Serilog, 4.0.1"
#r @"nuget: NTDLS.Helpers, 1.2.9.0"
#r @"nuget: NTDLS.ReliableMessaging, 1.10.9.0"
#r @"nuget: protobuf-net"
#r @"../../../NTDLS.Katzebase.Shared/bin/Debug/net8.0/NTDLS.Katzebase.Shared.dll"
#r @"../../../NTDLS.Katzebase.Engine/bin/Debug/net8.0/NTDLS.Katzebase.Engine.dll"
#endif
open Newtonsoft.Json
open NTDLS.Katzebase.Shared
open NTDLS.Katzebase.Engine
open NTDLS.Katzebase.Parsers.Query.Fields
open System
open System.IO
open System.Reflection
open ProtoBuf

let json = 
    try
        File.ReadAllText("appsettings.json");
    with
    | _ ->
        let fi = new FileInfo(Assembly.GetEntryAssembly().Location)
        File.ReadAllText(fi.DirectoryName + @"\appsettings.json")


let settings = JsonConvert.DeserializeObject<KatzebaseSettings>(json)

let createIfDirNotExisted p =
    let di = DirectoryInfo p
    if not di.Exists then
        di.Create()

createIfDirNotExisted settings.DataRootPath
createIfDirNotExisted settings.TransactionDataPath
createIfDirNotExisted settings.LogDirectory


//prevent missing single schema for ver.0.0.1
createIfDirNotExisted $"{settings.DataRootPath}/single"

#if GENERIC_TDATA
[<ProtoContract>]
type fstring (s) =
    [<ProtoMember(1)>]
    member val Value: string = s with get, set
    member this.me = this.Value
    interface IStringable with
        override this.GetKey () = this.Value
        override this.IsNullOrEmpty () = this.Value = null || this.Value = ""
        override this.ToLowerInvariant () = fstring(this.Value.ToLowerInvariant())
        override this.ToT<'T> () =
            match typeof<'T> with
            | t when t = typeof<string> -> box this.Value :?> 'T
            | t when t = typeof<double> -> box (Double.Parse this.Value) :?> 'T
            | t when t = typeof<int> -> box (Int32.Parse this.Value) :?> 'T
            | t when t = typeof<bool> -> box (Boolean.Parse this.Value) :?> 'T
            | t ->
                failwithf "type %s not supported" t.Name
        override this.ToT (t:Type) =
            match t with
            | t when t = typeof<string> -> box this.Value
            | t when t = typeof<double> -> box (Double.Parse this.Value)
            | t when t = typeof<int> -> box (Int32.Parse this.Value)
            | t when t = typeof<bool> -> box (Boolean.Parse this.Value)
            | t ->
                failwithf "type %s not supported" t.Name

        override this.ToNullableT<'T> () =
            match typeof<'T> with
            | t when t = typeof<string> -> box this.Value :?> 'T
            | t when t = typeof<double> -> box (Double.Parse this.Value) :?> 'T
            | t when t = typeof<int> -> box (Int32.Parse this.Value) :?> 'T
            | t when t = typeof<bool> -> box (Boolean.Parse this.Value) :?> 'T
            | t ->
                failwithf "type %s not supported" t.Name
    new () =
        fstring (null)
#endif

let inline getValue<'S, 'T when 'S : (member Value : 'T)> (v:'S) =
    v.Value

type String 
    with
        member inline this.V<'S, 'U when 'S : (member Value : 'U)>() = 
            let o = box this            
            o :?> 'U

        member this.me = this

#if GENERIC_TDATA


open NTDLS.Katzebase.Api.Payloads
type KbQueryDocumentListResult = KbQueryDocumentListResult<fstring>


type QueryFieldConstantNumeric      = QueryFieldConstantNumeric<fstring>
type QueryFieldConstantString       = QueryFieldConstantString<fstring>
type QueryFieldDocumentIdentifier   = QueryFieldDocumentIdentifier<fstring>

open NTDLS.Katzebase.Parsers
type StaticQueryParser = StaticQueryParser<fstring>

type QueryFieldConstantNumeric<'T 
    when 'T :> IStringable
    > 
    with
        member inline this.V<'S, 'U when 'S : (member Value : 'U)>() = 
            let o:obj = box this.Value
            try
                getValue (o :?> 'S)
            with
            | exn ->
                o :?> 'U

type QueryFieldConstantString<'T 
    when 'T :> IStringable
    > 
    with
        member inline this.V<'S, 'U when 'S : (member Value : 'U)>() = 
            let o = box this.Value
            try
                getValue (o :?> 'S)
            with
            | exn ->
                o :?> 'U

let _core = 
    new EngineCore<fstring>(
        settings
        , Func<string, fstring>(fun s -> fstring(s))
        , Func<string, fstring>(fun s -> fstring(s))
        , Func<fstring, fstring, int> (fun s1 s2 -> String.Compare(s1.Value, s2.Value))
        )
let preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), "admin", "")
open NTDLS.Katzebase.Engine.Sessions
let accounts = _core.Query.ExecuteQuery<Account<fstring>>(preLogin, $"SELECT Username, PasswordHash FROM Master:Account")
printfn "%A" (accounts |> Seq.toArray)

#else
let _core = new EngineCore(settings)
let preLogin = _core.Sessions.CreateSession(Guid.NewGuid(), "testUser", "testClient")
open NTDLS.Katzebase.Parsers.Query.Fields

type fstring = string
type QueryFieldConstantNumeric with
    member this.V<'S, 'U>() = this.Value
type QueryFieldConstantString with 
    member this.V<'S, 'U>() = this.Value


#endif






let testSchemaDDL = "testSchDDL"
let testSchemaDML = "testSchDML"

_core.Query.SystemExecuteAndCommitNonQuery($"DROP SCHEMA {testSchemaDDL}")
_core.Query.SystemExecuteAndCommitNonQuery($"CREATE SCHEMA {testSchemaDDL}")

_core.Query.SystemExecuteAndCommitNonQuery($"DROP SCHEMA {testSchemaDML}")
_core.Query.SystemExecuteAndCommitNonQuery($"CREATE SCHEMA {testSchemaDML}")