module CDG.Formatter

open CDG.BinaryFormat
open System.IO

let formatFile path =
    File.ReadAllBytes(path)
    |> Array.map (fun v -> $"{v,2}")
    |> Array.chunkBySize (Sector.packetCount * SubCodePacket.dataLength)
    |> Array.map (Array.chunkBySize SubCodePacket.dataLength >> Array.map (String.concat " "))
    |> Array.map (String.concat "\r\n")
    |> String.concat "\r\n\r\n"
    |> fun v -> File.WriteAllText(Path.ChangeExtension(path, ".cdg.format"), v)
