module Scryfall

open System.Net.Http
open ScryfallApi.Client
open System
open FSharp.Control.Tasks
open ScryfallApi.Client.Models
open System.IO

let private httpClient = new HttpClient()
httpClient.BaseAddress <- Uri "https://api.scryfall.com"

let mutable private config = ScryfallApiClientConfig ()
config.ScryfallApiBaseAddress <- Uri "https://api.scryfall.com"

let private scryfall = ScryfallApiClient(httpClient, config)

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

let mutable private setsCache = []
let getAllSets () = task {
    if setsCache |> List.isEmpty then
        let! results = scryfall.Sets.Get()
        setsCache <- results.Data |> Seq.toList
    return setsCache
}

let downloadSetSymbolSvg (code: string) = task {
    let inner () = task { 
        let set = setsCache |> Seq.find (fun s -> s.Code = code)    
        use request = new HttpRequestMessage(HttpMethod.Get, set.IconSvgUri)
        let! response = httpClient.SendAsync request
        let! contentStream = response.Content.ReadAsStreamAsync ()
        use stream = new FileStream(FileSystem.svgPath code, FileMode.Create)
        do! contentStream.CopyToAsync stream
    }

    if File.Exists (FileSystem.svgPath code) 
    then 
        printfn "Found downloaded SVG for %s" code
        return ()
    else 
        printfn "Downloading SVG for %s" code
        return! inner ()
}
