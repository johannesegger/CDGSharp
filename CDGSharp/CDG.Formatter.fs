module CDG.Formatter

open System.IO

let formatFile path =
    File.ReadAllBytes(path)
    |> Array.map (fun v -> $"{v,2}")
    |> Array.chunkBySize (Sector.packetCount * SubCodePacket.length)
    |> Array.map (Array.chunkBySize SubCodePacket.length >> Array.map (String.concat " "))
    |> Array.map (String.concat "\r\n")
    |> String.concat "\r\n\r\n"
    |> fun v -> File.WriteAllText(Path.ChangeExtension(path, ".cdg.format"), v)
