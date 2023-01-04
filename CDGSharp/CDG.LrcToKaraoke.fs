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
    let getShowTitlePageCommand settings metadata =
        {
            StartTime = TimeSpan.Zero
            BackgroundColor = settings.BackgroundColor
            CommandType = ShowTitlePage {
                SongTitle = { Text = { Content = metadata.Title; Font = { settings.DefaultFont with Size = settings.DefaultFont.Size * 7 / 6 } }; X = Center; Y = OffsetStart (4 * TileBlock.height) }
                Artist = { Text = { Content = metadata.Artist; Font = settings.DefaultFont }; X = Center; Y = OffsetStart (12 * TileBlock.height) }
                Color = settings.DefaultTextColor
            }
        }

    let getShowLyricsPageCommands settings lyrics =
        let getWordDuration index startTime =
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
        
        let words indices startTime =
            ((startTime, []), indices)
            ||> List.fold (fun (startTime, list) index ->
                let word = LyricsIndex.getWord lyrics index
                let startTime = word.StartTime |> Option.defaultValue startTime
                let (duration, breakDuration) = getWordDuration index startTime
                let part = {
                    Text = word.Text
                    Duration = duration
                }
                let breakPart =
                    match LyricsIndex.nextWord lyrics index with
                    | Some v when v.PageIndex <> index.PageIndex ->
                        None
                    | Some v when v.LineIndex <> index.LineIndex && breakDuration > TimeSpan.Zero ->
                        Some { Text = ""; Duration = breakDuration }
                    | Some v when v.LineIndex <> index.LineIndex ->
                        None
                    | Some _ ->
                        Some { Text = " "; Duration = breakDuration }
                    | None ->
                        None
                (startTime + duration, list @ [ part ] @ Option.toList breakPart)
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

        ((TimeSpan.Zero, []), indices)
        ||> List.fold (fun (startTime, list) index ->
            let startTime =
                index |> List.collect id |> List.tryHead
                |> Option.bind (LyricsIndex.getWord lyrics >> fun v -> v.StartTime)
                |> Option.defaultValue startTime
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
        [
            getShowTitlePageCommand settings lrcFile.Metadata
            yield! getShowLyricsPageCommands settings lrcFile.Lyrics
        ]
