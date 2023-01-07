open Argu
open CDG
open CDG.ImageProcessing
open CDG.KaraokeGenerator
open CDG.LrcToKaraoke
open CDG.LrcParser
open SixLabors.ImageSharp
open System
open System.IO
open System.Text.RegularExpressions

type FormatArgs =
    | [<AltCommandLine("-f")>] File_Path of path:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | File_Path _ -> "The path to the .cdg file."

type ExplainArgs =
    | [<AltCommandLine("-f")>] File_Path of path:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | File_Path _ -> "The path to the .cdg file."

type RenderImagesArgs =
    | [<AltCommandLine("-f")>] File_Path of path:string
    | [<AltCommandLine("-o")>] Output_Directory of path:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | File_Path _ -> "The path to the .cdg file."
            | Output_Directory _ -> "The target directory for the output images."

type ConvertLrcArgs =
    | [<AltCommandLine("-f")>] File_Path of path:string
    | [<AltCommandLine("-u")>] Uppercase_Text
    | [<AltCommandLine("-m")>] Modify_Timestamps of seconds:float
    | Bg_Color of color:string
    | Text_Color of color:string
    | Sung_Text_Color of color:string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | File_Path _ -> "The path to the .lrc file."
            | Uppercase_Text _ -> "Transform text to uppercase."
            | Modify_Timestamps _ -> "Modify every timestamp in the .lrc file to align the lyrics with the audio file."
            | Bg_Color _ -> "Background color (RGB, 4 bits per channel, defaults to '#008')."
            | Text_Color _ -> "Text color (RGB, 4 bits per channel, defaults to '#FFF')."
            | Sung_Text_Color _ -> "Text color for sung text (RGB, 4 bits per channel, defaults to '#666')."

type CliArgs =
    | [<CliPrefix(CliPrefix.None)>] Format of ParseResults<FormatArgs>
    | [<CliPrefix(CliPrefix.None)>] Explain of ParseResults<ExplainArgs>
    | [<CliPrefix(CliPrefix.None)>] Render_Images of ParseResults<RenderImagesArgs>
    | [<CliPrefix(CliPrefix.None)>] Convert_Lrc of ParseResults<ConvertLrcArgs>

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Format _ -> "Format packets of a .cdg file."
            | Explain _ -> "Explain packets of a .cdg file."
            | Render_Images _ -> "Render images of a .cdg file."
            | Convert_Lrc _ -> "Convert an .lrc file into a .cdg file."

let parseColorChannel v =
    Convert.ToByte(v, 16)

let parseColor (v: string) =
    let m = Regex.Match(v, "^#([0-9A-F]){3}$")
    if m.Success then
        let r = m.Groups.[1].Captures.[0].Value |> parseColorChannel
        let g = m.Groups.[1].Captures.[1].Value |> parseColorChannel
        let b = m.Groups.[1].Captures.[2].Value |> parseColorChannel
        { Red = ColorChannel r; Green = ColorChannel g; Blue = ColorChannel b }
    else failwith $"Can't parse \"%s{v}\" as color."

[<EntryPoint>]
let main args =
    try
        let parser = ArgumentParser.Create<CliArgs>()
        let parseResults = parser.ParseCommandLine(inputs = args)
        match parseResults.GetSubCommand() with
        | Format v ->
            let filePath = v.GetResult(FormatArgs.File_Path)
            Formatter.formatFile filePath
        | Explain v ->
            let filePath = v.GetResult(ExplainArgs.File_Path)
            Explainer.explainFile filePath
        | Render_Images v ->
            let filePath = v.GetResult(RenderImagesArgs.File_Path)
            let targetDir =
                let baseDir = v.GetResult(RenderImagesArgs.Output_Directory, defaultValue = ".")
                Path.Combine(baseDir, Path.GetFileNameWithoutExtension(filePath))
            
            try
                Directory.Delete(targetDir, recursive = true)
            with :? DirectoryNotFoundException -> ()
            Directory.CreateDirectory(targetDir) |> ignore

            ImageRenderer.renderImagesFromFile filePath
            |> Seq.iter (fun (time, image) -> image.SaveAsBmp(Path.Combine(targetDir, $"{(time):``mm\-ss\-fffffff``}.bmp")))
        | Convert_Lrc v ->
            let filePath = v.GetResult(ConvertLrcArgs.File_Path)
            let targetFilePath = Path.ChangeExtension(filePath, ".cdg")
            let uppercaseText = v.Contains(ConvertLrcArgs.Uppercase_Text)
            let modifyTimes = v.GetResult(ConvertLrcArgs.Modify_Timestamps, defaultValue = 0.) |> TimeSpan.FromSeconds
            let bgColor =
                v.TryPostProcessResult(ConvertLrcArgs.Bg_Color, parseColor)
                |> Option.defaultValue { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 8uy }
            let textColor =
                v.TryPostProcessResult(ConvertLrcArgs.Text_Color, parseColor)
                |> Option.defaultValue { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
            let sungTextColor =
                v.TryPostProcessResult(ConvertLrcArgs.Sung_Text_Color, parseColor)
                |> Option.defaultValue { Red = ColorChannel 6uy; Green = ColorChannel 6uy; Blue = ColorChannel 6uy }
            let settings = {
                BackgroundColor = bgColor
                DefaultTextColor = textColor
                SungTextColor = sungTextColor
                DefaultFont =
                    // let fontDir = Path.Combine(Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location), "fonts")
                    // { Type = CustomFont (Path.Combine(fontDir, "OldSchoolAdventures-42j9.ttf")); Size = 15; Style = Regular }
                    { Type = SystemFont "Arial"; Style = Bold; Size = 16 }
            }

            LrcFile.parseFile filePath
            |> if uppercaseText then LrcFile.textToUpper else id
            |> if modifyTimes <> TimeSpan.Zero then LrcFile.modifyTimes (fun v -> v.Add(modifyTimes)) else id
            |> LrcToKaraoke.getKaraokeCommands settings
            |> KaraokeGenerator.generate
            |> Serializer.serialize
            |> fun content -> File.WriteAllBytes(targetFilePath, content)

    with e ->
        printfn "%s" e.Message
    0
