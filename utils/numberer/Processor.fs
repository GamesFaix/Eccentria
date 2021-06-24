// Modifies cards before saving.
module Processor

open System
open System.Linq
open Model

let cardsToCenter = [
    "Time Walk"
    "Demonic Tutor"
    "Wheel of Fortune"
    "Jokulhaups"
    "Bazaar of Baghdad"
    "Shahrazad"
    "Buried in Time"
    "Martyr Drill"
    "Krark's Thumb"
    "Krark's Trampoline"
    "Thirst for Power"
    "Unnatural Gravity"
    "Master of None"
    "Never-Ending Story"
    "Black Lotus"
]

let getColors (card: CardDetails) : char list =
    card.ManaCost.Intersect(['W';'U';'B';'R';'G']) |> Seq.toList

let private getColorGroup (card: CardDetails) : ColorGroup = 
    if card.SuperType.ToLower().Contains("token") then ColorGroup.Token
    elif card.Type.ToLower().Contains("land") then ColorGroup.Land
    else
        let colors = getColors card

        match colors.Length with
        | 0 -> if card.Type.Contains("Artifact") then ColorGroup.Artifact else ColorGroup.Colorless
        | 1 -> match colors.Head with
                | 'W' -> ColorGroup.White
                | 'U' -> ColorGroup.Blue
                | 'B' -> ColorGroup.Black
                | 'R' -> ColorGroup.Red
                | 'G' -> ColorGroup.Green
                | _ -> failwith "invalid symbol"
        | _ -> if card.ManaCost.Contains("/") then ColorGroup.Hybrid else ColorGroup.Multi

let private generateNumbers (cards: CardDetails seq) : (int * CardDetails) seq =
    cards
    |> Seq.groupBy getColorGroup
    |> Seq.sortBy (fun (grp, _) -> grp)
    |> Seq.collect (fun (_, cs) -> cs |> Seq.sortBy (fun c -> c.Name))
    |> Seq.indexed
    |> Seq.map (fun (n, c) -> (n+1, c))

let private getCardTemplate (card: CardDetails) : string =
    let colors = getColors card
    match colors.Length with
    | 0 -> "C"
    | 1 -> colors.Head.ToString()
    | 2 -> 
        if not <| card.ManaCost.Contains('/') then "Gld"
        else String(colors |> Seq.toArray)
    | _ -> "Gld"   

let processCards (cards : CardDetails list) : CardDetails list =
    printfn "Processing cards..."

    printfn "\tGenerating card numbers..."
    let withNumbers = 
        let count = cards.Length
        generateNumbers cards 
        |> Seq.map (fun (n, c) -> 
            { c with 
                Number = n.ToString().PadLeft(count.ToString().Length, '0'); 
                Total = count.ToString() 
            })

    printfn "\tFixing centering bug..."
    let withCenteringCorrected =
        withNumbers
        |> Seq.map (fun c -> 
            if cardsToCenter |> Seq.contains c.Name 
            then { c with Center = "true" }
            else c
        )

    printfn "\tSetting templates..."
    let withTemplates =
        withCenteringCorrected
        |> Seq.map (fun c -> { c with Template = getCardTemplate c })

    withTemplates |> Seq.toList