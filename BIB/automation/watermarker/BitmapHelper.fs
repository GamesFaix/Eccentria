module BitmapHelper

open System.Drawing

type private PixelSpan = int * Color seq

let private getRow (bmp: Bitmap) (i: int) : PixelSpan =
    i, seq { for x in 0..bmp.Width-1 do bmp.GetPixel(x, i) }

let private getRows (bmp: Bitmap) (reverse: bool): PixelSpan seq =
    let order = [0..bmp.Height-1]
    let order = if reverse then order |> List.rev else order
    seq { for y in order do getRow bmp y }

let private getColumn (bmp: Bitmap) (i: int) : PixelSpan =
    i, seq { for y in 0..bmp.Height-1 do bmp.GetPixel(i, y) }

let private getColumns (bmp: Bitmap) (reverse: bool) : PixelSpan seq =
    let order = [0..bmp.Width-1]
    let order = if reverse then order |> List.rev else order
    seq { for x in order do getColumn bmp x }

let getBounds (bmp: Bitmap) : Rectangle =
    // Iterate the rows and columns of pixels, and note the first one in each direction that has a non-transparent pixel

    let hasVisiblePixel ((_, pxs): PixelSpan) = pxs |> Seq.exists(fun px -> px.A > 0uy)

    let top =  getRows bmp false |> Seq.tryFind hasVisiblePixel |> Option.map (fun (y, _) -> y) |> Option.defaultValue 0
    let bottom = getRows bmp true |> Seq.tryFind hasVisiblePixel |> Option.map (fun (y, _) -> y) |> Option.defaultValue 0
    let left = getColumns bmp false |> Seq.tryFind hasVisiblePixel |> Option.map (fun (x, _) -> x) |> Option.defaultValue 0
    let right = getColumns bmp true |> Seq.tryFind hasVisiblePixel |> Option.map (fun (x, _) -> x) |> Option.defaultValue 0

    Rectangle(left, top, right-left, bottom-top)

