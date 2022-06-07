module Rendering

open Svg
open Model
open ScryfallApi.Client.Models
open System.Drawing
open System.Drawing.Imaging

let maxWidth = 225
let maxHeight = 225

let toScaledBitmap (svg: SvgDocument) = 
    let dimensions = svg.GetDimensions()

    // If height or width is 0, it preserves aspect ratio
    let rasterWidth, rasterHeight =
        if dimensions.Width > dimensions.Height
        then maxWidth, 0 
        else 0, maxHeight

    svg.Draw(rasterWidth, rasterHeight)

let getColor (c: Card) : WatermarkColor =
    if c.TypeLine.Contains("Land")
    then LandColorless 
    else
        match c.Colors with
        | [| |] -> Colorless
        | [| color |] -> 
            match color with
            | "W" -> White
            | "U" -> Blue
            | "B" -> Black
            | "R" -> Red
            | "G" -> Green
            | _ -> failwith $"Unknown color {color}"
        | _ -> Gold

let crop (img: Image) (rect: Rectangle) =
    (new Bitmap(img)).Clone(rect, PixelFormat.Format32bppArgb)

let loadBackground (color: WatermarkColor) =
    use bmp = Bitmap.FromFile(FileSystem.backgroundPath color)
    let scaled = new Bitmap(bmp, Size(maxWidth, maxHeight))
    scaled

let maskImage (source: Bitmap) (mask: Bitmap) =
    let rect = Rectangle(0, 0, mask.Width, mask.Height)
    use source = crop source rect
    let bmp = source.Clone(rect, PixelFormat.Format32bppArgb)

    for y in [0..source.Height-1] do
        for x in [0..source.Width-1] do
            let sourcePx = source.GetPixel(x, y)
            let maskPx = mask.GetPixel(x, y)
            if maskPx.A = 255uy then   
                bmp.SetPixel(x, y, sourcePx)
            else
                bmp.SetPixel(x, y, Color.Transparent)

    bmp.MakeTransparent(Color.Transparent)

    bmp
