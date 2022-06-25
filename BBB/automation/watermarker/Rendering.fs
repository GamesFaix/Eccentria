module Rendering

open Model
open ScryfallApi.Client.Models
open System.Drawing
open System.Drawing.Imaging
open FSharp.Control.Tasks
open System.IO
open System.Drawing.Drawing2D

let maxWidth = 375
let maxHeight = 235
let maxSize = Size(maxWidth, maxHeight)

let private toFloat (size: Size) = SizeF(float32 size.Width, float32 size.Height)

let getWatermarkType (c: Card) : WatermarkType =
    let parse (colors: string seq) =
        match colors |> Seq.sort |> Seq.toList with
        | [ ] -> Colorless
        | [ "W" ] -> One White
        | [ "U" ] -> One Blue
        | [ "B" ] -> One Black
        | [ "R" ] -> One Red
        | [ "G" ] -> One Green
        | [ "U"; "W" ] -> Two (White, Blue)
        | [ "B"; "U" ] -> Two (Blue, Black) 
        | [ "B"; "R" ] -> Two (Black, Red) 
        | [ "G"; "R" ] -> Two (Red, Green) 
        | [ "G"; "W" ] -> Two (Green, White) 
        | [ "B"; "W" ] -> Two (White, Black) 
        | [ "R"; "U" ] -> Two (Blue, Red) 
        | [ "B"; "G" ] -> Two (Black, Green) 
        | [ "R"; "W" ] -> Two (Red, White) 
        | [ "G"; "U" ] -> Two (Green, Blue) 
        | _ -> Multi

    if c.TypeLine.Contains("Land") then 
        let colors = 
            match parse c.ColorIdentity with
            | Colorless ->
                if c.OracleText.Contains("any color") 
                then Multi
                else Colorless
            | x -> x
        Land, colors
    else Spell, parse c.Colors

let generateBackGround (w: WatermarkType) (size: Size) =
    let percent b =
        (float b)/ 100.0 * 255.0 |> int

    let white =  Color.FromArgb(60 |> percent, 189, 172, 129)
    let blue =   Color.FromArgb(60 |> percent, 132, 152, 175)
    let black =  Color.FromArgb(60 |> percent, 144, 139, 138)
    let red =    Color.FromArgb(60 |> percent, 220, 151, 112)
    let green =  Color.FromArgb(60 |> percent, 135, 164, 134)
    let gold =   Color.FromArgb(60 |> percent, 191, 170, 93)
    let silver = Color.FromArgb(60 |> percent, 124, 136, 145)
    let gray =   Color.FromArgb(60 |> percent, 110, 109, 107)

    let landWhite = Color.FromArgb(60 |> percent, 185, 165, 99)
    let landBlue = Color.FromArgb(60 |> percent, 84, 138, 175)
    let landBlack = Color.FromArgb(60 |> percent, 95, 84, 90)
    let landRed = Color.FromArgb(60 |> percent, 201, 109, 62)
    let landGreen = Color.FromArgb(60 |> percent, 113, 149, 119)

    let solid c =
        new SolidBrush(c) :> Brush

    let gradient c1 c2 =
        let middleLeft = Point(0, size.Height/2)
        let middleRight = Point(size.Width, size.Height/2)
        new LinearGradientBrush(middleLeft, middleRight, c1, c2) :> Brush

    let getSpellColor = function
        | White -> white
        | Blue -> blue
        | Black -> black
        | Red -> red
        | Green -> green

    let getLandColor = function
        | White -> landWhite
        | Blue -> landBlue
        | Black -> landBlack
        | Red -> landRed
        | Green -> landGreen

    let getSpellBrush = function
        | Colorless -> solid silver
        | One c -> solid (getSpellColor c)
        | Two (a,b) -> gradient (getSpellColor a) (getSpellColor b)
        | Multi -> solid gold

    let getLandBrush = function
        | Colorless -> solid gray
        | One c -> solid (getLandColor c)
        | Two (a, b) -> gradient (getLandColor a) (getLandColor b)
        | Multi -> solid gold // TODO: Fix this

    let getBrush = function
        | Spell, c -> getSpellBrush c
        | Land, c -> getLandBrush c

    let img = new Bitmap(size.Width, size.Height)
    use g = Graphics.FromImage img
    use b = getBrush w
    g.FillRectangle(b, Rectangle(0, 0, size.Width, size.Height))
    img

let private maskImage (source: Bitmap) (mask: Bitmap) =
    let rect = Rectangle(0, 0, mask.Width, mask.Height)
    let source = BitmapHelper.crop source rect

    for y in [0..source.Height-1] do
        for x in [0..source.Width-1] do
            let sourcePx = source.GetPixel(x, y)
            let maskPx = mask.GetPixel(x, y)
            let color = if maskPx.A = 255uy then sourcePx else Color.Transparent
            source.SetPixel(x, y, color)

    source.MakeTransparent(Color.Transparent)

    source
    
let createWatermarkPng (card: Card) = task {
    let color = getWatermarkType card
    let path = FileSystem.watermarkPath card.Set color
    let maskPath = FileSystem.maskPath card.Set
    
    let createMask () = task {
        let svgPath = FileSystem.svgPath card.Set
        let mask = SvgHelper.renderAsLargeAsPossibleInContainerWithNoMargin svgPath (maxSize |> toFloat)
        mask.Save maskPath
        return mask
    }

    let getOrCreateMask () = task {
        if File.Exists maskPath then
            return new Bitmap(maskPath)
        else
            return! createMask ()
    }

    let createWatermark () = task {
        use! mask = getOrCreateMask ()
        use background = generateBackGround color mask.Size
        use watermark = maskImage background mask :> Image
        watermark.Save(path, ImageFormat.Png)
    }
    
    //if File.Exists path then
    //    printfn "Found PNG for %s - %s" card.Name path
    //    return ()
    //else 
    printfn "Rendering PNG for %s - %s..." card.Name path
    return! createWatermark ()
}