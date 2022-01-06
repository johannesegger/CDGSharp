module CDG.ImageRenderer

open CDG.Renderer
open SixLabors.Fonts
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Drawing.Processing
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open System.IO

module private Display =
    let tileBlockSize = Size(TileBlock.width, TileBlock.height)
    let borderSize = tileBlockSize
    let imageSize = Size(tileBlockSize.Width * int Tiles.columns + 2 * borderSize.Width, tileBlockSize.Height * int Tiles.rows + 2 * borderSize.Height)
    let fullImageSize = imageSize * 2
    let cdgImageRectangle =
        let location = Point(
            (fullImageSize.Width - imageSize.Width) / 2,
            fullImageSize.Height - imageSize.Height
        )
        Rectangle(location, imageSize)
    let contentSize = imageSize - 2 * tileBlockSize
    let contentRectangle =
        let location = Point(
            cdgImageRectangle.Left + borderSize.Width,
            cdgImageRectangle.Top + borderSize.Height
        )
        Rectangle(location, contentSize)

module private Color =
    let to8BitColorPart (ColorChannel color) = color * 16uy
    let fromCDGColor color =
        Rgba32(to8BitColorPart color.Red, to8BitColorPart color.Green, to8BitColorPart color.Blue)

type ImageRenderState = {
    Image: Image<Rgba32>
    RenderState: RenderState
}
module ImageRenderState =
    let refreshTileColors (image: Image<Rgba32>) tileRow tileColumn renderState =
        let (xOffset, yOffset) = TileBlock.getBlockCoordinates tileRow tileColumn
        let fullXOffset = Display.contentRectangle.Left + xOffset
        let fullYOffset = Display.contentRectangle.Top + yOffset
        let tileColorIndices = ImageColorIndices.get tileRow tileColumn renderState.ColorIndices
        TileBlock.coords
        |> List.iter (fun (x, y) ->
            let colorIndex = TileColorIndices.get x y tileColorIndices
            image.[fullXOffset + x, fullYOffset + y] <- ColorTable.getColor colorIndex renderState.ColorTable |> Color.fromCDGColor
        )

    let refreshContent image renderState =
        Tiles.coords
        |> List.iter (fun (row, column) ->
            refreshTileColors image row column renderState
        )

    let refreshBorder (image: Image<Rgba32>) renderState =
        let color = RenderState.getBorderColor renderState |> Color.fromCDGColor
        image.Mutate(fun ctx ->
            ctx.Fill(color, Rectangle(Display.cdgImageRectangle.Left, Display.cdgImageRectangle.Top, Display.borderSize.Width, Display.cdgImageRectangle.Height))
               .Fill(color, Rectangle(Display.contentRectangle.Left, Display.cdgImageRectangle.Top, Display.contentRectangle.Width, Display.borderSize.Height))
               .Fill(color, Rectangle(Display.contentRectangle.Right, Display.cdgImageRectangle.Top, Display.borderSize.Width, Display.cdgImageRectangle.Height))
               .Fill(color, Rectangle(Display.contentRectangle.Left, Display.contentRectangle.Bottom, Display.contentRectangle.Width, Display.borderSize.Height)) |> ignore
        )

    let empty = {
        Image =
            let image = new Image<Rgba32>(Display.fullImageSize.Width, Display.fullImageSize.Height, Color.White)
            refreshContent image RenderState.empty
            refreshBorder image RenderState.empty
            image
        RenderState = RenderState.empty
    }

let private writeExplanation (ctx: IImageProcessingContext) text =
    ctx.DrawText(text, SystemFonts.CreateFont("Arial", 10f), Color.Black, Point(10, 10)) |> ignore

let private clearExplanation (ctx: IImageProcessingContext) =
    ctx.Fill(Color.White, Rectangle(0, 0, Display.fullImageSize.Width, Display.cdgImageRectangle.Top)) |> ignore

let private applyCDGPacket state packetInstruction =
    let state = { state with RenderState = Renderer.applyCDGPacket state.RenderState packetInstruction }

    let image = state.Image.Clone(fun ctx -> clearExplanation ctx)
    image.Mutate(fun ctx ->
        Explainer.CDGPacketInstruction.explain packetInstruction |> writeExplanation ctx
    )

    match packetInstruction with
    | MemoryPreset (colorIndex, repeat) ->
        ImageRenderState.refreshContent image state.RenderState
        { state with Image = image }
    | BorderPreset colorIndex ->
        ImageRenderState.refreshBorder image state.RenderState
        { state with Image = image }
    | TileBlock (ReplaceTileBlock, data)
    | TileBlock (XORTileBlock, data) ->
        ImageRenderState.refreshTileColors image data.Row data.Column state.RenderState
        { state with Image = image }
    | ScrollPreset (colorIndex, hScroll, vScroll) -> failwith "ScrollPreset: Not implemented"
    | ScrollCopy (hScroll, vScroll) -> failwith "ScrollCopy: Not implemented"
    | DefineTransparentColor colorIndex ->
        { state with Image = image }
    | LoadColorTableLow colorSpecs
    | LoadColorTableHigh colorSpecs ->
        ImageRenderState.refreshContent image state.RenderState
        { state with Image = image }

let applyPacket state = function
    | CDGPacket instruction -> applyCDGPacket state instruction
    | Other data ->
        let image = state.Image.Clone(fun ctx ->
            ctx.Fill(Color.White, Rectangle(0, 0, Display.fullImageSize.Width, Display.cdgImageRectangle.Top)) |> ignore
            writeExplanation ctx "No CD+G information"
        )
        { state with Image = image }

let renderImages packets =
    (ImageRenderState.empty, packets)
    ||> Array.scan applyPacket
    |> Array.skip 1
    |> Array.map (fun v -> v.Image)

let renderImagesFromCDGFile (path: string) =
    let targetDir = Path.GetFileNameWithoutExtension(path)
    try
        Directory.Delete(targetDir, recursive = true)
    with :? DirectoryNotFoundException -> ()
    Directory.CreateDirectory(targetDir) |> ignore
    File.ReadAllBytes(path)
    |> Parser.parse
    |> Array.truncate (4 * 75 * 20)
    |> renderImages
    |> Seq.iteri (fun i image -> image.SaveAsBmp(Path.Combine(targetDir, $"{i + 1}.bmp")))