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

let private getXDoc (cookie: string) (client : HttpClient) (url: string) : XDocument Task =    
    task {
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
        return doc
    }
    
let private getElementById (doc: XDocument, id: string): XElement =
    doc.Descendants()
    |> Seq.filter (fun el -> el.Attribute(XName.op_Implicit("id")) <> null)
    |> Seq.find (fun el -> el.Attribute(XName.op_Implicit("id")).Value = id)
    
let private getCardInfosFromSetPage (setName: string) (doc: XDocument) : CardInfo list =
    
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
                Set = setName
            }
        )
        |> Seq.toList
    
    cards
        
let private getCardDetailsFromCardPage (doc: XDocument) : CardDetails =
    let getValue(id: string): string = 
        let el = getElementById(doc, id)
        let valueAttr = el.Attribute(XName.op_Implicit("value"))
        if valueAttr <> null then valueAttr.Value.Trim()
        else el.Value.Trim()

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
        PlaneswalkerSize = getValue("pw-size")
        Rules2 = getValue("planeswalker-text-2")
        Rules3 = getValue("planeswalker-text-3")
        Rules4 = getValue("planeswalker-text-4")
        LoyaltyCost1 = getValue("loyalty-ability-1")
        LoyaltyCost2 = getValue("loyalty-ability-2")
        LoyaltyCost3 = getValue("loyalty-ability-3")
        LoyaltyCost4 = getValue("loyalty-ability-4")
    }
    card

let getSetCardInfos (cookie: string) (client: HttpClient) (setName: string) : CardInfo list Task =
    task {
        printfn "Loading list of cards in %s..." setName

        let url = sprintf "https://mtg.design/set/%s" setName
        let! page = getXDoc cookie client url
        let cards = getCardInfosFromSetPage setName page       
        
        printfn "Found %i cards:" cards.Length
        for c in cards do
            printfn "\t%s" c.Name
            
        return cards
    }

let getCardDetails (cookie: string) (client: HttpClient) (cardInfo: CardInfo) : CardDetails Task =
    task {
        printfn "\tParsing details for %s..." cardInfo.Name
        let url = sprintf "https://mtg.design/i/%s/edit" cardInfo.Id
        let! page = getXDoc cookie client url
        let card = getCardDetailsFromCardPage page
        printfn "\tParsed %s." cardInfo.Name
        return { card with Id = cardInfo.Id }
    }

let getSetCardDetails (cookie: string) (client : HttpClient) (setName : string) : CardDetails list Task = 
    task {
        printfn "Parsing card details..."
        let! cardInfos = getSetCardInfos cookie client setName
        let! cardDetails = cardInfos |> Utils.concurrentMap (getCardDetails cookie client)
        printfn "Card details parsed."
        return cardDetails
    }