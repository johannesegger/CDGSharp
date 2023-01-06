﻿open Argu
open CDG
open CDG.ImageProcessing
open CDG.KaraokeGenerator
open CDG.LrcToKaraoke
open CDG.LrcParser
open SixLabors.ImageSharp
open System
open System.IO

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

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | File_Path _ -> "The path to the .lrc file."

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
            let settings = {
                BackgroundColor = { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 8uy }
                DefaultTextColor = { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
                SungTextColor = { Red = ColorChannel 6uy; Green = ColorChannel 6uy; Blue = ColorChannel 6uy }
                DefaultFont =
                    { Type = SystemFont "Arial"; Style = Bold; Size = 16 }
            }

            LrcFile.parseFile filePath
            |> LrcToKaraoke.getKaraokeCommands settings
            |> KaraokeGenerator.generate
            |> Serializer.serialize
            |> fun content -> File.WriteAllBytes(targetFilePath, content)

    with e ->
        printfn "%s" e.Message
    0
