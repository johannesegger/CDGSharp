namespace CDG

type ColorIndex = ColorIndex of byte
module ColorIndex =
    let xor (ColorIndex a) (ColorIndex b) =
        ColorIndex (a ^^^ b)
type Repeat = Repeat of byte
type Row = Row of byte
type Column = Column of byte
type PixelRow = PixelRow of byte
type TileBlockOperation =
    | ReplaceTileBlock
    | XORTileBlock
type TileBlockData = {
    Color1: ColorIndex
    Color2: ColorIndex
    Row: Row
    Column: Column
    PixelRows: PixelRow array
}
module TileBlock =
    let width = 6
    let height = 12
    let getColors data =
        data.PixelRows
        |> Seq.map (fun (PixelRow v) ->
            [width - 1..-1..0]
            |> List.map (fun i ->
                if (v >>> i) &&& 1uy = 0uy then data.Color1 else data.Color2
            )
        )
        |> array2D
    let getBlockCoordinates (Row row) (Column column) =
        ((int column - 1) * width, (int row - 1) * height)
    let coords =
        [
            for y in [0..height - 1] do
            for x in [0..width - 1] -> (x, y)
        ]

module Tiles =
    let rows = 16uy
    let columns = 48uy
    let coords =
        [
            for row in [1uy..rows] do
            for column in [1uy..columns] -> (Row row, Column column)
        ]
type HScrollCommand =
    | NoHScroll
    | ScrollRight
    | ScrollLeft
type HScrollOffset = HScrollOffset of byte
type HScroll = HScroll of HScrollCommand * HScrollOffset
type VScrollCommand =
    | NoVScroll
    | ScrollDown
    | ScrollUp
type VScrollOffset = VScrollOffset of byte
type VScroll = VScroll of VScrollCommand * VScrollOffset
type ColorChannel = ColorChannel of byte
type Color = {
    Red: ColorChannel
    Green: ColorChannel
    Blue: ColorChannel
}
module Color =
    let black = { Red = ColorChannel 0uy; Green = ColorChannel 0uy; Blue = ColorChannel 0uy }
type CDGPacketInstruction =
    | MemoryPreset of ColorIndex * Repeat
    | BorderPreset of ColorIndex
    | TileBlock of TileBlockOperation * TileBlockData
    | ScrollPreset of (ColorIndex * HScroll * VScroll)
    | ScrollCopy of (HScroll * VScroll)
    | DefineTransparentColor of ColorIndex
    | LoadColorTableLow of Color array
    | LoadColorTableHigh of Color array
module CDGPacketInstruction =
    let length = 16
type CDGPacket = {
    Instruction: CDGPacketInstruction
}
type SubCodePacket =
    | CDGPacket of CDGPacket
    | Other of byte array
module SubCodePacket =
    let length = 24
    let empty = Other (Array.zeroCreate length)

module Sector =
    let packetCount = 4

