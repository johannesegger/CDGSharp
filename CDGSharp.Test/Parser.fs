module CDG.Test.Parser

open Expecto
open CDG
open CDG.Parser

module Gen =
    open FsCheck
    type FourBitNumber = FourBitNumber of byte
    type FiveBitNumber = FiveBitNumber of byte
    type SixBitNumber = SixBitNumber of byte
    let nBitNumberArb n typeFn =
        Gen.choose (0, (1 <<< n) - 1)
        |> Gen.map (byte >> typeFn)
        |> Arb.fromGen
    type TwelveElementArray<'a> = TwelveElementArray of 'a[]
    let nElementArray n typeFn =
        Gen.arrayOfLength n Arb.generate
        |> Gen.map typeFn
        |> Arb.fromGen
    let twelveElementArrayArb<'a> : Arbitrary<TwelveElementArray<'a>> = nElementArray 12 TwelveElementArray
    let fourBitNumberArb = nBitNumberArb 4 FourBitNumber
    let fiveBitNumberArb = nBitNumberArb 5 FiveBitNumber
    let sixBitNumberArb = nBitNumberArb 6 SixBitNumber
    let addToConfig config =
        { config with arbitrary = typeof<FourBitNumber>.DeclaringType::config.arbitrary }

let private fsCheckConfig = Gen.addToConfig FsCheckConfig.defaultConfig

let private filler length = Array.zeroCreate<byte> length

let tests = testList "Parser" [
    testProperty "Can parse color index" <| fun colorIndex ->
        let (ColorIndex actualColorIndex) = ColorIndex.parse colorIndex
        Expect.isLessThan actualColorIndex (1uy <<< 4) "Should only use lower 4 bits"

    testProperty "Can parse repeat counter" <| fun repeat ->
        let (Repeat actualRepeat) = Repeat.parse repeat
        Expect.isLessThan actualRepeat (1uy <<< 4) "Should only use lower 4 bits"

    testProperty "Can parse row" <| fun row ->
        let (Row actualRow) = Row.parse row
        Expect.isLessThan actualRow (1uy <<< 5) "Should only use lower 5 bits"

    testProperty "Can parse column" <| fun column ->
        let (Column actualColumn) = Column.parse column
        Expect.isLessThan actualColumn (1uy <<< 6) "Should only use lower 6 bits"

    testProperty "Can parse pixel row" <| fun pixelRow ->
        let (PixelRow actualPixelRow) = PixelRow.parse pixelRow
        Expect.isLessThan actualPixelRow (1uy <<< 6) "Should only use lower 6 bits"

    testPropertyWithConfig fsCheckConfig "Can parse tile block data" <| fun (Gen.FourBitNumber colorIndex1) (Gen.FourBitNumber colorIndex2) (Gen.FiveBitNumber row) (Gen.SixBitNumber column) (Gen.TwelveElementArray pixelRows) ->
        let actualTileBlockData = TileBlockData.parse [|
            colorIndex1
            colorIndex2
            row
            column
            yield! pixelRows |> Array.map (fun (Gen.SixBitNumber v) -> v)
        |]
        let expectedTileBlockData = {
            Color1 = ColorIndex colorIndex1
            Color2 = ColorIndex colorIndex2
            Row = Row row
            Column = Column column
            PixelRows = pixelRows |> Array.map (fun (Gen.SixBitNumber v) -> PixelRow v)
        }
        Expect.equal actualTileBlockData expectedTileBlockData "Should correctly parse tile block data"

    testPropertyWithConfig fsCheckConfig "Can parse MemoryPreset package" <| fun (Gen.FourBitNumber colorIndex) (Gen.FourBitNumber repeat) ->
        let packages = parse [|
            9uy
            1uy
            0uy; 0uy
            colorIndex; repeat; yield! filler 14
            0uy; 0uy; 0uy; 0uy
        |]
        Expect.equal packages [| CDGPacket (MemoryPreset (ColorIndex colorIndex, Repeat repeat))  |]

    testPropertyWithConfig fsCheckConfig "Can parse BorderPreset package" <| fun (Gen.FourBitNumber colorIndex) ->
        let packages = parse [|
            9uy
            2uy
            0uy; 0uy
            colorIndex; yield! filler 15
            0uy; 0uy; 0uy; 0uy
        |]
        Expect.equal packages [| CDGPacket (BorderPreset (ColorIndex colorIndex))  |]
]
