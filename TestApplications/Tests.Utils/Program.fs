namespace Test.Utils

type HasValue = {
    Value: int
}


module Util =
    let inline getValue<'S, 'T when 'S : (member Value : 'T)> (v:'S) =
        v.Value

    let inline getRun<'S, 'T when 'S : (member run : unit -> 'T)> (v:'S) =
        v.run ()

    let gv () =
        getValue {Value=456}

    printfn "Hello from F#, %d" (gv())


open Util