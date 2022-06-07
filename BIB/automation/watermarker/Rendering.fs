module Rendering

open Svg
open Model
open ScryfallApi.Client.Models
open System.Drawing

let toScaledBitmap (svg: SvgDocument) = 
    let maxWatermarkSize = 225

    let dimensions = svg.GetDimensions()

    // If height or width is 0, it preserves aspect ratio
    let rasterWidth, rasterHeight =
        if dimensions.Width > dimensions.Height
        then maxWatermarkSize, 0 
        else 0, maxWatermarkSize

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
    (new Bitmap(img)).Clone(rect, img.PixelFormat)

let maskImage (source: Bitmap) (mask: Bitmap) =
    let rect = Rectangle(0, 0, mask.Width, mask.Height)
    let source = crop source rect
    let bmp = new Bitmap(mask.Width, mask.Height)

    for y in [0..source.Height-1] do
        for x in [0..source.Width-1] do
            let sourcePx = source.GetPixel(x, y)
            let maskPx = mask.GetPixel(x, y)
            let newColor = if maskPx.A <> 255uy then Color.Transparent else sourcePx
            bmp.SetPixel(x, y, newColor)    

    bmp