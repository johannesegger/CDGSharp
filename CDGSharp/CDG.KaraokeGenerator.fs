module CDG.KaraokeGenerator

open System
open CDG.BinaryFormat
open CDG.ImageProcessing
open CDG.Renderer

type Font = {
    Type: FontType
    Size: int
}

type Text = {
    Content: string
    Font: Font
}

type Position =
    | Center
    | OffsetStart of int
    | OffsetEnd of int
module private Position =
    let getOffsetStart textSize imageSize = function
        | Center -> (imageSize - textSize) / 2
        | OffsetStart v -> v
        | OffsetEnd v -> imageSize - textSize - v

type PositionedText = {
    Text: Text
    X: Position
    Y: Position
}

type TitlePageData = {
    SongTitle: PositionedText
    Artist: PositionedText
    Color: Color
}

type LinePart = {
    Text: string
    Duration: TimeSpan
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
    type private TileColors = TileColors of Color option[,]
    module private TileColors =
        let init fn =
            Array2D.init TileBlock.height TileBlock.width (fun y x -> fn x y)
            |> TileColors
        let getAllColors (TileColors colors) =
            colors
            |> Array2D.toFlatSequence
        let toXORPixelRows colorIndex colorIndexLookup tileColorIndices (TileColors data) =
            data
            |> Array2D.mapi (fun y x color ->
                let pixelColorIndex =
                    color
                    |> Option.map (fun v -> Map.find v colorIndexLookup)
                let previousColorIndex = TileColorIndices.get x y tileColorIndices
                if pixelColorIndex |> Option.map (ColorIndex.xor previousColorIndex) = Some colorIndex then 1uy else 0uy
            )
            |> Array2D.toSequence
            |> Seq.map PixelRow.create
            |> Seq.toArray
        let slice xs (TileColors data) =
            data
            |> Array2D.mapi (fun _y x v -> if xs |> List.contains x then v else None)
            |> TileColors

    type private TilesColors = TilesColors of TileColors[,]
    module private TilesColors =
        let private rowToIndex (Row row) = int row - 1
        let private columnToIndex (Column column) = int column - 1
        let private indexToRow i = Row (byte i + 1uy)
        let private indexToColumn i = Column (byte i + 1uy)
        let columns (TilesColors data) = Array2D.indices2 data |> Seq.map indexToColumn
        let rows (TilesColors data) = Array2D.indices1 data |> Seq.map indexToRow
        let get row column (TilesColors data) = data.[rowToIndex row, columnToIndex column]
        let getAllColors (TilesColors colors) =
            colors
            |> Array2D.toFlatSequence
            |> Seq.collect TileColors.getAllColors
        let generateTiles x y renderedText =
            let (textWidth, textHeight) = (RenderedText.width renderedText, RenderedText.height renderedText)

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

            let (base1, length1) =
                if startRow < 0 || startRow + rows > int Tiles.rows then
                    printfn $"WARNING: Text \"{RenderedText.text renderedText}\" doesn't fit on screen vertically"
                    let startRow = Math.Max(startRow, 0)
                    let rowsLeft = Math.Max(int Tiles.rows - startRow, 0)
                    (startRow, Math.Min(rows, rowsLeft))
                else (startRow, rows)

            let (base2, length2) =
                if startColumn < 0 || startColumn + columns > int Tiles.columns then
                    printfn $"WARNING: Text \"{RenderedText.text renderedText}\" doesn't fit on screen horizontally"
                    let startColumn = Math.Max(startColumn, 0)
                    let columnsLeft = Math.Max(int Tiles.columns - startColumn, 0)
                    (startColumn, Math.Min(columns, columnsLeft))
                else (startColumn, columns)

            Array2D.initBased base1 base2 length1 length2 (fun row column ->
                let xStart = (column - startColumn) * TileBlock.width
                let yStart = (row - startRow) * TileBlock.height
                TileColors.init (fun x y ->
                    let sourceX = xStart + x - xOffsetFromTile
                    let sourceY = yStart + y - yOffsetFromTile
                    RenderedText.tryGet sourceX sourceY renderedText |> Option.flatten
                )
            )
            |> TilesColors

        let slice column xs (TilesColors data) =
            data
            |> Array2D.slice (Array2D.base1 data) (columnToIndex column) (Array2D.length1 data) 1
            |> Array2D.map (TileColors.slice xs)
            |> TilesColors

    let private ticksPerSecond = 10_000_000
    let private sectorsPerSecond = 75

    let private getRenderDuration packetCount =
        TimeSpan((int64 packetCount * int64 ticksPerSecond) / (int64 sectorsPerSecond * int64 Sector.packetCount))

    let private getPacketCount (duration: TimeSpan) =
        (duration.Ticks * int64 sectorsPerSecond * int64 Sector.packetCount) / int64 ticksPerSecond |> int

    let private tryGetFillingPackets timeToFill =
        if timeToFill < TimeSpan.Zero then None
        else
            let count = getPacketCount timeToFill
            List.replicate count EmptyPacket |> Some

    let private renderText text foregroundColor backgroundColor =
        ImageProcessing.renderText text.Content text.Font.Type text.Font.Size foregroundColor backgroundColor
        |> RenderedText.remove backgroundColor

    let private renderTiledText text x y foregroundColor backgroundColor =
        renderText text foregroundColor backgroundColor
        |> TilesColors.generateTiles x y

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
            let totalWidth = renderedLineParts |> List.sumBy (snd >> RenderedText.width)
            let offsetX = Position.getOffsetStart totalWidth Display.contentWidth x
            (([], offsetX), renderedLineParts)
            ||> List.fold (fun (tiles, offsetX) (linePart, renderedText) ->
                let partWidth = RenderedText.width renderedText
                let partTiles = TilesColors.generateTiles (OffsetStart offsetX) (OffsetStart (offsetY + i * lineHeight)) renderedText
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

    let private sliceLinePart (tiles: TilesColors) (duration: TimeSpan) =
        [
            match TilesColors.columns tiles |> Seq.toList with
            | [] -> (tiles, duration)
            | columns ->
                let width = columns.Length * TileBlock.width
                for column in columns do
                let sliceWidth = 3
                let sliceDisplayDuration = duration / float width * float sliceWidth
                for sliceX in [ 0..TileBlock.width - 1] |> List.chunkBySize sliceWidth do
                    let slice =
                        tiles
                        |> TilesColors.slice column sliceX
                    (slice, sliceDisplayDuration)
            ]

    let private sliceLineParts (lineParts: (TilesColors * TimeSpan) list) =
        lineParts
        |> List.collect (fun (linePartTiles, duration) -> sliceLinePart linePartTiles duration)

    let private tileBlocks tiles (colorTable: Color array) colorIndices =
        let indexedColors =
            colorTable
            |> Seq.mapi (fun i v -> (ColorIndex (byte i), v))
            |> Seq.toList
        let colorIndexLookup =
            indexedColors
            |> List.map (fun (i, c) -> (c, i))
            |> Map.ofList

        [
            for column in TilesColors.columns tiles do
            for row in TilesColors.rows tiles do
            for colorIndex in indexedColors |> List.map fst do
                let tileColorIndices =
                    colorIndices
                    |> ImageColorIndices.get row column
                let pixelRows =
                    tiles
                    |> TilesColors.get row column
                    |> TileColors.toXORPixelRows colorIndex colorIndexLookup tileColorIndices
                if pixelRows |> Array.exists (fun (PixelRow v) -> v > 0uy) then
                    let tileBlockData = {
                        Color1 = ColorIndex 0uy
                        Color2 = colorIndex
                        Row = row
                        Column = column
                        PixelRows = pixelRows
                    }
                    TileBlock (XORTileBlock, tileBlockData) |> CDGPacket
        ]

    let private getTitlePagePackets backgroundColor data =
        let titleTiles = renderTiledText data.SongTitle.Text data.SongTitle.X data.SongTitle.Y data.Color backgroundColor
        let artistTiles = renderTiledText data.Artist.Text data.Artist.X data.Artist.Y data.Color backgroundColor

        let colorTable =
            [
                titleTiles
                artistTiles
            ]
            |> Seq.collect TilesColors.getAllColors
            |> getColorTable backgroundColor

        [
            MemoryPreset (ColorIndex 0uy, Repeat 0uy) |> CDGPacket
            LoadColorTableLow colorTable.[..7] |> CDGPacket
            LoadColorTableHigh colorTable.[8..] |> CDGPacket
            yield! tileBlocks titleTiles colorTable ImageColorIndices.empty
            yield! tileBlocks artistTiles colorTable  ImageColorIndices.empty
        ]

    let private getLyricsPagePackets backgroundColor data =
        let tiles = renderTiledLines data.Lines data.Font Center Center (data.Font.Size * 2) data.NotSungYetColor backgroundColor |> List.collect id
        let sungTiles = renderTiledLines data.Lines data.Font Center Center (data.Font.Size * 2) data.SungColor backgroundColor |> List.collect id

        let colorTable =
            (tiles @ sungTiles)
            |> Seq.collect (fst >> TilesColors.getAllColors)
            |> getColorTable backgroundColor

        let initPackets = [
            MemoryPreset (ColorIndex 0uy, Repeat 0uy) |> CDGPacket
            LoadColorTableLow colorTable.[..7] |> CDGPacket
            LoadColorTableHigh colorTable.[8..] |> CDGPacket
        ]

        let drawTextPackets =
            tiles
            |> List.collect (fun (lineTiles, _duration) ->
                tileBlocks lineTiles colorTable ImageColorIndices.empty
            )

        let renderState = Renderer.render (initPackets @ drawTextPackets)

        let singPackets = [
            yield!
                sungTiles
                |> sliceLineParts
                |> List.collect (fun (tiles, displayDuration) ->
                    let instructions = tileBlocks tiles colorTable renderState.ColorIndices
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

        (initPackets, drawTextPackets, singPackets)

    let private replaceEmptyPackets list newPackets =
        let emptyPackets =
            list
            |> List.filter (function | EmptyPacket -> true | _ -> false)
        printfn $"Empty: %d{emptyPackets.Length}/%d{list.Length}, New: %d{List.length newPackets}"
        let rec fn remainingPackets remainingNewPackets acc =
            let remainingCDGPacketCount =
                remainingPackets
                |> List.filter (function CDGPacket _ -> true | _ -> false)
                |> List.length
            match remainingPackets, remainingNewPackets with
            | [], [] -> List.rev acc
            | [], x ->
                printfn $"WARNING: %d{x.Length} new packet(s) dropped at the end"
                fn [] [] acc
            | (CDGPacket _ as packet :: remainingPackets', _)
            | (OtherPacket _ as packet :: remainingPackets', _) ->
                let acc' = packet :: acc
                fn remainingPackets' remainingNewPackets acc'
            | EmptyPacket :: _, remainingNewPackets when List.length remainingNewPackets > remainingCDGPacketCount ->
                let dropCount = List.length remainingNewPackets - remainingCDGPacketCount
                printfn $"WARNING: %d{dropCount} new packet(s) dropped because insert would be too late"
                let remainingNewPackets' = List.skip dropCount remainingNewPackets
                fn remainingPackets remainingNewPackets' acc
            | EmptyPacket :: remainingPackets', newPacket :: remainingNewPackets' ->
                let acc' = newPacket :: acc
                fn remainingPackets' remainingNewPackets' acc'
            | (EmptyPacket as packet) :: remainingPackets', [] ->
                let acc' = packet :: acc
                fn remainingPackets' [] acc'

        fn list newPackets []

    let private processCommand (totalPacketCount, previousPackets) command =
        let currentRenderDuration = getRenderDuration totalPacketCount
        let newPackets =
            match command.CommandType with
            | ShowTitlePage data ->
                getTitlePagePackets command.BackgroundColor data
            | ShowLyricsPage data ->
                let (initPackets, drawTextPackets, singPackets) = getLyricsPagePackets command.BackgroundColor data
                let drawTextStartTime =
                    let drawTextDuration = getRenderDuration (initPackets.Length + drawTextPackets.Length)
                    let earliestStartTime = command.StartTime - drawTextDuration - TimeSpan.FromSeconds(2.)
                    if earliestStartTime > currentRenderDuration then earliestStartTime
                    else currentRenderDuration
                let (drawTextPacketsBeforeSingStart, drawTextPacketsInBetween) =
                    let splitIndex = getPacketCount (command.StartTime - drawTextStartTime)
                    if splitIndex < List.length drawTextPackets then
                        List.splitAt splitIndex drawTextPackets
                    else (drawTextPackets, [])
                let fillingPackets1 =
                    tryGetFillingPackets (drawTextStartTime - currentRenderDuration)
                    |> Option.defaultValue []
                let fillingPackets2 =
                    tryGetFillingPackets (command.StartTime - (currentRenderDuration + getRenderDuration (fillingPackets1.Length + initPackets.Length + drawTextPacketsBeforeSingStart.Length)))
                    |> Option.defaultValue []
                [
                    yield! initPackets
                    yield! fillingPackets1
                    yield! drawTextPacketsBeforeSingStart
                    yield! fillingPackets2
                    yield! replaceEmptyPackets singPackets drawTextPacketsInBetween
                ]

        (totalPacketCount + newPackets.Length, newPackets)

    let generate commands =
        ((0, []), commands)
        ||> List.scan processCommand
        |> List.collect snd
        |> List.toArray
