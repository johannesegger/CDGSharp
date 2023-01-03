open CDG
open CDG.ImageProcessing
open CDG.KaraokeGenerator
open CDG.LrcToKaraoke
open CDG.LrcParser
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

// let backgroundColor = { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 8uy }
// let defaultTextColor = { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
// let sungTextColor = { Red = ColorChannel 8uy; Green = ColorChannel 0uy; Blue = ColorChannel 0uy }
// let fontDir = Path.Combine(Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location), "fonts")
// // let defaultFont = { Type = CustomFont (Path.Combine(fontDir, "Retro Gaming.ttf")); Size = 15 }
// // let defaultFont = { Type = CustomFont (Path.Combine(fontDir, "PixeloidMono-1G8ae.ttf")); Size = 15 }
// let defaultFont = { Type = CustomFont (Path.Combine(fontDir, "OldSchoolAdventures-42j9.ttf")); Size = 15 }
// // let defaultFont = { Type = SystemFont "Arial"; Size = 20 }
// let space = { Text = " "; Duration = TimeSpan.Zero }
// let pause duration = { Text = ""; Duration = duration }
// let bpm = 128.295
// let quarterNoteTime = TimeSpan.FromMinutes(1.) / float bpm
// let eighthNoteTime = quarterNoteTime / 2.
// let halfNoteTime = quarterNoteTime * 2.
// let barTime = quarterNoteTime * 4.
// let t1_8 text = { Text = text; Duration = eighthNoteTime }
// let t1_4 text = { Text = text; Duration = quarterNoteTime }
// let t3_8 text = { Text = text; Duration = 3. * eighthNoteTime }
// let t1_2 text = { Text = text; Duration = halfNoteTime }
// let t5_8 text = { Text = text; Duration = 5. * eighthNoteTime }
// let t3_4 text = { Text = text; Duration = 3. * quarterNoteTime }
// let p1_8 = { Text = ""; Duration = eighthNoteTime }
// let p1_4 = { Text = ""; Duration = quarterNoteTime }
// let p3_8 = { Text = ""; Duration = 3. * eighthNoteTime }
// let p1_2 = { Text = ""; Duration = halfNoteTime }
// let offset = 4. * barTime
// let lyricsPage startTime lines =
//     {
//         StartTime = offset + startTime
//         BackgroundColor = backgroundColor
//         CommandType = ShowLyricsPage {
//             NotSungYetColor = defaultTextColor
//             SungColor = sungTextColor
//             Font = defaultFont
//             Lines =
//                 let prependSpace (part: LinePart) = [
//                     if part.Text <> "" then space
//                     part
//                 ]
//                 lines
//                 |> List.map (List.collect prependSpace >> List.skip 1) 
//         }
//     }

// [
//     {
//         StartTime = TimeSpan.Zero
//         BackgroundColor = backgroundColor
//         CommandType = ShowTitlePage {
//             DisplayDuration = TimeSpan.FromSeconds 5.
//             SongTitle = { Text = { Content = "Atemlos"; Font = { defaultFont with Size = 40 } }; X = Center; Y = OffsetStart (4 * TileBlock.height) }
//             Artist = { Text = { Content = "Helene Fischer"; Font = defaultFont }; X = Center; Y = OffsetStart (12 * TileBlock.height) }
//             Color = defaultTextColor
//         }
//     }
//     let refrainShort start = [
//         lyricsPage (float start * barTime - 2. * quarterNoteTime) [ [t3_4 "Atemlos"; p1_4; t1_4 "durch"; t1_8 "die"; t1_4 "Nacht"; p3_8]; [t1_4 "Spür,"; t1_8 "was"; t1_2 "Liebe"]; [t1_4 "mit"; t1_4 "uns"; t1_4 "macht"] ]
//         lyricsPage ((float start + 4.) * barTime - 2. * quarterNoteTime) [ [t3_4 "Atemlos,"; p1_4; t5_8 "schwindelfrei"; p3_8]; [t3_8 "großes"; t1_2 "Kino"; t1_4 "für"; t1_4 "uns"; t1_4 "zwei"] ]
//         lyricsPage ((float start + 8.) * barTime) [ [t1_8 "Wir"; t1_8 "sind"; t1_4 "heute"; t1_4 "ewig,"; p1_8]; [ t3_8 "tausend"; t1_2 "Glücksgefühle"; p1_4 ]; [t1_4 "Alles"; t1_8 "was"; t1_8 "ich"; t1_8 "bin"; p1_4]; [t1_8 "teil'"; t1_4 "ich"; t1_8 "mit"; t1_4 "dir"] ]
//         lyricsPage ((float start + 12.) * barTime) [ [t1_8 "Wir"; t1_8 "sind"; t1_2 "unzertrennlich"; p1_8]; [ t1_2 "irgendwie"; t3_8 "unsterblich"; p1_4 ]; [t1_8 "Komm,"; t1_8 "nimm"; t1_4 "meine"; t1_8 "Hand"; p1_4]; [t1_8 "und"; t1_4 "geh"; t1_8 "mit"; t1_4 "mir"] ]
//     ]

//     let refrainLong start = [
//         lyricsPage (float start * barTime - 2. * quarterNoteTime) [ [t3_4 "Atemlos"; p1_4; t1_4 "durch"; t1_8 "die"; t1_4 "Nacht"; p3_8]; [t1_4 "Bis"; t1_8 "ein"; t5_8 "neuer"; t1_4 "Tag"; t1_2 "erwacht"] ]
//         lyricsPage ((float start + 4.) * barTime - 2. * quarterNoteTime) [ [t3_4 "Atemlos"; p1_4; t3_8 "einfach"; t1_4 "raus"; p3_8]; [t3_8 "Deine"; t1_2 "Augen"; t1_4 "ziehen"; t1_4 "mich"]; [t1_4 "aus"] ]
//         yield! refrainShort (start + 8)
//     ]
//     lyricsPage (0. * barTime - eighthNoteTime) [ [t1_8 "Wir"; t1_4 "zieh'n"; t1_8 "durch"; t1_8 "die"]; [t1_4 "Straßen"; t1_8 "und"; t1_8 "die"; t1_8 "Clubs"]; [t1_4 "dieser"; t1_8 "Stadt"] ]
//     lyricsPage (2. * barTime) [ [t1_8 "Das"; t1_8 "ist"; t1_4 "unsre"; t1_8 "Nacht,"]; [t1_8 "wie"; t1_8 "für"; t1_8 "uns"; t1_4 "beide"; t1_4 "gemacht,"; p1_4]; [t1_2 "oho,"; p1_2; t1_2 "oho"] ]
//     lyricsPage (6. * barTime - eighthNoteTime) [ [t1_8 "Ich"; t1_4 "schließe"; t1_4 "meine"; t1_4 "Augen"]; [t1_4 "lösche"; t1_4 "jedes"; t1_4 "Tabu"] ]
//     lyricsPage (8. * barTime) [ [t1_4 "Küsse"; t1_8 "auf"; t1_8 "der"; t1_8 "Haut,"]; [t1_8 "so"; t1_8 "wie"; t1_8 "ein"; t1_2 "Liebes-Tattoo"; p1_4]; [t1_2 "oho,"; p1_2; t1_2 "oho"] ]
//     lyricsPage (12. * barTime - 2. * quarterNoteTime) [ [t1_8 "Was"; t1_8 "das"; t1_4 "zwischen"; t1_4 "uns"; p1_2]; [t1_4 "auch"; t1_4 "ist,"; p1_4]; [t1_4 "Bilder,"; t1_8 "die"; t1_8 "man"; t1_4 "nie"; p1_2]; [t1_2 "vergisst"] ]
//     lyricsPage (16. * barTime - 2. * quarterNoteTime) [ [t1_8 "Und"; t1_8 "dein"; t1_8 "Blick"; t1_8 "hat"; t1_4 "mir"; p1_2]; [t1_2 "gezeigt,"; p1_4]; [t1_8 "das"; t1_8 "ist"; t1_4 "unsre"; t1_4 "Zeit"] ]
//     yield! refrainLong 20
//     lyricsPage (46. * barTime) [ [t1_8 "Komm,"; t1_8 "wir"; t1_4 "steigen"; t1_8 "auf"; t1_8 "das"]; [t1_4 "höchste"; t1_8 "Dach"; t1_4 "dieser"; t1_8 "Welt"] ]
//     lyricsPage (48. * barTime) [ [t1_4 "Halten"; t1_4 "einfach"; t1_8 "fest,"; t1_8 "was"]; [t1_8 "uns"; t5_8 "zusammenhält"; p1_4]; [t1_2 "oho,"; p1_2; t1_2 "oho"] ]
//     lyricsPage (52. * barTime) [ [t1_8 "Bist"; t1_8 "du"; t1_4 "richtig"; t1_4 "süchtig,"]; [t1_8 "Haut"; t1_8 "an"; t1_8 "Haut"; t1_8 "ganz"]; [t1_4 "berauscht"] ]
//     lyricsPage (54. * barTime) [ [t1_8 "Fall"; t1_8 "in"; t1_4 "meine"; t1_4 "Arme"]; [t1_8 "und"; t1_8 "der"; t1_4 "Fallschirm"]; [t1_8 "geht"; t1_8 "auf,"; p1_4]; [t1_2 "oho,"; p1_2; t1_2 "oho"] ]
//     lyricsPage (58. * barTime - 2. * quarterNoteTime) [ [t1_4 "Alles"; t1_8 "was"; t1_8 "ich"; t1_4 "will,"; p1_2]; [t1_4 "ist"; t1_4 "da,"; p1_4]; [t1_4 "große"; t1_4 "Freiheit"; t1_4 "pur"; p1_2]; [t1_8 "ganz"; t1_4 "nah"] ]
//     lyricsPage (62. * barTime - 2. * quarterNoteTime) [ [t1_8 "Nein,"; t1_8 "wir"; t1_4 "wollen"; t1_4 "hier"; p1_2]; [t1_4 "nicht"; t1_4 "weg,"; p1_4]; [t1_4 "alles"; t1_8 "ist"; t1_4 "perfekt"] ]
//     yield! refrainShort 66
//     lyricsPage (89. * barTime - 2. * quarterNoteTime) [ [t1_4 "Lust"; t3_8 "pulsiert"; t1_4 "auf"]; [t1_2 "meiner"; t1_4 "Haut"] ]
//     yield! refrainShort 91
// ]
// |> KaraokeGenerator.generate
// |> Serializer.serialize
// |> fun content -> File.WriteAllBytes("Helene Fischer - Atemlos.cdg", content)

// ImageRenderer.renderImagesFromCDGFile "Helene Fischer - Atemlos.cdg"

// let settings = {
//     BackgroundColor = { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 8uy }
//     DefaultTextColor = { Red = ColorChannel 15uy; Green = ColorChannel 15uy; Blue = ColorChannel 15uy }
//     SungTextColor = { Red = ColorChannel 8uy; Green = ColorChannel 0uy; Blue = ColorChannel 0uy }
//     DefaultFont =
//         let fontDir = Path.Combine(Path.GetDirectoryName(Reflection.Assembly.GetExecutingAssembly().Location), "fonts")
//         { Type = CustomFont (Path.Combine(fontDir, "OldSchoolAdventures-42j9.ttf")); Size = 15 }
// }
// LrcFile.parseFile "Matthias Reim - Verdammt Ich Lieb Dich.lrc"
// |> fun file -> { file with Metadata = { file.Metadata with Title = "Verdammt\nIch Lieb Dich" } }
// |> LrcFile.textToUpper
// |> LrcToKaraoke.getKaraokeCommands settings
// |> KaraokeGenerator.generate
// |> Serializer.serialize
// |> fun content -> File.WriteAllBytes("Matthias Reim - Verdammt Ich Lieb Dich.cdg", content)

ImageRenderer.renderImagesFromCDGFile "Matthias Reim - Verdammt Ich Lieb Dich.cdg"
