﻿open System
open ScryfallApi.Client.Models
open FSharp.Control.Tasks

let createCheatSheet (cards: Card seq) =
    let createRow (c: Card) =        
        let name = c.Name.PadRight(30)
        let set = c.Set.PadRight(5)
        let color = Rendering.getColor c |> FileSystem.serialize
        $"{name} | {set} | {color}"

    let rows = cards |> Seq.map createRow
    String.Join("\n", rows)

let adjustSetSymbol (code: string) : string = 
    match code with
    | "cmb1" -> "sld"
    | "lea" -> "plist"
    | "pw09" -> "m10"
    | x -> x

[<EntryPoint>]
let main argv = 
    task {
        let! _ = Scryfall.getAllSets()
        let! cardNames = MtgDesign.getCardNames "BIB"

        let cardNames = 
            cardNames 
            |> Seq.filter (fun c ->
                c <> "Starfish" // Token generated by Alliances card, but not printed
            )

        let! cards = Scryfall.getCards cardNames
        cards |> List.iter (fun c -> 
            c.Set <- adjustSetSymbol c.Set)
        let cards = cards |> List.filter (fun c -> c.Set = "plist")
        
        do! Scryfall.downloadSetSymbolSvgs cards
            
        for c in cards do
            do! Rendering.createWatermarkPng c

        Console.WriteLine()

        let cheatSheet = createCheatSheet cards
        Console.WriteLine cheatSheet

        Console.ReadLine() |> ignore
        return 0
    } 
    |> Async.AwaitTask 
    |> Async.RunSynchronously