module CDG.Formatter

open CDG.BinaryFormat
open System
open System.IO

let private nl = Environment.NewLine

let formatFile path =
    File.ReadAllBytes(path)
    |> Array.map (fun v -> $"{v,2}")
    |> Array.chunkBySize (Sector.packetCount * SubCodePacket.dataLength)
    |> Array.map (Array.chunkBySize SubCodePacket.dataLength >> Array.map (String.concat " "))
    |> Array.map (String.concat nl)
    |> String.concat (nl + nl)
    |> printfn "%s"
