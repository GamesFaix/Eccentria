module SvgHelper

open System.Drawing
open Svg
open System

let private getMaxScaleForContainer (containerSize: SizeF) (contentSize: SizeF) : float32 =
    let maxXScale = float32 containerSize.Width / contentSize.Width
    let maxYScale = float32 containerSize.Height / contentSize.Height
    Math.Min(maxXScale, maxYScale)

let private scaleRect (rect: Rectangle) (scale: float32) : Rectangle =
    let left = float32 rect.Left * scale |> int
    let top = float32 rect.Top * scale |> int
    let width = float32 rect.Width * scale |> int
    let height = float32 rect.Height * scale |> int
    Rectangle(left, top, width, height)

let renderAsLargeAsPossibleInContainerWithNoMargin (svgPath: string) (containerSize: SizeF) : Bitmap =
    // Open the SVG and render to BMP
    let svg = SvgDocument.Open(svgPath)
    use originalBmp = svg.Draw()

    // Find the bounds of the content in the BMP, and use that to determine the largest scale the SVG
    // can be rendered at, while the content still fits in the container
    let bounds = BitmapHelper.getBounds originalBmp
    let scale = getMaxScaleForContainer containerSize (SizeF(float32 bounds.Width, float32 bounds.Height))

    // Render the SVG to BMP again, at the right scale
    let dim = svg.GetDimensions()
    let rasterWidth = scale * dim.Width |> int
    let rasterHeight = scale * dim.Height |> int
    use resizedBmp = svg.Draw(rasterWidth, rasterHeight)

    // Find the scaled bounds of the content, and crop the BMP to remove whitespace
    let scaledBounds = scaleRect bounds scale
    BitmapHelper.crop resizedBmp scaledBounds
