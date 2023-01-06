module CDG.ImageProcessing

module internal Color =
    open SixLabors.ImageSharp.PixelFormats

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

type FontStyle = Regular | Bold

// ImageSharp renders text not as good as System.Drawing does (see https://github.com/SixLabors/ImageSharp/issues/138)
// But of course it would be nice to only use a single image library,
// so check back later to see if this is fixed
module internal ImageSharpImageProcessing =
    open System
    open SixLabors.Fonts
    open SixLabors.ImageSharp
    open SixLabors.ImageSharp.Drawing
    open SixLabors.ImageSharp.Drawing.Processing
    open SixLabors.ImageSharp.PixelFormats
    open SixLabors.ImageSharp.Processing

    module internal Font =
        let create fontType size fontStyle =
            let style =
                match fontStyle with
                | Regular -> FontStyle.Regular
                | Bold -> FontStyle.Bold
            match fontType with
            | SystemFont name ->
                SystemFonts.CreateFont(name, float32 size, style)
            | CustomFont path ->
                let fontCollection = FontCollection()
                let fontFamily = fontCollection.Install(path)
                fontFamily.CreateFont(float32 size, style)

    let renderText (text: string) fontType fontSize fontStyle foregroundColor backgroundColor =
        let foregroundColor = Color.fromCDGColor foregroundColor
        let backgroundColor = Color.fromCDGColor backgroundColor
        let font = Font.create fontType fontSize fontStyle
        let textRenderOptions = RendererOptions(font)
        let glyphs = TextBuilder.GenerateGlyphs(text, textRenderOptions)
        let bounds =
            if glyphs |> Seq.isEmpty then
                let bounds = TextMeasurer.Measure(text, textRenderOptions)
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

module internal ImageProcessing =
    open System
    open System.Drawing
    open System.Drawing.Text

    module Color =
        let to8BitColorPart (ColorChannel color) = int color * 255 / 15
        let fromCDGColor color =
            Color.FromArgb(to8BitColorPart color.Red, to8BitColorPart color.Green, to8BitColorPart color.Blue)

        let to4BitColorPart color = int color * 15 / 255 |> byte |> ColorChannel
        let toCDGColor (color: Color) =
            {
                Red = to4BitColorPart color.R
                Green = to4BitColorPart color.G
                Blue = to4BitColorPart color.B
            }

    module internal Font =
        let create fontType size fontStyle =
            let style =
                match fontStyle with
                | Regular -> FontStyle.Regular
                | Bold -> FontStyle.Bold
            match fontType with
            | SystemFont name ->
                new Font(name, float32 size, style)
            | CustomFont path ->
                use fontCollection = new PrivateFontCollection()
                fontCollection.AddFontFile(path)
                new Font(fontCollection.Families.[0], float32 size, style)

    let renderText (text: string) fontType fontSize fontStyle foregroundColor backgroundColor =
        let foregroundColor = Color.fromCDGColor foregroundColor
        let backgroundColor = Color.fromCDGColor backgroundColor
        use font = Font.create fontType fontSize fontStyle
        use dummyBmp = new Bitmap(1, 1)
        dummyBmp.SetResolution(72f, 72f)
        use graphics = Graphics.FromImage(dummyBmp)
        graphics.InterpolationMode <- Drawing2D.InterpolationMode.HighQualityBicubic
        graphics.TextRenderingHint <- TextRenderingHint.ClearTypeGridFit
        let size =
            if text = " " then
                graphics.MeasureString(text, font, PointF.Empty, StringFormat.GenericDefault)
            else
                graphics.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic)
        if size.Width = 0f || size.Height = 0f then RenderedText.empty
        else
            use image = new Bitmap(int (Math.Ceiling(float size.Width)), int (Math.Ceiling(float size.Height)), graphics)
            use graphics = Graphics.FromImage(image)
            graphics.Clear(backgroundColor)
            use foregroundBrush = new SolidBrush(foregroundColor)
            graphics.DrawString(text, font, foregroundBrush, PointF.Empty, StringFormat.GenericTypographic)
            RenderedText.init text (image.Width, image.Height) (fun x y ->
                let color = image.GetPixel(x, y)
                Color.toCDGColor color
            )
