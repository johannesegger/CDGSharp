module CDG.KaraokeGenerator

open System
open System.Diagnostics

type Font = {
    Name: string
    Size: int
}

type Text = {
    Content: string
    Font: Font
}

type LinePart = {
    Text: string
    Duration: TimeSpan
}

type TitlePageData = {
    DisplayDuration: TimeSpan
    SongTitle: Text
    Artist: Text
    Color: Color
}

type LyricsPageData = {
    NotSungYetColor: Color
    SungColor: Color
    Font: Font
    Lines: LinePart list list
}

type CommandType =
    | ShowTitlePage of TitlePageData
    | ShowLyricsPage of LyricsPageData

type Command = {
    StartTime: TimeSpan
    BackgroundColor: Color
    CommandType: CommandType
}

module KaraokeGenerator =
    type private Position =
        | Center
        | OffsetStart of int
        | OffsetEnd of int
    module private Position =
        let getOffsetStart textSize imageSize = function
            | Center -> (imageSize - textSize) / 2
            | OffsetStart v -> v
            | OffsetEnd v -> imageSize - textSize - v

    let private getRenderDuration packetCount =
        TimeSpan((int64 packetCount * 1_000_000_0L) / (75L * 4L))

    let private getPacketCount (duration: TimeSpan) =
        (duration.Ticks * 75L * 4L) / 1_000_000_0L |> int

    let private tryGetFillingPackets timeToFill =
        if timeToFill < TimeSpan.Zero then None
        else
            let count = getPacketCount timeToFill
            List.replicate count SubCodePacket.empty |> Some

    let private renderText text foregroundColor backgroundColor =
        ImageProcessing.renderText text.Content text.Font.Name text.Font.Size foregroundColor backgroundColor
        |> Array2D.map (fun color -> if color = backgroundColor then None else Some color)

    let private generateTiles x y pixels =
        let (textWidth, textHeight) = (Array2D.length2 pixels, Array2D.length1 pixels)

        let x = Position.getOffsetStart textWidth Display.contentWidth x
        let xOffsetFromTile = x % TileBlock.width
        let y = Position.getOffsetStart textHeight Display.contentHeight y
        let yOffsetFromTile = y % TileBlock.height

        let startRow = y / TileBlock.height
        let startColumn = x / TileBlock.width

        let rows =
            float (yOffsetFromTile + textHeight) / float TileBlock.height
            |> Math.Ceiling
            |> int
        let columns =
            float (xOffsetFromTile + textWidth) / float TileBlock.width
            |> Math.Ceiling
            |> int
        
        Array2D.initBased startRow startColumn rows columns (fun row column ->
            let xStart = (column - startColumn) * TileBlock.width
            let yStart = (row - startRow) * TileBlock.height
            Array2D.init TileBlock.height TileBlock.width (fun y x ->
                let sourceX = xStart + x - xOffsetFromTile
                let sourceY = yStart + y - yOffsetFromTile
                if sourceX < 0 || sourceY < 0 || sourceX >= textWidth || sourceY >= textHeight then None
                else pixels.[sourceY, sourceX]
            )
        )

    let private renderTiledText text x y foregroundColor backgroundColor =
        renderText text foregroundColor backgroundColor
        |> generateTiles x y

    let private renderTiledLines lines font x y lineHeight foregroundColor backgroundColor =
        let totalHeight = (List.length lines - 1) * lineHeight + font.Size
        let offsetY = Position.getOffsetStart totalHeight Display.contentHeight y
        lines
        |> List.mapi (fun i lineParts ->
            let renderedLineParts =
                lineParts
                |> List.map (fun linePart ->
                    let renderedText = renderText { Content = linePart.Text; Font = font } foregroundColor backgroundColor
                    (linePart, renderedText)
                )
            let totalWidth = renderedLineParts |> List.sumBy (snd >> Array2D.length2)
            let offsetX = Position.getOffsetStart totalWidth Display.contentWidth x
            (([], offsetX), renderedLineParts)
            ||> List.fold (fun (tiles, offsetX) (linePart, renderedText) ->
                let partWidth = Array2D.length2 renderedText
                let partTiles = generateTiles (OffsetStart offsetX) (OffsetStart (offsetY + i * lineHeight)) renderedText
                ((partTiles, linePart.Duration) :: tiles, offsetX + partWidth)
            )
            |> fst
            |> List.rev
        )

    let private getColorTable backgroundColor colors =
        let colorTable =
            colors
            |> Seq.distinct
            |> Seq.choose id
            |> Seq.append [ backgroundColor ]
            |> Seq.toList

        if List.length colorTable > 16 then
            printfn $"WARNING: Page can only contain 16 colors, but contains {List.length colorTable}"
        seq {
            yield! colorTable
            while true do yield Color.black
        }
        |> Seq.take 16
        |> Seq.toArray

    let private sliceLineParts (lineParts: (Color option[,][,] * TimeSpan) list) =
        lineParts
        |> List.collect (fun (linePartTiles, duration) ->
            [
                let width = Array2D.length2 linePartTiles * TileBlock.width
                for column in Array2D.indices2 linePartTiles do
                let sliceWidth = 3
                let sliceDisplayDuration = duration / float width * float sliceWidth
                for sliceX in [ 0..sliceWidth..TileBlock.width - 1] do
                    let slice =
                        linePartTiles
                        |> Array2D.slice (Array2D.base1 linePartTiles) column (Array2D.length1 linePartTiles) 1
                        |> Array2D.map (fun row ->
                            row
                            |> Array2D.mapi (fun _y x v -> if x >= sliceX && x < sliceX + sliceWidth then v else None)
                        )
                    (slice, sliceDisplayDuration)
            ]
        )

    let private tileBlocks (tiles: Color option[,][,]) (colorTable: Color array) =
        let colorMap =
            colorTable
            |> Seq.mapi (fun i v -> (v, ColorIndex (byte i)))
            |> Map.ofSeq

        [
            for column in Seq.init (Array2D.length2 tiles) (fun v -> v + Array2D.base2 tiles) do
            for row in Seq.init (Array2D.length1 tiles) (fun v -> v + Array2D.base1 tiles) do
            for colorIndex in Map.values colorMap do
                let pixelRows =
                    tiles.[row, column]
                    |> Array2D.map (fun color ->
                        let pixelColorIndex =
                            color
                            |> Option.map (fun v -> Map.find v colorMap)
                        if pixelColorIndex = Some colorIndex then 1uy else 0uy
                    )
                    |> Array2D.toSequence
                    |> Seq.map PixelRow.create
                    |> Seq.toArray
                if pixelRows |> Array.exists (fun (PixelRow v) -> v > 0uy) then
                    let tileBlockData = {
                        Color1 = ColorIndex 0uy
                        Color2 = colorIndex
                        Row = byte row + 1uy |> Row
                        Column = byte column + 1uy |> Column
                        PixelRows = pixelRows
                    }
                    TileBlock (XORTileBlock, tileBlockData) |> CDGPacket
        ]

    let private getTitlePagePackets backgroundColor data =
        let titleTiles = renderTiledText data.SongTitle Center (OffsetStart (5 * TileBlock.height)) data.Color backgroundColor
        let artistTiles = renderTiledText data.Artist Center (OffsetStart (12 * TileBlock.height)) data.Color backgroundColor

        let colorTable =
            [
                titleTiles
                artistTiles
            ]
            |> Seq.collect (Array2D.toFlatSequence >> Seq.collect Array2D.toFlatSequence)
            |> getColorTable backgroundColor

        [
            MemoryPreset (ColorIndex 0uy, Repeat 0uy) |> CDGPacket
            LoadColorTableLow colorTable.[0..7] |> CDGPacket
            LoadColorTableHigh colorTable.[8..15] |> CDGPacket
            yield! tileBlocks titleTiles colorTable
            yield! tileBlocks artistTiles colorTable
        ]

    let private getLyricsPagePackets backgroundColor data =
        let tiles = renderTiledLines data.Lines data.Font Center Center (data.Font.Size * 2) data.NotSungYetColor backgroundColor |> List.collect id
        let sungTiles = renderTiledLines data.Lines data.Font Center Center (data.Font.Size * 2) data.SungColor backgroundColor |> List.collect id

        let colorTable =
            (tiles @ sungTiles)
            |> Seq.collect (fst >> Array2D.toFlatSequence)
            |> Seq.collect Array2D.toFlatSequence
            |> getColorTable backgroundColor

        [
            MemoryPreset (ColorIndex 0uy, Repeat 0uy) |> CDGPacket
            LoadColorTableLow colorTable.[0..7] |> CDGPacket
            LoadColorTableHigh colorTable.[8..15] |> CDGPacket
            yield!
                tiles
                |> List.collect (fun (lineTiles, _duration) ->
                    tileBlocks lineTiles colorTable
                )
        ],
        [
            yield!
                sungTiles
                |> sliceLineParts
                |> List.collect (fun (tiles, displayDuration) ->
                    let instructions = tileBlocks tiles colorTable
                    let renderDuration = getRenderDuration instructions.Length
                    let timeToFill = displayDuration - renderDuration
                    let fillingPackets =
                        tryGetFillingPackets timeToFill
                        |> Option.defaultWith (fun () ->
                            printfn $"WARNING: Rendering {instructions.Length} tile blocks takes longer than time available ({renderDuration} > {displayDuration})"
                            []
                        )
                    instructions @ fillingPackets
                )
        ]

    let private processCommand packets command =
        let currentRenderDuration =
            packets
            |> List.sumBy List.length
            |> getRenderDuration
        let newPackets =
            match command.CommandType with
            | ShowTitlePage data ->
                let packets = getTitlePagePackets command.BackgroundColor data
                let renderDuration = getRenderDuration packets.Length
                let fillingPackets =
                    tryGetFillingPackets (data.DisplayDuration - renderDuration)
                    |> Option.defaultWith (fun () ->
                        printfn $"WARNING: Rendering {packets.Length} tile blocks takes longer than time available ({renderDuration} > {data.DisplayDuration})"
                        []
                    )
                packets @ fillingPackets
            | ShowLyricsPage data ->
                let (initPackets, animationPackets) = getLyricsPagePackets command.BackgroundColor data
                let renderDuration = currentRenderDuration + (getRenderDuration initPackets.Length)
                let fillingPackets =
                    tryGetFillingPackets (command.StartTime - renderDuration)
                    |> Option.defaultWith (fun () ->
                        printfn $"WARNING: Lyrics page starting at {command.StartTime} can't be scheduled on time ({renderDuration} > {command.StartTime})"
                        []
                    )
                initPackets @ fillingPackets @ animationPackets

        newPackets :: packets

    let generate commands =
        ([], commands)
        ||> Array.fold processCommand
        |> List.rev
        |> List.collect id
        |> List.toArray
