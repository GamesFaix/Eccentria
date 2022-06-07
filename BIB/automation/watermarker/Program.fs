open System
open ScryfallApi.Client
open System.Net.Http
open ScryfallApi.Client.Models
open FSharp.Control.Tasks
open System.IO
open Svg
open System.Drawing.Imaging
open System.Drawing

let cardNames = [ 
    "Hedron Crab"
    "Whirlpool Warrior"
    "Lightning Bolt"
    "The Underworld Cookbook"
]

let httpClient = new HttpClient()
httpClient.BaseAddress <- Uri "https://api.scryfall.com"

let mutable config = ScryfallApiClientConfig ()
config.ScryfallApiBaseAddress <- Uri "https://api.scryfall.com"

let scryfall = ScryfallApiClient(httpClient, config)

let getCard (name: string) = task {
    // https://scryfall.com/docs/syntax
    let query = $"!\"{name}\""
    
    let mutable options = SearchOptions()
    options.Mode <- SearchOptions.RollupMode.Prints
    options.Sort <- SearchOptions.CardSort.Released
    options.Direction <- SearchOptions.SortDirection.Asc

    let! results = scryfall.Cards.Search(query, 1, options)

    if results.TotalCards = 0 then
        return failwith $"No card found named \"{name}\"."
    else 
        return results.Data |> Seq.head
}

let mutable setsCache = []
let getAllSets () = task {
    if setsCache |> List.isEmpty then
        let! results = scryfall.Sets.Get()
        setsCache <- results.Data |> Seq.toList
    return setsCache
}

let svgPath (code: string) =
    $"c:/users/james/desktop/icons/{code}.svg"

let downloadSetSymbolSvg (code: string) = task {
    let inner () = task { 
        let set = setsCache |> Seq.find (fun s -> s.Code = code)    
        use request = new HttpRequestMessage(HttpMethod.Get, set.IconSvgUri)
        let! response = httpClient.SendAsync request
        let! contentStream = response.Content.ReadAsStreamAsync ()
        use stream = new FileStream(svgPath code, FileMode.Create)
        do! contentStream.CopyToAsync stream
    }

    if File.Exists (svgPath code) 
    then 
        printfn "Found downloaded SVG for %s" code
        return ()
    else 
        printfn "Downloading SVG for %s" code
        return! inner ()
}

let loadSetSymbolSvg (code: string) = task {
    do! downloadSetSymbolSvg code
    let! bytes = File.ReadAllBytesAsync (svgPath code)
    use stream = new MemoryStream(buffer = bytes)
    let svg = SvgDocument.Open stream
    return svg
}

let toScaledBitmap (svg: SvgDocument) = 
    let maxWatermarkSize = 225

    let dimensions = svg.GetDimensions()

    // If height or width is 0, it preserves aspect ratio
    let rasterWidth, rasterHeight =
        if dimensions.Width > dimensions.Height
        then maxWatermarkSize, 0 
        else 0, maxWatermarkSize

    svg.Draw(rasterWidth, rasterHeight)

// Not necessary with masking
//let setOpacity (opacity: float) (image: Image) =
//    let bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb)
//    use graphics = Graphics.FromImage bmp
//    let mutable matrix = ColorMatrix()
//    matrix.Matrix33 <- float32 opacity
//    use attributes = new ImageAttributes()
//    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap)
//    let rect = Rectangle(0, 0, bmp.Width, bmp.Height)
//    graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes)
//    bmp    

    // Not working
//let overlayColor (color: Color) (image: Image) =
//    let tr = float32 color.R / 255.0f
//    let tg = float32 color.G / 255.0f
//    let tb = float32 color.B / 255.0f
//    let matrix = ColorMatrix([| 
//        [| 0.0f; 0.0f; 0.0f; 0.0f; 0.0f |]
//        [| 0.0f; 0.0f; 0.0f; 0.0f; 0.0f |]
//        [| 0.0f; 0.0f; 0.0f; 0.0f; 0.0f |]
//        [| 0.0f; 0.0f; 0.0f; 0.0f; 0.0f |]
//        [|   tr;   tg;   tb; 0.0f; 1.0f |]
//    |])
//    use attributes = new ImageAttributes()
//    attributes.SetColorMatrix(matrix)

//    let bmp = new Bitmap(image.Width, image.Height)
//    use graphics = Graphics.FromImage bmp
//    let rect = Rectangle(0, 0, bmp.Width, bmp.Height)
//    graphics.DrawImage(bmp, rect, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attributes)
//    bmp

type WatermarkColor =
    | White
    | Blue
    | Black
    | Red
    | Green
    | Colorless
    // TODO: Add 2-color gradient masks
    | Gold
    // TODO: Add colored land masks
    | LandColorless

let getColor (c: Card) =
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

let serialize = function
    | White -> "white"
    | Blue -> "blue"
    | Black -> "black"
    | Red -> "red"
    | Green -> "green"
    | Colorless -> "colorless"
    | Gold -> "gold"
    | LandColorless -> "land-colorless"

let maskPath (color: WatermarkColor) =
    $"c:/users/james/desktop/icons/background-{serialize color}.png"

let pngPath (code: string) (color: WatermarkColor) =
    $"c:/users/james/desktop/icons/{code}-{serialize color}.png"

let createWatermarkPng (card: Card) = task {
    let color = getColor card

    let inner () = task {
        let! svg = loadSetSymbolSvg card.Set
        let bmp = toScaledBitmap svg
        // let mask = load image from maskPath 
        // Apply mask to symbol        
        
        let path = pngPath card.Set color
        bmp.Save(path, ImageFormat.Png)
    }

    let path = pngPath card.Set color
    if File.Exists path then
        printfn "Found PNG for %s - %s" card.Name path
        return ()
    else 
        printfn "Rendering PNG for %s - %s" card.Name path
        return! inner ()
}

// Ensure image mode is RGB
// Add color mask

[<EntryPoint>]
let main argv = 
    task {
        let! _ = getAllSets()

        for name in cardNames do
            let! c = getCard name
            printfn "%s first printing is %s" name c.Set
            do! createSetSymbolPng c.Set
            ()

        Console.ReadLine() |> ignore
        return 0
    } 
    |> Async.AwaitTask 
    |> Async.RunSynchronously