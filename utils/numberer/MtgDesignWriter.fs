// Saves card changes.
module MtgDesignWriter

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
        query.Add("card-accent", card.Accent)
        if card.SpecialFrames = "token" || card.Type.Contains("Land")
        then query.Add("land-overlay", card.LandOverlay) else ()
        query.Add("stars", "0") // ???
        query.Add("edit", if mode = SaverMode.Create then "false" else card.Id)
        if not <| String.IsNullOrEmpty(card.ColorIndicator) then query.Add("color-indicator", card.ColorIndicator) else ()  
        if not <| String.IsNullOrEmpty(card.PlaneswalkerSize) then query.Add("pw-size", card.PlaneswalkerSize) else ()
        if not <| String.IsNullOrEmpty(card.Rules2) then query.Add("pw-text2", card.Rules2) else ()
        if not <| String.IsNullOrEmpty(card.Rules3) then query.Add("pw-text3", card.Rules3) else ()
        if not <| String.IsNullOrEmpty(card.Rules4) then query.Add("pw-text4", card.Rules4) else ()
        // Not yet supporting loyalty costs for planeswalkers

        let mutable url = sprintf "https://mtg.design/render?%s" (query.ToString())
        url <- url.Replace("+", "%20")
                  .Replace("%26rsquo%3b", "%E2%80%99")
                  .Replace("%26rsquo%253", "%E2%80%99")

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "render error" else ()

        return ()
    }

let private shareCard (card : CardDetails) (mode: SaverMode) (client: HttpClient): unit Task =
    task {

        let query = HttpUtility.ParseQueryString("")
        query.Add("edit", if mode = SaverMode.Create then "false" else card.Id)
        query.Add("name", card.Name)
        
        let url = sprintf "https://mtg.design/shared?%s" (query.ToString())

        let! response = client.GetAsync(url)
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "share error" else ()
        
        return ()
    }

let private saveCard (client: HttpClient) (mode: SaverMode) (card: CardDetails) : unit Task =
    task {
        printfn "\tRendering (%s/%s) %s..." card.Number card.Total card.Name
        let! _ = renderCard card mode client
        printfn "\tRendered (%s/%s) %s." card.Number card.Total card.Name
        printfn "\tSharing (%s/%s) %s..." card.Number card.Total card.Name
        let! _ = shareCard card mode client
        printfn "\tShared (%s/%s) %s." card.Number card.Total card.Name
        ()
    }

let saveCards (client: HttpClient) (mode : SaverMode) (cards : CardDetails list) : unit Task =
    printfn "Saving cards..."
    // Must go in series or the same image gets rendered for each card
    cards |> Utils.seriesMap (saveCard client mode) |> Utils.mergeUnit
    
let deleteCard (client: HttpClient) (card: CardInfo) : unit Task =
    task {
        printfn "\tDeleting %s - %s..." card.Set card.Name
        let url = sprintf "https://mtg.design/set/%s/i/%s/delete" card.Set card.Id
        let! response = client.GetAsync url
        if response.StatusCode >= HttpStatusCode.BadRequest then failwith "delete error" else ()        
        printfn "\tDeleted %s - %s." card.Set card.Name
        return ()
    }

let deleteCards (cards : CardInfo list) (client : HttpClient) : unit Task =
    printfn "Deleting cards..."
    cards |> Utils.concurrentMap (deleteCard client) |> Utils.mergeUnit