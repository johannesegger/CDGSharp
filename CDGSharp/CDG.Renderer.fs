module CDG.Renderer

type ColorTable = {
    Low: Color array
    High: Color array
}
module ColorTable =
    let empty = {
        Low = Array.replicate 8 Color.black
        High = Array.replicate 8 Color.black
    }
    let getColor (ColorIndex idx) table =
        if int idx < table.Low.Length then table.Low.[int idx]
        else table.High.[int (idx - 8uy)]

type TileColorIndices = TileColorIndices of ColorIndex[,]
module TileColorIndices =
    let create colorIndex =
        Array2D.create TileBlock.height TileBlock.width colorIndex
        |> TileColorIndices
    let fromTileBlock data =
        TileBlock.getColors data
        |> TileColorIndices
    let get x y (TileColorIndices v) =
        v.[y, x]
    let xor (TileColorIndices a) (TileColorIndices b) =
        Array2D.init TileBlock.height TileBlock.width (fun row column ->
            ColorIndex.xor a.[row, column] b.[row, column]
        )
        |> TileColorIndices

type ImageColorIndices = ImageColorIndices of TileColorIndices[,]
module ImageColorIndices =
    let create colorIndex =
        TileColorIndices.create colorIndex
        |> Array2D.create (int Tiles.rows) (int Tiles.columns)
        |> ImageColorIndices

    let empty = create (ColorIndex 0uy)

    let get (Row blockRow) (Column blockColumn) (ImageColorIndices colorIndices) =
        colorIndices.[int blockRow - 1, int blockColumn - 1]

    let set (Row row) (Column column) colors (ImageColorIndices colorIndices) =
        let clone = colorIndices.Clone() :?> TileColorIndices[,]
        clone.[int (row - 1uy), int (column - 1uy)] <- colors
        ImageColorIndices clone

type RenderState = {
    ColorTable: ColorTable
    ColorIndices: ImageColorIndices
    BorderColorIndex: ColorIndex
}

module RenderState =
    let empty = {
        ColorTable = ColorTable.empty
        ColorIndices = ImageColorIndices.create (ColorIndex 0uy)
        BorderColorIndex = ColorIndex 0uy
    }
    let getBorderColor v =
        ColorTable.getColor v.BorderColorIndex v.ColorTable

module Renderer =
    let applyCDGPacket state packetInstruction =
        match packetInstruction with
        | MemoryPreset (colorIndex, repeat) ->
            { state with ColorIndices = ImageColorIndices.create colorIndex; BorderColorIndex = colorIndex }
        | BorderPreset colorIndex ->
            { state with BorderColorIndex = colorIndex }
        | TileBlock (ReplaceTileBlock, data) ->
            let tileColorIndices = TileColorIndices.fromTileBlock data
            let imageColorIndices = ImageColorIndices.set data.Row data.Column tileColorIndices state.ColorIndices
            { state with ColorIndices = imageColorIndices }
        | TileBlock (XORTileBlock, data) ->
            let newColorIndices = TileColorIndices.fromTileBlock data
            let currentColorIndices = ImageColorIndices.get data.Row data.Column state.ColorIndices
            let tileColorIndices = TileColorIndices.xor currentColorIndices newColorIndices
            let imageColorIndices = ImageColorIndices.set data.Row data.Column tileColorIndices state.ColorIndices
            { state with ColorIndices = imageColorIndices }
        | ScrollPreset (colorIndex, hScroll, vScroll) -> failwith "ScrollPreset: Not implemented"
        | ScrollCopy (hScroll, vScroll) -> failwith "ScrollCopy: Not implemented"
        | DefineTransparentColor colorIndex -> state
        | LoadColorTableLow colors ->
            { state with ColorTable = { state.ColorTable with Low = colors } }
        | LoadColorTableHigh colors ->
            { state with ColorTable = { state.ColorTable with High = colors } }

    let applyPacket state = function
        | CDGPacket instruction -> applyCDGPacket state instruction
        | EmptyPacket
        | OtherPacket _ -> state

    let render packets =
        (RenderState.empty, packets)
        ||> List.fold applyPacket
