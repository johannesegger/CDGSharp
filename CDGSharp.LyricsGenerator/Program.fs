open Argu
open CDG.LrcParser
open NAudio.Utils
open NAudio.Wave
open System
open System.IO

type Position = Index of LyricsIndex | End

let insertStartTime time lyrics index =
    LyricsIndex.updateWord
        lyrics
        (fun word -> { word with StartTime = Some time })
        index

let insertEndTime time lyrics index =
    LyricsIndex.updateWord
        lyrics
        (fun word -> { word with EndTime = Some time })
        index

let writeLyricsLine (Lyrics pages) position =
    let (words, index) =
        match position with
        | Index v ->
            pages.[v.PageIndex].[v.LineIndex], v
        | End ->
            let index = LyricsIndex.last (Lyrics pages)
            let words = pages.[index.PageIndex].[index.LineIndex] @ [ { Text = " "; StartTime = None; EndTime = None } ]
            words, { index with WordIndex = words.Length - 1 }

    Console.CursorLeft <- 0
    words
    |> List.iteri (fun i word ->
        if i > 0 then printf " "
        if Option.isSome word.StartTime && Option.isSome word.EndTime then Console.ForegroundColor <- ConsoleColor.Green
        elif Option.isSome word.StartTime then Console.ForegroundColor <- ConsoleColor.Yellow
        if i = index.WordIndex then Console.BackgroundColor <- ConsoleColor.DarkGray
        printf "%s" word.Text
        Console.ResetColor()
    )

let updateLyricsLine lyrics oldIndex newIndex =
    match newIndex with
    | Some newIndex ->
        if newIndex.PageIndex > oldIndex.PageIndex then
            writeLyricsLine lyrics (Index { oldIndex with WordIndex = oldIndex.WordIndex + 1 })
            printf "%s" Lyrics.pageSeparator
        elif newIndex.LineIndex > oldIndex.LineIndex then
            writeLyricsLine lyrics (Index { oldIndex with WordIndex = oldIndex.WordIndex + 1 })
            printf "%s" LyricsPage.lineSeparator
    | None ->
        writeLyricsLine lyrics (Index { oldIndex with WordIndex = oldIndex.WordIndex + 1 })
        printf "%s" Lyrics.pageSeparator

let addTimes lyrics getTime =
    let rec fn lyrics position =
        writeLyricsLine lyrics position
        let input = Console.ReadKey(intercept = true).Key
        let time = getTime ()
        match position, input with
        | Index index, ConsoleKey.S ->
            let lyrics = insertStartTime time lyrics index
            match LyricsIndex.nextWord lyrics index with
            | Some nextIndex ->
                updateLyricsLine lyrics index (Some nextIndex)
                fn lyrics (Index nextIndex)
            | None ->
                fn lyrics End
        | End, ConsoleKey.S -> fn lyrics position
        | Index index, ConsoleKey.E ->
            match LyricsIndex.previousWord lyrics index with
            | Some previousIndex ->
                let lyrics = insertEndTime time lyrics previousIndex
                fn lyrics (Index index)
            | None ->
                fn lyrics (Index index)
        | End, ConsoleKey.E ->
            let lyrics = insertEndTime time lyrics (LyricsIndex.last lyrics)
            fn lyrics position
        | _, ConsoleKey.Escape ->
            writeLyricsLine lyrics position
            printf "%s" Lyrics.pageSeparator
            lyrics
        | _ -> fn lyrics position
    fn lyrics (Index LyricsIndex.zero)

let playAudio (path: string) speedFactor =
    let reader = new Mp3FileReaderBase(path, Mp3FileReaderBase.FrameDecompressorBuilder(fun v -> new AcmMp3FrameDecompressor(v)))
    let sampleRate = int (float reader.Mp3WaveFormat.SampleRate * speedFactor)
    let source = new RawSourceWaveStream(reader, WaveFormat(sampleRate, reader.Mp3WaveFormat.Channels))
    let waveOut = new WaveOutEvent()
    waveOut.Init(source)
    waveOut.Play()
    waveOut

let run lyrics audioPath speedFactor =
    printfn "Press <S> to set the start time of the current word."
    printfn "Press <E> to set the end time of the  word. This is only really necessary when there's a gap between two words and after the very last word."
    printfn "Press <Escape> to end playback."
    printfn ""
    let audio = playAudio audioPath speedFactor
    addTimes lyrics (fun () -> audio.GetPositionTimeSpan() * speedFactor)

let checkBitRate (audioPath: string) =
    let reader = new Mp3FileReaderBase(audioPath, Mp3FileReaderBase.FrameDecompressorBuilder(fun v -> new AcmMp3FrameDecompressor(v)))
    let frames = [
        let mutable frame = reader.ReadNextFrame()
        while not <| isNull frame do
            yield frame
            frame <- reader.ReadNextFrame()
    ]
    let minBitRate = frames |> List.map (fun v -> v.BitRate) |> List.min
    let maxBitRate = frames |> List.map (fun v -> v.BitRate) |> List.max
    if minBitRate <> maxBitRate then
        printfn $"WARNING: Audio file should have a constant bitrate, but bitrates are between %d{minBitRate / 1000} kbps and %d{maxBitRate / 1000} kbps"
        printfn ""

type CliArgs =
    | Lyrics_Path of string
    | Audio_Path of string
    | Target_Path of string
    | Speed_Factor of float

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Lyrics_Path _ -> "Path to a text file that contains the raw lyrics."
            | Audio_Path _ -> "Path to the audio file that is played during generation of the .lrc file."
            | Target_Path _ -> "Path to the generated .lrc file."
            | Speed_Factor _ -> "Controls the speed of the audio file, defaults to '0.5'."

[<EntryPoint>]
let main args =
    try
        let parser = ArgumentParser.Create<CliArgs>()
        let parseResults = parser.ParseCommandLine(inputs = args)
        let lyrics =
            parseResults.GetResult(Lyrics_Path)
            |> File.ReadLines
            |> Seq.splitBy String.IsNullOrWhiteSpace
            |> Lyrics.parse
        let audioPath = parseResults.GetResult(Audio_Path)
        let targetPath = parseResults.GetResult(Target_Path)
        let speedFactor = parseResults.GetResult(Speed_Factor, defaultValue = 0.5)

        checkBitRate audioPath

        run lyrics audioPath speedFactor
        |> Lyrics.toString
        |> fun v -> File.WriteAllText(targetPath, v)

        printfn $"Output written to %s{targetPath}"
    with e ->
        printfn "%s" e.Message
    0
