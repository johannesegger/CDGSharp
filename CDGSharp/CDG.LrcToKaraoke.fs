module CDG.LrcToKaraoke

open CDG.KaraokeGenerator
open CDG.LrcParser
open System

type Settings = {
    BackgroundColor: Color
    DefaultTextColor: Color
    SungTextColor: Color
    DefaultFont: Font
}

module LrcToKaraoke =
    let private getShowTitlePageCommand settings metadata =
        {
            StartTime = TimeSpan.Zero
            BackgroundColor = settings.BackgroundColor
            CommandType = ShowTitlePage {
                DisplayDuration = TimeSpan.FromSeconds 5.
                SongTitle = { Text = { Content = metadata.Title; Font = { settings.DefaultFont with Size = settings.DefaultFont.Size * 7 / 6 } }; X = Center; Y = OffsetStart (4 * TileBlock.height) }
                Artist = { Text = { Content = metadata.Artist; Font = settings.DefaultFont }; X = Center; Y = OffsetStart (12 * TileBlock.height) }
                Color = settings.DefaultTextColor
            }
        }

    let private getShowLyricsPageCommands settings lyrics =
        let words indices startTime =
            ((startTime, []), indices)
            ||> List.fold (fun (startTime, list) index ->
                let word = LyricsIndex.getWord lyrics index
                let startTime = word.StartTime |> Option.defaultValue startTime
                let (duration, breakDuration) =
                    let nextWord =
                        LyricsIndex.nextWord lyrics index
                        |> Option.map (LyricsIndex.getWord lyrics)
                    match LyricsIndex.getWord lyrics index, nextWord with
                    | { EndTime = Some endTime }, Some { StartTime = Some nextWordStartTime} ->
                        (endTime - startTime, nextWordStartTime - endTime)
                    | { EndTime = Some endTime }, _ ->
                        (endTime - startTime, TimeSpan.Zero)
                    | _ -> 
                        match LyricsIndex.tryPickNextWord (fun word -> word.StartTime) lyrics index with
                        | Some ((_nextIndex, offset), nextStartTime) ->
                            ((nextStartTime - startTime) / float offset, TimeSpan.Zero)
                        | None -> failwithf "Can't determine duration of %A" index
                let part = {
                    Text = word.Text
                    Duration = duration
                }
                let list =
                    match LyricsIndex.nextWord lyrics index with // TODO this is ugly
                    | Some v when v.PageIndex <> index.PageIndex ->
                        list @ [ part ]
                    | Some v when v.LineIndex <> index.LineIndex ->
                        let breakPart = { Text = ""; Duration = breakDuration }
                        list @ [ part; breakPart ]
                    | Some _ ->
                        let breakPart = { Text = " "; Duration = breakDuration }
                        list @ [ part; breakPart ]
                    | None ->
                        list @ [ part ]
                (startTime + duration, list)
            )

        let lines indices startTime =
            ((startTime, []), indices)
            ||> List.fold (fun (startTime, lines) index ->
                let (nextStartTime, result) = words index startTime
                (nextStartTime, lines @ [ result ])
            )

        let indices =
            LyricsIndex.allIndices lyrics
            |> List.groupBy (fun v -> v.PageIndex)
            |> List.map (snd >> fun lines ->
                lines
                |> List.groupBy (fun v -> v.LineIndex)
                |> List.map snd
            )

        let startTime =
            LyricsIndex.getWord lyrics LyricsIndex.zero
            |> fun v -> v.StartTime
            |> Option.defaultValue TimeSpan.Zero
        ((startTime, []), indices)
        ||> List.fold (fun (startTime, list) index ->
            let (nextStartTime, result) = lines index startTime
            let page = {
                StartTime = startTime
                BackgroundColor = settings.BackgroundColor
                CommandType = ShowLyricsPage {
                    NotSungYetColor = settings.DefaultTextColor
                    SungColor = settings.SungTextColor
                    Font = settings.DefaultFont
                    Lines = result
                }
            }
            (nextStartTime, list @ [ page ])
        )
        |> snd

    let getKaraokeCommands settings lrcFile =
        [|
            getShowTitlePageCommand settings lrcFile.Metadata
            yield! getShowLyricsPageCommands settings lrcFile.Lyrics
        |]
