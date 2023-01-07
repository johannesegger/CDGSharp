open Argu
open CDG.LrcParser
open NAudio.Utils
open NAudio.Wave
open System
open System.IO

let insertStartTime time lyrics index =
    LyricsIndex.updateWord lyrics (fun word -> { word with StartTime = Some time }) index

let insertEndTime time lyrics index =
    LyricsIndex.updateWord lyrics (fun word -> { word with EndTime = Some time }) index

let writeLyricsLine (Lyrics pages) index =
    Console.CursorLeft <- 0
    let words = pages |> List.item index.PageIndex |> List.item index.LineIndex
    words
    |> List.iteri (fun i word ->
        if i < index.WordIndex then Console.ForegroundColor <- ConsoleColor.Green
        elif i = index.WordIndex then Console.ForegroundColor <- ConsoleColor.Yellow
        if i > 0 then printf " "
        printf "%s" word.Text
        Console.ResetColor()
    )

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

let addTimes lyrics getTime =
    let rec fn lyrics index =
        writeLyricsLine lyrics index
        let input = Console.ReadKey(intercept = true).Key
        if input = ConsoleKey.S then
            let lyrics = insertStartTime (getTime()) lyrics index
            match LyricsIndex.nextWord lyrics index with
            | Some newIndex ->
                updateLyricsLine lyrics index (Some newIndex)
                fn lyrics newIndex
            | None ->
                updateLyricsLine lyrics index None
                fn lyrics index
        elif input = ConsoleKey.E then
            match LyricsIndex.previousWord lyrics index with
            | Some previousIndex ->
                let lyrics = insertEndTime (getTime()) lyrics previousIndex
                fn lyrics index
            | None ->
                fn lyrics index
        elif input = ConsoleKey.Escape then
            let lyrics = insertEndTime (getTime()) lyrics index
            printf "%s" Lyrics.pageSeparator
            lyrics
        else fn lyrics index
    fn lyrics LyricsIndex.zero

let playAudio (path: string) speedFactor =
    let reader = new Mp3FileReaderBase(path, Mp3FileReaderBase.FrameDecompressorBuilder(fun v -> new AcmMp3FrameDecompressor(v)))
    let sampleRate = int (float reader.Mp3WaveFormat.SampleRate * speedFactor)
    let source = new RawSourceWaveStream(reader, WaveFormat(sampleRate, reader.Mp3WaveFormat.Channels))
    let waveOut = new WaveOutEvent()
    waveOut.Init(source)
    waveOut.Play()
    waveOut

let run lyrics audioPath speedFactor =
    printfn "Press <S> to set start time of current word."
    printfn "Press <E> to set end time of previous word. This is only really necessary if there's a gap between two words."
    printfn "Press <Escape> to end playback."
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
