module Tests
#if INTERACTIVE
#r "nuget: Xunit"
#endif


open Xunit
open Xunit.Abstractions

let equals (expected: 'a) (value: 'a) = Assert.Equal<'a>(expected, value)
let success = ()

let (|String|_|) (message: obj) =
    match message with
    | :? string as x -> Some x
    | _ -> None

let testPrint (outputOpt: ITestOutputHelper option) format =
    Printf.kprintf (fun str ->
        // write to console
        printfn "%s" str
        if outputOpt.IsSome then
        // write to xUnit test output
            outputOpt.Value.WriteLine(str)
    ) format