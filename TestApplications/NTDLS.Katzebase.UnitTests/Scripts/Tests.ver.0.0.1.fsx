#if INTERACTIVE
#r "nuget: Xunit"
#endif

[<AutoOpen>]
module Tests

open Xunit

let equals (expected: 'a) (value: 'a) = Assert.Equal<'a>(expected, value)
let success = ()

let (|String|_|) (message: obj) =
    match message with
    | :? string as x -> Some x
    | _ -> None
