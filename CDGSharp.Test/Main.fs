module CDG.Test.Main

open Expecto

let tests = testList "All" [
    Parser.tests
    LrcToKaraoke.tests
]

[<EntryPoint>]
let main argv =
    runTestsWithArgs defaultConfig argv tests
