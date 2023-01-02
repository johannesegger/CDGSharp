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
        if input = ConsoleKey.T then
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

let run lyrics audioPath =
    printfn "Press <Escape> to end playback"
    let speedFactor = 0.5
    let audio = playAudio audioPath speedFactor
    addTimes lyrics (fun () -> audio.GetPositionTimeSpan() * speedFactor)

let lyrics = File.ReadLines "Matthias Reim - Verdammt Ich Lieb Dich.txt" |> Seq.splitBy String.IsNullOrWhiteSpace |> Lyrics.parse
let audioPath = "Matthias Reim - Verdammt Ich Lieb Dich.mp3"
run lyrics audioPath
|> Lyrics.toString
|> printfn "%s"
