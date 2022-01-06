module CDG.Explainer

open System.IO

module ColorIndex =
    let explain (ColorIndex v) = $"{v}"

module Repeat =
    let explain (Repeat v) = $"{v}"

module Row =
    let explain (Row v) = $"{v}"

module Column =
    let explain (Column v) = $"{v}"

module PixelRow =
    let explain (PixelRow v) =
        [|5..-1..0|]
        |> Array.map (fun offset -> if v >>> offset &&& 1uy = 1uy then "1" else "0")
        |> String.concat " "

module TileBlockData =
    let explain v =
        let pixelRows = v.PixelRows |> Array.map (PixelRow.explain >> sprintf "    %s") |> String.concat "\r\n"
        $"Color1: {ColorIndex.explain v.Color1}, Color2: {ColorIndex.explain v.Color2}, Row: {Row.explain v.Row}, Column: {Column.explain v.Column}, Pixels:\r\n{pixelRows}"

module HScrollCommand =
    let explain = function
        | NoHScroll -> "Don't scroll"
        | ScrollLeft -> "Scroll left"
        | ScrollRight -> "Scroll right"

module HScrollOffset =
    let explain v = $"{v} pixels"

module HScroll =
    let explain (HScroll (command, offset)) =
        $"{HScrollCommand.explain command} - {HScrollOffset.explain offset}"

module VScrollCommand =
    let explain = function
        | NoVScroll -> "Don't scroll"
        | ScrollDown -> "Scroll down"
        | ScrollUp -> "Scroll up"

module VScrollOffset =
    let explain v = $"{v} pixels"

module VScroll =
    let explain (VScroll (command, offset)) =
        $"{VScrollCommand.explain command} - {VScrollOffset.explain offset}"

module ColorChannel =
    let explain (ColorChannel v) = $"{v}"

module Color =
    let explain v =
        $"Red: {ColorChannel.explain v.Red}, Green: {ColorChannel.explain v.Green}, Blue: {ColorChannel.explain v.Blue}"

module CDGPacketInstruction =
    let explain = function
        | MemoryPreset (color, repeat) -> $"Memory Preset: Color index: {ColorIndex.explain color}, Repeat: {Repeat.explain repeat}"
        | BorderPreset color -> $"Border Preset: Color index: {ColorIndex.explain color}"
        | TileBlock (ReplaceTileBlock, data) -> $"Tile Block: Replace: {TileBlockData.explain data}"
        | TileBlock (XORTileBlock, data) -> $"Tile Block: XOR: {TileBlockData.explain data}"
        | ScrollPreset (color, hScroll, vScroll) -> $"Scroll preset: Color index: {ColorIndex.explain color}, H-Scroll: {HScroll.explain hScroll}, V-Scroll: {VScroll.explain vScroll}"
        | ScrollCopy (hScroll, vScroll) -> $"Scroll copy: H-Scroll: {HScroll.explain hScroll}, V-Scroll: {VScroll.explain vScroll}"
        | DefineTransparentColor color -> $"Define transparent color: Color index: {ColorIndex.explain color}"
        | LoadColorTableLow colorSpecs ->
            let colors = colorSpecs |> Array.map (Color.explain >> sprintf "    %s") |> String.concat "\r\n"
            $"Load color table low: \r\n{colors}"
        | LoadColorTableHigh colorSpecs ->
            let colors = colorSpecs |> Array.map (Color.explain >> sprintf "    %s") |> String.concat "\r\n"
            $"Load color table high: \r\n{colors}"

module SubCodePacket =
    let explain = function
        | CDGPacket v -> $"CD+G: {CDGPacketInstruction.explain v}"
        | Other data ->
            data
            |> Array.map (fun v -> $"{v,2}")
            |> String.concat " "
            |> sprintf "Other: %s"

let explain =
    Array.mapi (fun i p -> $"{i}: {SubCodePacket.explain p}") >> String.concat "\r\n"

let explainFile path =
    File.ReadAllBytes(path)
    |> Parser.parse
    |> fun packets -> sprintf $"{explain packets}\r\n\r\n{packets.Length} packets"
    |> fun content -> File.WriteAllText(Path.ChangeExtension(path, ".cdg.explain"), content)
