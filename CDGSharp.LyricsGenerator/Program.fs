open System
open System.Diagnostics
open System.IO
open NAudio.Utils
open NAudio.Wave

module String =
    let trim (text: string) =
        text.Trim()
    let split (separator: string) (text: string) =
        text.Split(separator)

module LyricsTime =
    let toString (v: TimeSpan) =
        sprintf "[%02d:%02d:%02d]" (int v.TotalMinutes) v.Seconds (v.Milliseconds / 10)

type LyricsWord = {
    Text: string
    StartTime: TimeSpan option
    End: TimeSpan option
}
module LyricsWord =
    let noTime text =
        { Text = text; StartTime = None; End = None }
    let toString word =
        [
            yield! word.StartTime |> Option.map LyricsTime.toString |> Option.toList
            word.Text
            yield! word.End |> Option.map LyricsTime.toString |> Option.toList
        ]
        |> String.concat ""

type LyricsLine = LyricsWord list
module LyricsLine =
    let private wordSeparator = " "
    let parse =
        String.split wordSeparator
        >> Seq.map LyricsWord.noTime
        >> Seq.toList
    let toString =
        List.map LyricsWord.toString
        >> String.concat wordSeparator

type LyricsPage = LyricsLine list
module LyricsPage =
    let lineSeparator = Environment.NewLine
    let parse =
        String.split lineSeparator
        >> Seq.map LyricsLine.parse
        >> Seq.toList
    let toString =
        List.map LyricsLine.toString
        >> String.concat lineSeparator

type Lyrics = Lyrics of LyricsPage list
module Lyrics =
    let pageSeparator = Environment.NewLine + Environment.NewLine
    let parse =
        String.trim
        >> String.split pageSeparator
        >> Seq.map LyricsPage.parse
        >> Seq.toList
        >> Lyrics
    let toString (Lyrics pages) =
        pages
        |> List.map LyricsPage.toString
        |> String.concat pageSeparator

type Index = {
    PageIndex: int
    LineIndex: int
    WordIndex: int
}
module Index =
    let zero = { PageIndex = 0; LineIndex = 0; WordIndex = 0 }
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

let updateWord (Lyrics pages) index fn =
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

let writeLyricsLine (Lyrics pages) index =
    Console.CursorLeft <- 0
    let words = pages |> List.item index.PageIndex |> List.item index.LineIndex
    words
    |> List.iteri (fun i word ->
        if i < index.WordIndex then Console.ForegroundColor <- ConsoleColor.Green
        elif i = index.WordIndex then Console.ForegroundColor <- ConsoleColor.Yellow
        else Console.ResetColor()
        if i > 0 then printf " "
        printf "%s" word.Text
    )
    Console.ResetColor()

let updateLyricsLine lyrics oldIndex newIndex =
    match newIndex with
    | Some newIndex ->
        if newIndex.PageIndex > oldIndex.PageIndex then
            writeLyricsLine lyrics { oldIndex with WordIndex = oldIndex.WordIndex + 1 }
            printf "%s" Lyrics.pageSeparator
        elif newIndex.LineIndex > oldIndex.LineIndex then
            writeLyricsLine lyrics { oldIndex with WordIndex = oldIndex.WordIndex + 1 }
            printf "%s" LyricsPage.lineSeparator
    | None ->
        writeLyricsLine lyrics { oldIndex with WordIndex = oldIndex.WordIndex + 1 }
        printf "%s" Lyrics.pageSeparator

let insertStartTime time lyrics index =
    updateWord lyrics index (fun word -> { word with StartTime = Some time })

let addTimes lyrics getTime =
    let rec fn lyrics index =
        writeLyricsLine lyrics index
        let input = Console.ReadKey(intercept = true).Key
        if input = ConsoleKey.T then
            let lyrics = insertStartTime (getTime()) lyrics index
            match Index.nextWord lyrics index with
            | Some newIndex ->
                updateLyricsLine lyrics index (Some newIndex)
                fn lyrics newIndex
            | None ->
                updateLyricsLine lyrics index None
                lyrics
        else fn lyrics index
    fn lyrics Index.zero

let playAudio (path: string) =
    let reader = new Mp3FileReaderBase(path, Mp3FileReaderBase.FrameDecompressorBuilder(fun v -> new AcmMp3FrameDecompressor(v)))
    let source = new RawSourceWaveStream(reader, WaveFormat(reader.Mp3WaveFormat.SampleRate * 3 / 4, reader.Mp3WaveFormat.Channels))
    let waveOut = new WaveOutEvent()
    waveOut.Init(source)
    waveOut.Play()
    waveOut

let run lyrics audioPath =
    let audio = playAudio audioPath
    addTimes lyrics (fun () -> audio.GetPositionTimeSpan())

let lyrics = File.ReadAllText "Matthias Reim - Verdammt Ich Lieb Dich.txt" |> Lyrics.parse
let audioPath = "Matthias Reim - Verdammt Ich Lieb Dich.mp3"
run lyrics audioPath
|> Lyrics.toString
|> printfn "%s"
