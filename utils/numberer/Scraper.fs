// Reads data from pages
module Scraper

open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Xml
open System.Xml.Linq
open FSharp.Control.Tasks
open Sgml
open System.Text.RegularExpressions
open Model

let getXDoc (url: string) (cookie: string) (client : HttpClient): XDocument Task =    
    task {
        printfn "Getting document from %s..." url
        use request = new HttpRequestMessage()
        request.RequestUri <- Uri(url)
        request.Method <- HttpMethod.Get
        request.Headers.Add("Cookie", cookie)

        let! response = client.SendAsync(request)
        let! stream = response.Content.ReadAsStreamAsync()

        let sgmlReader = new SgmlReader()
        sgmlReader.DocType <- "HTML"
        sgmlReader.WhitespaceHandling <- WhitespaceHandling.All
        sgmlReader.CaseFolding <- CaseFolding.ToLower
        sgmlReader.InputStream <- new StreamReader(stream)

        let doc = XDocument.Load(sgmlReader)

        printfn "Loaded document from %s." url
        return doc
    }
    
let getCardListFromSetPage(doc: XDocument) : CardInfo list =
    printfn "Parsing list of cards from page..."
    
    let listElements = 
        doc.Descendants() 
        |> Seq.filter (fun el -> el.Name.LocalName = "li")
        |> Seq.toList
    
    let withParagraphs =
        listElements
        |> Seq.collect (fun li -> 
            li.Descendants() 
            |> Seq.filter(fun el -> el.Name.LocalName = "p") 
            |> Seq.map(fun p -> (li, p)))
    
    let withLinks =
        withParagraphs
        |> Seq.map (fun (li, p) -> (li, p, p.Descendants() |> Seq.tryHead))
        |> Seq.filter (fun (li, p, maybeDesc) -> 
            match maybeDesc with
            | Some child -> child.Name.LocalName = "a"
            | _ -> false)
        |> Seq.map (fun (li, p, maybeDesc) -> (li, p, maybeDesc.Value))
        |> Seq.toList
    
    let hrefName = XName.op_Implicit("href")
    
    let cards =
        withLinks
        |> Seq.map (fun (li, p, a) -> 
            let url = a.Attribute(hrefName).Value
            let m = Regex.Match(url, "https://mtg.design/i/(\w+)/edit")
            {
                Id = m.Groups.[1].Value
                Name = a.Value
            }
        )
        |> Seq.toList
    
    printfn "Found %i cards:" cards.Length
    for c in cards do
        printfn "\t%s" c.Name
    
    cards
    
let getSetPage (name: string) (cookie: string) (client : HttpClient) : XDocument Task =
    let url = sprintf "https://mtg.design/set/%s" name
    getXDoc url cookie client
    
let getCardEditPage (id: string) (cookie: string) (client : HttpClient) : XDocument Task =
    let url = sprintf "https://mtg.design/i/%s/edit" id
    getXDoc url cookie client
    
let getElementById (doc: XDocument, id: string): XElement =
    doc.Descendants()
    |> Seq.filter (fun el -> el.Attribute(XName.op_Implicit("id")) <> null)
    |> Seq.find (fun el -> el.Attribute(XName.op_Implicit("id")).Value = id)

let getCardDetailsFromPage (doc: XDocument) : CardDetails =
    printfn "Parsing card details..."

    let getValue(id: string): string = 
        let el = getElementById(doc, id)
        let valueAttr = el.Attribute(XName.op_Implicit("value"))
        if valueAttr <> null then valueAttr.Value
        else el.Value

    let card = {
        Id = ""
        Number = getValue("card-number")
        Total = getValue("card-total")
        Set = getValue("card-set")
        Lang = getValue("language")
        Designer = getValue("designer")
        Name = getValue("card-title")
        ManaCost = getValue("mana-cost")
        SuperType = getValue("super-type")
        Type = getValue("type")
        SubType = getValue("sub-type")
        SpecialFrames = getValue("card-layout")
        ColorIndicator = getValue("color-indicator")
        Rarity = getValue("rarity")
        RulesText = getValue("rules-text")
        FlavorText = getValue("flavor-text")
        TextSize = getValue("text-size")
        Center = getValue("centered")
        Foil = getValue("foil")
        Border = getValue("card-border")
        ArtworkUrl = getValue("artwork")
        CustomSetSymbolUrl = getValue("set-symbol")
        WatermarkUrl = getValue("watermark")
        LightenWatermark = getValue("lighten")
        Artist = getValue("artist")
        Power = getValue("power")
        Toughness = getValue("toughness")
        LandOverlay = getValue("land-overlay")
        Template = ""
    }

    printfn "Parsed %s." card.Name
    card

let getCardDetails (cardInfos: CardInfo list) (cookie: string) (client: HttpClient) : CardDetails list Task =
    task {
        let tasks = 
            cardInfos 
            |> Seq.map (fun c -> 
                task { 
                    let! cardPage = (getCardEditPage c.Id cookie client)
                    let card = getCardDetailsFromPage cardPage
                    return { card with Id = c.Id }
                })
            |> Seq.toList

        let! _ = Task.WhenAll tasks

        return tasks |> List.map (fun t -> t.Result)
    }
