open System
open ScryfallApi.Client
open System.Net.Http
open ScryfallApi.Client.Models
open FSharp.Control.Tasks
open System.IO
open Svg
open System.Drawing.Imaging
open System.Drawing

let workingDir = "c:/github/jamesfaix/eccentria/bib/watermarks"

let cardNames = [ 
    "Hedron Crab"
    "Whirlpool Warrior"
    "Lightning Bolt"
    "The Underworld Cookbook"
    "Island Fish Jasconius"
    "Unicycle"
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
    options.IncludeExtras <- true

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
    $"{workingDir}/{code}.svg"

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

let backgroundPath (color: WatermarkColor) =
    $"{workingDir}/background-{serialize color}.png"

let watermarkPath (code: string) (color: WatermarkColor) =
    $"{workingDir}/{code}-{serialize color}.png"

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

let createWatermarkPng (card: Card) = task {
    let color = getColor card

    let inner () = task {
        let! svg = loadSetSymbolSvg card.Set
        let mask = toScaledBitmap svg
        let background = Bitmap.FromFile(backgroundPath color) :?> Bitmap
        let watermark = maskImage background mask        
        let path = watermarkPath card.Set color
        watermark.Save(path, ImageFormat.Png)
    }

    let path = watermarkPath card.Set color
    if File.Exists path then
        printfn "Found PNG for %s - %s" card.Name path
        return ()
    else 
        printfn "Rendering PNG for %s - %s" card.Name path
        return! inner ()
}

[<EntryPoint>]
let main argv = 
    task {
        let! _ = getAllSets()

        for name in cardNames do
            let! c = getCard name
            printfn "%s first printing is %s" name c.Set
            do! createWatermarkPng c

        Console.ReadLine() |> ignore
        return 0
    } 
    |> Async.AwaitTask 
    |> Async.RunSynchronously