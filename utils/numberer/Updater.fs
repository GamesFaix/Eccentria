// Saves card changes.
module Updater

open System
open System.Net.Http
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Net
open System.Web
open Model

let renderCard (card: CardDetails) (client: HttpClient) : unit Task =
    task {
        printfn "Rendering %s..." card.Name

        let query = HttpUtility.ParseQueryString("")
        query.Add("card-number", card.Number)
        query.Add("card-total", card.Total)
        query.Add("card-set", card.Set)
        query.Add("language", card.Lang)
        query.Add("card-title", card.Name)
        query.Add("mana-cost", card.ManaCost)
        if not <| String.IsNullOrEmpty(card.SuperType) then query.Add("super-type", card.SuperType) else ()
        if not <| String.IsNullOrEmpty(card.SubType) then query.Add("sub-type", card.SubType) else ()
        if not <| String.IsNullOrEmpty(card.Center) then query.Add("centered", "true") else ()
        query.Add("type", card.Type)
        query.Add("text-size", card.TextSize)
        query.Add("rarity", card.Rarity)
        query.Add("artist", card.Artist)
        query.Add("power", card.Power)
        query.Add("toughness", card.Toughness)
        query.Add("artwork", card.ArtworkUrl)        
        query.Add("designer", card.Designer)
        query.Add("card-border", card.Border)
        query.Add("watermark", card.WatermarkUrl)
        query.Add("card-layout", card.SpecialFrames)
        query.Add("set-symbol", card.CustomSetSymbolUrl)
        query.Add("rules-text", card.RulesText)
        query.Add("flavor-text", card.FlavorText)
        query.Add("card-template", card.Template)
        query.Add("card-accent", card.LandOverlay)
        query.Add("stars", "0") // ???
        query.Add("edit", card.Id)

        let url = sprintf "https://mtg.design/render?%s" (query.ToString())

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "render error" else ()

        printfn "Rendered %s." card.Name
        return ()
    }

let shareCard (card : CardDetails) (client: HttpClient): unit Task =
    task {
        printfn "Sharing %s..." card.Name

        let query = HttpUtility.ParseQueryString("")
        query.Add("edit", card.Id)
        query.Add("name", card.Name)
        
        let url = sprintf "https://mtg.design/shared?%s" (query.ToString())

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "share error" else ()
        
        printfn "Shared %s." card.Name
        return ()
    }

let saveCards (cards : CardDetails list) (client: HttpClient) : unit Task =
    task {
        let tasks =
            cards
            |> List.map (fun c -> 
                task {
                    let! _ = renderCard c client
                    let! _ = shareCard c client
                    return ()
                })

        let! _ = Task.WhenAll tasks
        return ()
    }