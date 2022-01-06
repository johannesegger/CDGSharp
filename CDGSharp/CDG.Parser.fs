module CDG.Parser

open System.Diagnostics

let private ignorePQChannel v = v &&& 0b0011_1111uy

module ColorIndex =
    let parse v =
        ColorIndex (v &&& 0b0000_1111uy)

module Repeat =
    let parse v =
        Repeat (v &&& 0b0000_1111uy)

module Row =
    let parse v =
        Row (v &&& 0b0001_1111uy)

module Column =
    let parse v =
        Column (ignorePQChannel v)

module PixelRow =
    let parse v =
        PixelRow (ignorePQChannel v)

module TileBlockData =
    let parse (data: byte array) =
        {
            Color1 = ColorIndex.parse data.[0]
            Color2 = ColorIndex.parse data.[1]
            Row = Row.parse data.[2]
            Column = Column.parse data.[3]
            PixelRows = data.[4..] |> Array.map PixelRow.parse
        }

module HScrollCommand =
    let parse v =
        match ignorePQChannel v >>> 4 with
        | 0uy -> NoHScroll
        | 1uy -> ScrollRight
        | 2uy -> ScrollLeft
        | x -> failwith $"Unknown h-scroll command \"{x}\""

module HScrollOffset =
    let parse v =
        let offset = v &&& 0b0000_0111uy
        if offset > 5uy then failwith $"H-scroll offset must be between 0 and 5, but was {offset}"
        HScrollOffset offset

module HScroll =
    let parse v =
        HScroll (HScrollCommand.parse v, HScrollOffset.parse v)

module VScrollCommand =
    let parse v =
        match ignorePQChannel v >>> 4 with
        | 0uy -> NoVScroll
        | 1uy -> ScrollDown
        | 2uy -> ScrollUp
        | x -> failwith $"Unknown v-scroll command \"{x}\""

module VScrollOffset =
    let parse v =
        let offset = v &&& 0b0000_1111uy
        if offset > 11uy then failwith $"V-scroll offset must be between 0 and 11, but was {offset}"
        VScrollOffset offset

module VScroll =
    let parse v =
        VScroll (VScrollCommand.parse v, VScrollOffset.parse v)

module ColorChannel =
    let parse v =
        ColorChannel (v &&& 0b0000_1111uy)

module Color =
    let parse highByte lowByte =
        let red = ignorePQChannel highByte >>> 2 |> ColorChannel
        let green = ((highByte &&& 0b0000_0011uy) <<< 2) ||| (ignorePQChannel lowByte >>> 4) |> ColorChannel
        let blue = lowByte &&& 0b0000_1111uy |> ColorChannel
        { Red = red; Green = green; Blue = blue }

module CDGPacketInstruction =
    let parse instruction (data: byte array) =
        Debug.Assert(Array.length data = CDGPacketInstruction.length, $"Packet data size is expected to be {CDGPacketInstruction.length}")

        match ignorePQChannel instruction with
        | 1uy -> MemoryPreset (ColorIndex.parse data.[0], Repeat.parse data.[1])
        | 2uy -> BorderPreset (ColorIndex.parse data.[0])
        | 6uy -> TileBlock (ReplaceTileBlock, TileBlockData.parse data)
        | 38uy -> TileBlock (XORTileBlock, TileBlockData.parse data)
        | 20uy -> ScrollPreset (ColorIndex.parse data.[0], HScroll.parse data.[1], VScroll.parse data.[2])
        | 24uy -> ScrollCopy (HScroll.parse data.[1], VScroll.parse data.[2])
        | 28uy -> DefineTransparentColor (ColorIndex.parse data.[0])
        | 30uy -> LoadColorTableLow (data |> Array.chunkBySize 2 |> Array.map (fun v -> Color.parse v.[0] v.[1]))
        | 31uy -> LoadColorTableHigh (data |> Array.chunkBySize 2 |> Array.map (fun v -> Color.parse v.[0] v.[1]))
        | x -> failwith $"Unknown CDG packet instruction \"{x}\""

module SubCodePacket =
    let parse content =
        Debug.Assert(Array.length content = SubCodePacket.length, $"Sub code packet size is expected to be {SubCodePacket.length}")

        if ignorePQChannel content.[0] = 9uy then
            CDGPacket (CDGPacketInstruction.parse content.[1] content.[4..19])
        else
            Other content

let parse content =
    content
    |> Array.chunkBySize (Sector.packetCount * SubCodePacket.length)
    |> Array.map (Array.chunkBySize SubCodePacket.length >> Array.map SubCodePacket.parse)
    |> Array.collect id
