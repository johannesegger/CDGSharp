module internal CDG.ImageProcessing

open SixLabors.Fonts
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Drawing
open SixLabors.ImageSharp.Drawing.Processing
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open System

module Color =
    let to8BitColorPart (ColorChannel color) = int color * 255 / 15 |> byte
    let fromCDGColor color =
        Rgba32(to8BitColorPart color.Red, to8BitColorPart color.Green, to8BitColorPart color.Blue)

    let to4BitColorPart color = int color * 15 / 255 |> byte |> ColorChannel
    let toCDGColor (color: Rgba32) =
        {
            Red = to4BitColorPart color.R
            Green = to4BitColorPart color.G
            Blue = to4BitColorPart color.B
        }

let renderText (text: string) fontName fontSize foregroundColor backgroundColor =
    let foregroundColor = Color.fromCDGColor foregroundColor
    let backgroundColor = Color.fromCDGColor backgroundColor
    let font = SystemFonts.CreateFont(fontName, float32 fontSize)
    let glyphs = TextBuilder.GenerateGlyphs(text, RendererOptions(font))
    let bounds =
        if glyphs |> Seq.isEmpty then
            let bounds = TextMeasurer.Measure(text, RendererOptions(font))
            RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height)
        else
            glyphs.Bounds
    let width = Math.Ceiling(float bounds.Width + float bounds.Left) |> int
    let height = Math.Ceiling(float bounds.Height + float bounds.Top) |> int
    let image = new Image<Rgba32>(width, height, backgroundColor)
    image.Mutate(fun ctx ->
        let options = DrawingOptions(GraphicsOptions = GraphicsOptions(Antialias = false))
        ctx.Fill(options, foregroundColor, glyphs) |> ignore
    )
    Array2D.init image.Height image.Width (fun y x ->
        let color = image.[x, y]
        Color.toCDGColor color
    )
