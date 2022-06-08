module Rendering

open Svg
open Model
open ScryfallApi.Client.Models
open System.Drawing
open System.Drawing.Imaging
open FSharp.Control.Tasks
open System.IO
open System

let maxHeight = 280
let maxSize = Size(maxHeight * 2, maxHeight)

let private loadSetSymbolSvg (code: string) = task {
    let! bytes = File.ReadAllBytesAsync (FileSystem.svgPath code)
    use stream = new MemoryStream(buffer = bytes)
    let svg = SvgDocument.Open stream
    return svg
}

let private getMaxRasterSize (svgDimensions: SizeF) (max: Size): Size =
    let maxScaleX = float32 max.Width / svgDimensions.Width 
    let maxScaleY = float32 max.Height / svgDimensions.Height
    let scale = Math.Min(maxScaleX, maxScaleY)
    let width = svgDimensions.Width * scale |> int
    let height = svgDimensions.Height * scale |> int
    Size(width, height)

let private toScaledBitmap (svg: SvgDocument) = 
    let dimensions = svg.GetDimensions()
    let size = getMaxRasterSize dimensions maxSize
    svg.Draw(size.Width, size.Height)

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
        let! svg = loadSetSymbolSvg card.Set
        use mask = toScaledBitmap svg
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