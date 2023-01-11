module CDG.Serializer

open CDG.BinaryFormat

module ColorIndex =
    let serialize (ColorIndex v) = v

module Repeat =
    let serialize (Repeat v) = v

module Row =
    let serialize (Row v) = v

module Column =
    let serialize (Column v) = v

module PixelRow =
    let serialize (PixelRow v) = v

module TileBlockData =
    let serialize v =
        [|
            ColorIndex.serialize v.Color1
            ColorIndex.serialize v.Color2
            Row.serialize v.Row
            Column.serialize v.Column
            yield! v.PixelRows |> Array.map PixelRow.serialize
        |]

module HScrollCommand =
    let serialize = function
        | NoHScroll -> 0uy
        | ScrollRight -> 1uy <<< 4
        | ScrollLeft -> 2uy <<< 4

module HScrollOffset =
    let serialize (HScrollOffset v) = v

module HScroll =
    let serialize (HScroll (command, offset)) =
        HScrollCommand.serialize command ||| HScrollOffset.serialize offset

module VScrollCommand =
    let serialize = function
        | NoVScroll -> 0uy
        | ScrollDown -> 1uy <<< 4
        | ScrollUp -> 2uy <<< 4

module VScrollOffset =
    let serialize (VScrollOffset v) = v

module VScroll =
    let serialize (VScroll (command, offset)) =
        VScrollCommand.serialize command ||| VScrollOffset.serialize offset

module ColorChannel =
    let serialize (ColorChannel v) = v

module Color =
    let serialize v =
        let highByte = (ColorChannel.serialize v.Red <<< 2) ||| (ColorChannel.serialize v.Green >>> 2)
        let lowByte = (ColorChannel.serialize v.Green &&& 0b0000_0011uy <<< 4) ||| ColorChannel.serialize v.Blue
        [| highByte; lowByte |]

module CDGPacketInstruction =
    let serialize = function
        | MemoryPreset (color, repeat) ->
            (1uy, [| ColorIndex.serialize color; Repeat.serialize repeat; yield! Array.zeroCreate 14 |])
        | BorderPreset color ->
            (2uy, [| ColorIndex.serialize color; yield! Array.zeroCreate 15 |])
        | TileBlock (ReplaceTileBlock, v) ->
            (6uy, TileBlockData.serialize v)
        | TileBlock (XORTileBlock, v) ->
            (38uy, TileBlockData.serialize v)
        | ScrollPreset (color, hScroll, vScroll) ->
            (20uy, [| ColorIndex.serialize color; HScroll.serialize hScroll; VScroll.serialize vScroll; yield! Array.zeroCreate 13 |])
        | ScrollCopy (hScroll, vScroll) ->
            (24uy, [| 0uy; HScroll.serialize hScroll; VScroll.serialize vScroll; yield! Array.zeroCreate 13 |])
        | DefineTransparentColor color ->
            (28uy, [| ColorIndex.serialize color; yield! Array.zeroCreate 15 |])
        | LoadColorTableLow colorSpecs ->
            (30uy, colorSpecs |> Array.collect Color.serialize)
        | LoadColorTableHigh colorSpecs ->
            (31uy, colorSpecs |> Array.collect Color.serialize)

module SubCodePacket =
    let serialize = function
        | CDGPacket v ->
            let (instructionCode, data) = CDGPacketInstruction.serialize v
            if Array.length data <> CDGPacketInstruction.dataLength then
                failwith $"Packet data size is expected to be {CDGPacketInstruction.dataLength}"
            [|
                9uy
                instructionCode
                0uy; 0uy
                yield! data
                0uy; 0uy; 0uy; 0uy
            |]
        | EmptyPacket -> Array.zeroCreate SubCodePacket.dataLength
        | OtherPacket data -> data

module Serializer =
    let serializePackets =
        Array.map SubCodePacket.serialize >> Array.collect id
