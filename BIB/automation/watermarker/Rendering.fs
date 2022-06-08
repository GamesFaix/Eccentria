module Rendering

open Model
open ScryfallApi.Client.Models
open System.Drawing
open System.Drawing.Imaging
open FSharp.Control.Tasks

let maxHeight = 250
let maxSize = Size(maxHeight * 2, maxHeight)

let private toFloat (size: Size) = SizeF(float32 size.Width, float32 size.Height)

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

let private crop (img: Bitmap) (rect: Rectangle) =
    img.Clone(rect, PixelFormat.Format32bppArgb)

let loadBackground (color: WatermarkColor) =
    use bmp = Bitmap.FromFile(FileSystem.backgroundPath color)
    new Bitmap(bmp, maxSize)
    
let private maskImage (source: Bitmap) (mask: Bitmap) =
    let rect = Rectangle(0, 0, mask.Width, mask.Height)
    let source = crop source rect

    for y in [0..source.Height-1] do
        for x in [0..source.Width-1] do
            let sourcePx = source.GetPixel(x, y)
            let maskPx = mask.GetPixel(x, y)
            let color = if maskPx.A = 255uy then sourcePx else Color.Transparent
            source.SetPixel(x, y, color)

    source.MakeTransparent(Color.Transparent)

    source
    
let createWatermarkPng (card: Card) = task {
    let color = getColor card
    let path = FileSystem.watermarkPath card.Set color
    
    let inner () = task {
        let svgPath = FileSystem.svgPath card.Set
        use mask = SvgHelper.renderAsLargeAsPossibleInContainerWithNoMargin svgPath (maxSize |> toFloat)
        use background = loadBackground color
        use watermark = maskImage background mask :> Image
        watermark.Save(path, ImageFormat.Png)
    }
    
    //if File.Exists path then
    //    printfn "Found PNG for %s - %s" card.Name path
    //    return ()
    //else 
    printfn "Rendering PNG for %s - %s..." card.Name path
    return! inner ()
}