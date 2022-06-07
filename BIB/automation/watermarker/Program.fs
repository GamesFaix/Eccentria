open System
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
    "Island Fish Jasconius"
    "Unicycle"
    "Sliver Queen"
]

let loadSetSymbolSvg (code: string) = task {
    do! Scryfall.downloadSetSymbolSvg code
    let! bytes = File.ReadAllBytesAsync (FileSystem.svgPath code)
    use stream = new MemoryStream(buffer = bytes)
    let svg = SvgDocument.Open stream
    return svg
}

let createWatermarkPng (card: Card) = task {
    let color = Rendering.getColor card

    let inner () = task {
        let! svg = loadSetSymbolSvg card.Set
        let mask = Rendering.toScaledBitmap svg
        let background = Bitmap.FromFile(FileSystem.backgroundPath color) :?> Bitmap
        let watermark = Rendering.maskImage background mask        
        let path = FileSystem.watermarkPath card.Set color
        watermark.Save(path, ImageFormat.Png)
    }

    let path = FileSystem.watermarkPath card.Set color
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
        let! _ = Scryfall.getAllSets()

        for name in cardNames do
            let! c = Scryfall.getCard name
            printfn "%s first printing is %s" name c.Set
            do! createWatermarkPng c

        Console.ReadLine() |> ignore
        return 0
    } 
    |> Async.AwaitTask 
    |> Async.RunSynchronously