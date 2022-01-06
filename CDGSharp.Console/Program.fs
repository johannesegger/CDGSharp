open CDG
open CDG.KaraokeGenerator
open System
open System.IO

// Explainer.explainFile "JingleBells.cdg"

// Formatter.formatFile "sample2.cdg"
// Formatter.formatFile "sample2.out.cdg"

// [|
//     CDGPacket {
//         Instruction =
//             LoadColorTableLow [|
//                 { Red = ColorChannel 0x0uy; Green = ColorChannel 0x0uy; Blue = ColorChannel 0x0uy }
//                 { Red = ColorChannel 0x0uy; Green = ColorChannel 0x0uy; Blue = ColorChannel 0xFuy }
//                 { Red = ColorChannel 0x0uy; Green = ColorChannel 0xFuy; Blue = ColorChannel 0x0uy }
//                 { Red = ColorChannel 0x0uy; Green = ColorChannel 0xFuy; Blue = ColorChannel 0xFuy }
//                 { Red = ColorChannel 0xFuy; Green = ColorChannel 0x0uy; Blue = ColorChannel 0x0uy }
//                 { Red = ColorChannel 0xFuy; Green = ColorChannel 0x0uy; Blue = ColorChannel 0xFuy }
//                 { Red = ColorChannel 0xFuy; Green = ColorChannel 0xFuy; Blue = ColorChannel 0x0uy }
//                 { Red = ColorChannel 0xFuy; Green = ColorChannel 0xFuy; Blue = ColorChannel 0xFuy }
//             |]
//     }
//     CDGPacket {
//         Instruction =
//             MemoryPreset (ColorIndex 7uy, Repeat 0uy)
//     }
//     CDGPacket {
//         Instruction =
//             BorderPreset (ColorIndex 1uy)
//     }
//     yield! Array.replicate (4 * 75 * 5) SubCodePacket.empty
//     CDGPacket {
//         Instruction =
//             TileBlock (
//                 ReplaceTileBlock,
//                 {
//                     Color1 = ColorIndex 0uy
//                     Color2 = ColorIndex 6uy
//                     Row = Row 2uy
//                     Column = Column 2uy
//                     PixelRows = [|
//                         yield! Array.replicate 12 (PixelRow 0b111111uy)
//                     |]
//                 }
//             )
//     }
//     yield! Array.replicate (4 * 75 * 5) SubCodePacket.empty
//     CDGPacket {
//         Instruction =
//             TileBlock (
//                 XORTileBlock,
//                 {
//                     Color1 = ColorIndex 0uy
//                     Color2 = ColorIndex 5uy
//                     Row = Row 2uy
//                     Column = Column 3uy
//                     PixelRows = [|
//                         yield! Array.replicate 12 (PixelRow 0b111111uy)
//                     |]
//                 }
//             )
//     }
// |]
// |> Serializer.serialize
// |> fun v -> File.WriteAllBytes("test.cdg", v)

// ImageRenderer.renderImagesFromCDGFile "test.cdg"

// ImageRenderer.renderImagesFromCDGFile "I will survive.cdg"

// Explainer.explainFile "Helene Fischer Atemlos durch die Nacht Karaoke by Rolf Rattay HD-ROnKMMQjFd8.cdg"

// File.ReadAllBytes("Helene Fischer Atemlos durch die Nacht Karaoke by Rolf Rattay HD-ROnKMMQjFd8.cdg")
// |> Parser.parse
// |> Array.skip 37
// |> Array.insertManyAt 1 [
//     CDGPacket {
//         Instruction =
//             MemoryPreset (ColorIndex 0uy, Repeat 0uy)
//     }
//     CDGPacket {
//         Instruction =
//             BorderPreset (ColorIndex 0uy)
//     }
//     // yield! Array.replicate (35 + 4 * 75) SubCodePacket.empty
// ]
// |> Serializer.serialize
// |> fun content -> File.WriteAllBytes("Atemlos.cdg", content)

// ImageRenderer.renderImagesFromCDGFile "Anton-aus-Tirol.cdg"

// Formatter.formatFile "Anton-aus-Tirol.cdg"
// let expected =
//     File.ReadAllBytes "Anton-aus-Tirol.cdg"
//     |> Parser.parse
//     |> Serializer.serialize
//     |> fun content -> File.WriteAllBytes("Anton-aus-Tirol.out.cdg", content)
// Formatter.formatFile "Anton-aus-Tirol.out.cdg"

let backgroundColor = { Red = ColorChannel 0uy; Green = ColorChannel 8uy; Blue = ColorChannel 0uy }
let defaultTextColor = { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
let sungTextColor = { Red = ColorChannel 0uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
let defaultFont = { Name = "Arial"; Size = 20 }
let space = { Text = " "; Duration = TimeSpan.Zero }
let bpm = 128
let quarterNoteTime = TimeSpan.FromMinutes(1.) / float bpm
let eighthNoteTime = quarterNoteTime / 2.
let barTime = quarterNoteTime * 4.
let offset = TimeSpan(0, 0, 0, 0, 900)
[|
    {
        StartTime = TimeSpan.Zero
        BackgroundColor = backgroundColor
        CommandType = ShowTitlePage {
            DisplayDuration = TimeSpan.FromSeconds 5.
            SongTitle = { Content = "Atemlos"; Font = { defaultFont with Size = 40 } }
            Artist = { Content = "Helene Fischer"; Font = defaultFont }
            Color = defaultTextColor
        }
    }
    {
        StartTime = offset + 4. * barTime - eighthNoteTime
        BackgroundColor = backgroundColor
        CommandType = ShowLyricsPage {
            NotSungYetColor = defaultTextColor
            SungColor = sungTextColor
            Font = defaultFont
            Lines = [
                [
                    { Text = "Wir"; Duration = eighthNoteTime }; space
                    { Text = "zieh'n"; Duration = quarterNoteTime }; space
                    { Text = "durch"; Duration = eighthNoteTime }; space
                    { Text = "die"; Duration = eighthNoteTime }
                ]
                [
                    { Text = "Straßen"; Duration = quarterNoteTime }; space
                    { Text = "und"; Duration = eighthNoteTime }; space
                    { Text = "die"; Duration = eighthNoteTime }; space
                    { Text = "Clubs"; Duration = eighthNoteTime }
                ]
                [
                    { Text = "dieser"; Duration = quarterNoteTime }; space
                    { Text = "Stadt"; Duration = quarterNoteTime }
                ]
            ]
        }
    }
    {
        StartTime = offset + 6. * barTime
        BackgroundColor = backgroundColor
        CommandType = ShowLyricsPage {
            NotSungYetColor = defaultTextColor
            SungColor = sungTextColor
            Font = defaultFont
            Lines = [
                [
                    { Text = "Das"; Duration = eighthNoteTime }; space
                    { Text = "ist"; Duration = eighthNoteTime }; space
                    { Text = "unsre"; Duration = quarterNoteTime }; space
                    { Text = "Nacht,"; Duration = eighthNoteTime }
                ]
                [
                    { Text = "wie"; Duration = eighthNoteTime }; space
                    { Text = "für"; Duration = eighthNoteTime }; space
                    { Text = "uns"; Duration = eighthNoteTime }; space
                    { Text = "beide"; Duration = quarterNoteTime }; space
                    { Text = "gemacht,"; Duration = quarterNoteTime }
                ]
                [
                    { Text = "oho"; Duration = TimeSpan(0, 0, 0, 0, 1000) }; space
                    { Text = "oho"; Duration = TimeSpan(0, 0, 0, 0, 1000) }
                ]
            ]
        }
    }
|]
|> KaraokeGenerator.generate
|> Serializer.serialize
|> fun content -> File.WriteAllBytes("Atemlos.out.cdg", content)

// ImageRenderer.renderImagesFromCDGFile "Atemlos.out.cdg"
