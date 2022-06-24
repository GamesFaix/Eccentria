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

let getColor (c: Card) : WatermarkColor =
    let parse (colors: string seq) =
        match colors |> Seq.sort |> Seq.toList with
        | [ "W" ] -> Some White
        | [ "U" ] -> Some Blue
        | [ "B" ] -> Some Black
        | [ "R" ] -> Some Red
        | [ "G" ] -> Some Green
        | [ "U"; "W" ] -> Some WhiteBlue
        | [ "B"; "U" ] -> Some BlueBlack
        | [ "B"; "R" ] -> Some BlackRed
        | [ "G"; "R" ] -> Some RedGreen
        | [ "G"; "W" ] -> Some GreenWhite
        | [ "B"; "W" ] -> Some WhiteBlack
        | [ "R"; "U" ] -> Some BlueRed
        | [ "B"; "G" ] -> Some BlackGreen
        | [ "R"; "W" ] -> Some RedWhite
        | [ "G"; "U" ] -> Some GreenBlue
        | _ -> None

    if c.TypeLine.Contains("Land") then 
        match parse c.ColorIdentity with
        | Some x -> x
        | _ ->
            if c.OracleText.Contains("any color") then Gold
            elif c.ColorIdentity = [| |] then LandColorless
            else Gold
    else
        match parse c.Colors with
        | Some x -> x
        | _ ->
            if c.Colors = [| |] then Colorless
            else Gold

let generateBackGround (color: WatermarkColor) (size: Size) =
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

    let solid c =
        new SolidBrush(c) :> Brush

    let gradient c1 c2 =
        let middleLeft = Point(0, size.Height/2)
        let middleRight = Point(size.Width, size.Height/2)
        new LinearGradientBrush(middleLeft, middleRight, c1, c2) :> Brush

    let getBrush = function
        | White -> solid white
        | Blue ->  solid blue
        | Black -> solid black
        | Red ->   solid red
        | Green -> solid green
        | WhiteBlue ->  gradient white blue
        | BlueBlack ->  gradient blue black
        | BlackRed ->   gradient black red
        | RedGreen ->   gradient red green
        | GreenWhite -> gradient green white
        | WhiteBlack -> gradient white black
        | BlueRed ->    gradient blue red
        | BlackGreen -> gradient black green
        | RedWhite ->   gradient red white
        | GreenBlue ->  gradient green blue
        | Gold ->          solid gold
        | Colorless ->     solid silver
        | LandColorless -> solid gray

    let img = new Bitmap(size.Width, size.Height)
    use g = Graphics.FromImage img
    use b = getBrush color
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
    let color = getColor card
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