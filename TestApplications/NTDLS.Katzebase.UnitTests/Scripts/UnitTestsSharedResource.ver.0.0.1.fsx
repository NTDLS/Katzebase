module Shared
#if INTERACTIVE
#r @"nuget: Newtonsoft.Json, 13.0.3"
#r @"nuget: NTDLS.DelegateThreadPooling, 1.4.8"
#r @"nuget: NTDLS.FastMemoryCache, 1.7.5"
#r @"nuget: NTDLS.Katzebase.Client, 1.7.8"
//#r @"nuget: NTDLS.Katzebase.Client.dev, 1.7.8.1"
//#r @"G:\coldfar_py\NTDLS.Katzebase.Client\bin\Debug\net8.0\NTDLS.Katzebase.Client.dll"
#r @"nuget: Serilog, 4.0.1"
#r @"nuget: NTDLS.Helpers, 1.2.9.0"
#r @"nuget: NTDLS.ReliableMessaging, 1.10.9.0"
#r @"../../../NTDLS.Katzebase.Shared/bin/Debug/net8.0/NTDLS.Katzebase.Shared.dll"
#r @"../../../NTDLS.Katzebase.Engine/bin/Debug/net8.0/NTDLS.Katzebase.Engine.dll"
#endif
open Newtonsoft.Json
open NTDLS.Katzebase.Shared
open NTDLS.Katzebase.Engine
open System.IO
open System.Reflection

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

let _core = new EngineCore(settings)