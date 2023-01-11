module CDG.LrcParser

open System
open System.IO
open System.Text.RegularExpressions

module private String =
    let split (separator: string) (text: string) =
        text.Split(separator, StringSplitOptions.RemoveEmptyEntries)

module LyricsTime =
    let toString (v: TimeSpan) =
        sprintf "[%02d:%02d:%02d]" (int v.TotalMinutes) v.Seconds (v.Milliseconds / 10)

type LyricsWord = {
    Text: string
    StartTime: TimeSpan option
    EndTime: TimeSpan option
}
module LyricsWord =
    let parse text =
        let timePattern prefix = $@"(?<%s{prefix}>\[(?<%s{prefix}Minutes>\d{{2}}):(?<%s{prefix}Seconds>\d{{2}}):(?<%s{prefix}HundredthSeconds>\d{{2}})\])"
        let startTimePattern = timePattern "startTime"
        let endTimePattern = timePattern "endTime"
        let m = Regex.Match(text, $@"^{startTimePattern}?(?<text>[^\]]+){endTimePattern}?$")
        if not m.Success then failwithf "Can't parse word \"%s\"" text
        else
            {
                Text = m.Groups.["text"].Value
                StartTime =
                    if m.Groups.["startTime"].Success then
                        let minutes = int m.Groups.["startTimeMinutes"].Value
                        let seconds = int m.Groups.["startTimeSeconds"].Value
                        let hundredthSeconds = int m.Groups.["startTimeHundredthSeconds"].Value
                        Some (TimeSpan(0, 0, minutes, seconds, hundredthSeconds * 10))
                    else None
                EndTime =
                    if m.Groups.["endTime"].Success then
                        let minutes = int m.Groups.["endTimeMinutes"].Value
                        let seconds = int m.Groups.["endTimeSeconds"].Value
                        let hundredthSeconds = int m.Groups.["endTimeHundredthSeconds"].Value
                        Some (TimeSpan(0, 0, minutes, seconds, hundredthSeconds * 10))
                    else None
            }
    let toString word =
        [
            yield! word.StartTime |> Option.map LyricsTime.toString |> Option.toList
            word.Text
            yield! word.EndTime |> Option.map LyricsTime.toString |> Option.toList
        ]
        |> String.concat ""

type LyricsLine = LyricsWord list
module LyricsLine =
    let private wordSeparator = " "
    let parse =
        String.split wordSeparator
        >> Seq.map LyricsWord.parse
        >> Seq.toList
    let toString =
        List.map LyricsWord.toString
        >> String.concat wordSeparator

type LyricsPage = LyricsLine list
module LyricsPage =
    let lineSeparator = Environment.NewLine
    let parse =
        Seq.map LyricsLine.parse
        >> Seq.toList
    let toString =
        List.map LyricsLine.toString
        >> String.concat lineSeparator

type Lyrics = Lyrics of LyricsPage list
module Lyrics =
    let pageSeparator = Environment.NewLine + Environment.NewLine
    let parse =
        Seq.map LyricsPage.parse
        >> Seq.toList
        >> Lyrics
    let toString (Lyrics pages) =
        pages
        |> List.map LyricsPage.toString
        |> String.concat pageSeparator
    let map fn (Lyrics pages) =
        pages
        |> List.map (List.map (List.map fn))
        |> Lyrics

type LyricsIndex = {
    PageIndex: int
    LineIndex: int
    WordIndex: int
}
module LyricsIndex =
    let zero = { PageIndex = 0; LineIndex = 0; WordIndex = 0 }
    let last (Lyrics pages) =
        {
            PageIndex = List.length pages - 1
            LineIndex = (pages |> List.last |> List.length) - 1
            WordIndex = (pages |> List.last |> List.last |> List.length) - 1
        }
    let previousWord (Lyrics pages) index =
        if index.WordIndex > 0 then
            Some { index with WordIndex = index.WordIndex - 1 }
        elif index.LineIndex > 0 then
            let lineIndex = index.LineIndex - 1
            let words = pages |> List.item index.PageIndex |> List.item lineIndex
            let wordIndex = words.Length - 1
            Some { index with LineIndex = lineIndex; WordIndex = wordIndex }
        elif index.PageIndex > 0 then
            let pageIndex = index.PageIndex - 1
            let lines = pages |> List.item pageIndex
            let lineIndex = lines.Length - 1
            let words = lines |> List.item lineIndex 
            let wordIndex = words.Length - 1
            Some { index with PageIndex = pageIndex; LineIndex = lineIndex; WordIndex = wordIndex }
        else
            None
    let nextWord (Lyrics pages) index =
        let lines = pages |> List.item index.PageIndex
        let words = lines |> List.item index.LineIndex
        if List.length words > index.WordIndex + 1 then
            Some { index with WordIndex = index.WordIndex + 1 }
        elif List.length lines > index.LineIndex + 1 then
            Some { index with LineIndex = index.LineIndex + 1; WordIndex = 0 }
        elif List.length pages > index.PageIndex + 1 then
            Some { index with PageIndex = index.PageIndex + 1; LineIndex = 0; WordIndex = 0 }
        else
            None

    let getWord (Lyrics pages) index =
        pages
        |> List.item index.PageIndex
        |> List.item index.LineIndex
        |> List.item index.WordIndex

    let updateWord (Lyrics pages) fn index =
        pages
        |> List.mapi (fun i v ->
            if i = index.PageIndex then
                v
                |> List.mapi (fun i v ->
                    if i = index.LineIndex then
                        v
                        |> List.mapi (fun i v ->
                            if i = index.WordIndex then fn v
                            else v
                        )
                    else v
                )
            else v
        )
        |> Lyrics
    
    let tryPickNextWord fn lyrics index =
        let rec impl index offset =
            match nextWord lyrics index with
            | Some nextIndex ->
                let word = getWord lyrics nextIndex
                match fn word with
                | Some v -> Some ((nextIndex, offset + 1), v)
                | None -> impl nextIndex (offset + 1)
            | None -> None
        impl index 0

    let getIndicesWhile fn lyrics index =
        List.unfold
            (fun index ->
                match index with
                | Some index when fn index -> Some (index, nextWord lyrics index)
                | _ -> None
            )
            (Some index)

    let allIndices lyrics = getIndicesWhile (fun _ -> true) lyrics zero

type Metadata = {
        Artist: string
        Title: string
    }
module Metadata =
    let parse lines =
        let tryGetMetadataEntry key line =
            let m = Regex.Match(line, $"^\[%s{Regex.Escape(key)}:(?<value>[^]]+)\]$")
            if m.Success then Some m.Groups.["value"].Value
            else None
        let artist =
            lines
            |> List.tryPick (tryGetMetadataEntry "ar")
            |> Option.defaultWith (fun () -> failwithf "Can't find artist")
        let title =
            lines
            |> List.tryPick (tryGetMetadataEntry "ti")
            |> Option.defaultWith (fun () -> failwithf "Can't find title")
        {
            Artist = artist
            Title = title
        }

type LrcFile = {
    Metadata: Metadata
    Lyrics: Lyrics
}
module LrcFile =
    let parseFile path =
        match File.ReadLines(path) |> Seq.splitBy String.IsNullOrWhiteSpace with
        | metadata :: pages ->
            {
                Metadata = Metadata.parse metadata
                Lyrics = Lyrics.parse pages
            }
        | _ -> failwithf "Invalid file: Metadata must be separated from lyrics by empty line"
    
    let modifyTimes fn lrcFile =
        { lrcFile with
            Lyrics =
                lrcFile.Lyrics
                |> Lyrics.map (fun word ->
                    { word with
                        StartTime = word.StartTime |> Option.map fn
                        EndTime = word.EndTime |> Option.map fn
                    }
                )
        }

    let textToUpper lrcFile =
        { lrcFile with
            Metadata =
                { lrcFile.Metadata with
                    Artist = lrcFile.Metadata.Artist.ToUpper()
                    Title = lrcFile.Metadata.Title.ToUpper()
                }
            Lyrics =
                lrcFile.Lyrics
                |> Lyrics.map (fun word -> { word with Text = word.Text.ToUpper() })
        }
