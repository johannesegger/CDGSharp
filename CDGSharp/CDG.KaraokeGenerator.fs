module CDG.KaraokeGenerator

open System
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
            
            Array2D.initBased startRow startColumn rows columns (fun row column ->
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
        let titleTiles = renderTiledText data.SongTitle Center (OffsetStart (5 * TileBlock.height)) data.Color backgroundColor
        let artistTiles = renderTiledText data.Artist Center (OffsetStart (12 * TileBlock.height)) data.Color backgroundColor

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
            yield!
                tiles
                |> List.collect (fun (lineTiles, _duration) ->
                    tileBlocks lineTiles colorTable ImageColorIndices.empty
                )
        ]

        let renderState = Renderer.render initPackets

        let animationPackets = [
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

        (initPackets, animationPackets)

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
