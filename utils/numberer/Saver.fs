// Saves card changes.
module Saver

open System
open System.Net.Http
open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Net
open System.Web
open Model

type SaverMode = Create | Edit

let private renderCard (card: CardDetails) (mode: SaverMode) (client: HttpClient) : unit Task =
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
        if not <| String.IsNullOrEmpty(card.ArtworkUrl) then query.Add("artwork", card.ArtworkUrl) else ()  
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
        query.Add("edit", if mode = SaverMode.Create then "false" else card.Id)

        let mutable url = sprintf "https://mtg.design/render?%s" (query.ToString())
        url <- url.Replace("+", "%20")
                  .Replace("%26rsquo%3b", "%E2%80%99")
                  .Replace("%26rsquo%253", "%E2%80%99")

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "render error" else ()

        printfn "Rendered %s." card.Name
        return ()
    }

let private shareCard (card : CardDetails) (mode: SaverMode) (client: HttpClient): unit Task =
    task {
        printfn "Sharing %s..." card.Name

        let query = HttpUtility.ParseQueryString("")
        query.Add("edit", if mode = SaverMode.Create then "false" else card.Id)
        query.Add("name", card.Name)
        
        let url = sprintf "https://mtg.design/shared?%s" (query.ToString())

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "share error" else ()
        
        printfn "Shared %s." card.Name
        return ()
    }

let saveCards (cards : CardDetails list) (mode : SaverMode) (client: HttpClient) : unit Task =
    task {
        // Must go in series or the same image gets rendered for each card
        for c in cards do
            let! _ = renderCard c mode client
            let! _ = shareCard c mode client
            ()
         
        return ()
    }

let private deleteCard (card: CardDetails) (client: HttpClient) : unit Task =
    task {
        printfn "Deleting %s - %s..." card.Set card.Name
        let url = sprintf "https://mtg.design/set/%s/i/%s/delete" card.Set card.Id
        let! response = client.GetAsync url
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "delete error" else ()        
        printfn "Deleted %s - %s." card.Set card.Name
        return ()
    }

let deleteCards (cards : CardDetails list) (client : HttpClient) : unit Task =
    task {
        for c in cards do
            let! _ = deleteCard c client
            ()

        return ()
    }