﻿open System
open System.IO
open System.Net.Http
open System.Threading.Tasks
open System.Xml
open System.Xml.Linq
open FSharp.Control.Tasks
open Sgml
open System.Text.RegularExpressions
open System.Linq

(* 
    mtg.design uses server side rendering, so no API available
    Load the page for a set, then parse the DOM to find links to each card in the set to get their IDs.
    Go to the edit page for each set to get structured data about each card, by reading the form
    Create in-memory index of (cardID, card name, mana cost, type)
    Sort cards and assign collector numbers
    Send request to server to update each card
    Make sure to click Center box for select cards becaues it is never set in the form when loading an existing card
*)

let client = new HttpClient()

let setName = "REP"

let cookie = "remember_web_59ba36addc2b2f9401580f014c7f58ea4e30989d=eyJpdiI6IkZkTVl5dE5ScEJ1Y0xWeUpNZktYckE9PSIsInZhbHVlIjoiXC9FbVdEZWJlRUlHNDhzTmVMZVVCSWxNaWFSbCtNcFV1N0E4UjI4cDF6YkQ1VmtGXC9STFpHYm1raWVFYTF4dERaWTlmQkxSdTc2bU03VDF3c3h1RHlPWDlLOGVCUHBkdFZnWHNMblwvNjFuQXc9IiwibWFjIjoiOTM5ZDhkMTQwY2RlMzQ4M2IyZTM1ZjM1Y2ZlYjI0YjI2NmVhNDg1ZWU5YjI4ZWE0ZmViYmIyZmU3NjkzNWU3YiJ9; XSRF-TOKEN=eyJpdiI6IkNjMzhEcmVsaVpQb3JKR0VDckxcL1Z3PT0iLCJ2YWx1ZSI6IjZGbEQ2cTZtb0NpbE9DWnJxXC9oMlwvT0hOREN3T0g4cHIrVkphWHRSazFxblRvTmFFUkJaM0VoREhhZFNpRkFjZzFEQ29nRVBVN0Q2YU5yd1E0K3Z1Tmc9PSIsIm1hYyI6ImM5ZTc1YTY3YWM3ODJjYmZiMWUzNTkzZGE3NWJiNzcyNjlkYzFjYTIxZWZkZmJmNzM3MWRiYTU1YjFlMjM3ZGQifQ%3D%3D; laravel_session=fbd5eec3495cd3f3de7464700746e7283043afc9"

let getXDoc (url: string) : XDocument Task =
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

        return XDocument.Load(sgmlReader)
    }

let getCardListFromSetPage(doc: XDocument) : (string * string) list =
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
            let name = a.Value
            let url = a.Attribute(hrefName).Value
            let m = Regex.Match(url, "https://mtg.design/i/(\w+)/edit")
            let id = m.Groups.[1].Value
            (name, id)
        )
        |> Seq.toList

    cards

let getSetPage (name: string) : XDocument Task =
    let url = sprintf "https://mtg.design/set/%s" name
    getXDoc url

let getCardEditPage (id: string) : XDocument Task =
    let url = sprintf "https://mtg.design/i/%s/edit" id
    getXDoc url

type CardDetails = {
    Id: string
    Number: string
    Total: string
    Set: string
    Lang: string
    Designer: string
    Name: string
    ManaCost: string
    SuperType: string
    Type: string
    SubType: string
    SpecialFrames: string
    ColorIndicator: string
    Rarity: string
    RulesText: string
    FlavorText: string
    TextSize: string
    Center: string
    Foil: string
    Border: string
    ArtworkUrl: string
    CustomSetSymbolUrl: string
    WatermarkUrl: string
    LightenWatermark: string
    Artist: string
    Power: string
    Toughness: string
    // Extra properties that are conditional based on other choices, like saga frame
}

let getElementById (doc: XDocument, id: string): XElement =
    doc.Descendants()
    |> Seq.filter (fun el -> el.Attribute(XName.op_Implicit("id")) <> null)
    |> Seq.find (fun el -> el.Attribute(XName.op_Implicit("id")).Value = id)

let getCardDetailsFromPage (doc: XDocument) : CardDetails =
    let getValue(id: string): string = 
        let el = getElementById(doc, id)
        let valueAttr = el.Attribute(XName.op_Implicit("value"))
        if valueAttr <> null then valueAttr.Value
        else el.Value

    {
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
    }

type ColorGroup =
    | Colorless = 1
    | White = 2
    | Blue = 3
    | Black = 4
    | Red = 5
    | Green = 6
    | Multi = 7
    | Hybrid = 8
    | Artifact = 9
    | Land = 10
    | Token = 11

let getColorGroup (card: CardDetails) : ColorGroup = 
    if card.SuperType.Contains("Token") then ColorGroup.Token
    elif card.Type = "Land" then ColorGroup.Land
    else
        let coloredManaSymbols = 
            card.ManaCost.GroupBy(fun c -> c) 
            |> Seq.filter (fun grp -> ['W';'U';'B';'R';'G'] |> Seq.contains grp.Key)
            |> Seq.toList

        match coloredManaSymbols.Length with
        | 0 -> if card.Type.Contains("Artifact") then ColorGroup.Artifact else ColorGroup.Colorless
        | 1 -> match coloredManaSymbols.[0].Key with
                | 'W' -> ColorGroup.White
                | 'U' -> ColorGroup.Blue
                | 'B' -> ColorGroup.Black
                | 'R' -> ColorGroup.Red
                | 'G' -> ColorGroup.Green
                | _ -> failwith "invalid symbol"
        | _ as n -> if card.ManaCost.Contains("/") then ColorGroup.Hybrid else ColorGroup.Multi

let generateNumbers (cards: CardDetails seq) : (int * CardDetails) seq =
    cards
    |> Seq.groupBy getColorGroup
    |> Seq.sortBy (fun (grp, _) -> grp)
    |> Seq.collect (fun (_, cs) -> cs |> Seq.sortBy (fun c -> c.Name))
    |> Seq.indexed
    |> Seq.map (fun (n, c) -> (n+1, c))

[<EntryPoint>]
let main argv =
    let setPage = getSetPage(setName).Result
    let cards = getCardListFromSetPage setPage

    let cardDetails =
        cards
        |> Seq.map (fun (_, id) -> 
            let cardPage = getCardEditPage(id).Result
            let card = getCardDetailsFromPage cardPage
            let card = { card with Id = id }
            card
        )
        |> Seq.toList

    let withNumbers = 
        generateNumbers cardDetails 
        |> Seq.map (fun (n, c) -> { c with Number = n.ToString() })
        |> Seq.toList

    let xml = setPage.ToString()
    Console.WriteLine(xml)

    Console.Read() |> ignore
    0 // return an integer exit code