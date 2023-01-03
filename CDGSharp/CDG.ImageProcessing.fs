module CDG.ImageProcessing

open SixLabors.Fonts
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Drawing
open SixLabors.ImageSharp.Drawing.Processing
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing
open System

module internal Color =
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

type internal RenderedText<'a> = RenderedText of string * 'a[,]
module internal RenderedText =
    let empty =
        let data = Array2D.zeroCreate 0 0
        RenderedText ("", data)
    let init text (width, height) fn =
        let data = Array2D.init height width (fun y x -> fn x y)
        RenderedText (text, data)
    let map fn (RenderedText (text, data)) =
        let data' = Array2D.map fn data
        RenderedText (text, data')
    let remove value =
        map (fun v -> if v = value then None else Some v)
    let text (RenderedText (text, _)) = text
    let width (RenderedText (_, data)) = Array2D.length2 data
    let height (RenderedText (_, data)) = Array2D.length1 data
    let tryGet x y renderedText =
        if x < 0 || y < 0 || x >= width renderedText || y >= height renderedText then None
        else
            let (RenderedText (_, data)) = renderedText
            Some data.[y, x]

type FontType =
    | SystemFont of string
    | CustomFont of string
module internal FontType =
    let createFont size = function
        | SystemFont name ->
            SystemFonts.CreateFont(name, float32 size)
        | CustomFont path ->
            let fontCollection = FontCollection()
            let fontFamily = fontCollection.Install(path)
            fontFamily.CreateFont(float32 size)

module internal ImageProcessing =
    let renderText (text: string) fontType fontSize foregroundColor backgroundColor =
        let foregroundColor = Color.fromCDGColor foregroundColor
        let backgroundColor = Color.fromCDGColor backgroundColor
        let font = FontType.createFont fontSize fontType
        let glyphs = TextBuilder.GenerateGlyphs(text, RendererOptions(font))
        let bounds =
            if glyphs |> Seq.isEmpty then
                let bounds = TextMeasurer.Measure(text, RendererOptions(font))
                RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height)
            else
                glyphs.Bounds
        let width = Math.Ceiling(float bounds.Width + float bounds.Left) |> int
        let height = Math.Ceiling(float bounds.Height + float bounds.Top) |> int
        if width = 0 || height = 0 then RenderedText.empty
        else
            let image = new Image<Rgba32>(width, height, backgroundColor)
            image.Mutate(fun ctx ->
                let options = DrawingOptions(GraphicsOptions = GraphicsOptions(Antialias = false))
                ctx.Fill(options, foregroundColor, glyphs) |> ignore
            )
            RenderedText.init text (image.Width, image.Height) (fun x y ->
                let color = image.[x, y]
                Color.toCDGColor color
            )
