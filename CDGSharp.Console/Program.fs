open CDG
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
//     yield! Array.replicate (4 * 75 * 5) (Other (Array.zeroCreate 24))
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
//     yield! Array.replicate (4 * 75 * 5) (Other (Array.zeroCreate 24))
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
//     // yield! Array.replicate (35 + 4 * 75) (Other (Array.zeroCreate 24))
// ]
// |> Serializer.serialize
// |> fun content -> File.WriteAllBytes("Atemlos.cdg", content)

ImageRenderer.renderImagesFromCDGFile "Anton-aus-Tirol.cdg"

// Formatter.formatFile "Anton-aus-Tirol.cdg"
// let expected =
//     File.ReadAllBytes "Anton-aus-Tirol.cdg"
//     |> Parser.parse
//     |> Serializer.serialize
//     |> fun content -> File.WriteAllBytes("Anton-aus-Tirol.out.cdg", content)
// Formatter.formatFile "Anton-aus-Tirol.out.cdg"
