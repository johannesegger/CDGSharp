module internal CDG.ImageProcessing

open SixLabors.ImageSharp.PixelFormats

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

