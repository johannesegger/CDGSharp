module Seq

let splitBy fn items =
    (items, ([], []))
    ||> Seq.foldBack (fun line (section, sections) ->
        if fn line then
            let sections' =
                if section |> List.isEmpty |> not then section :: sections
                else sections
            ([], sections')
        else (line :: section, sections)
    )
    |> function
    | [], sections -> sections
    | section, sections -> section :: sections
