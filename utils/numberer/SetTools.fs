// High-level commands for managing sets
module SetTools

open System.Net.Http
open FSharp.Control.Tasks
open System.Threading.Tasks

let autonumberSet (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        printfn "Auto-numbering %s..." setName
        let! cardDetails = Scraper.getSetCardDetails cookie client setName
        let processed = Processor.processCards cardDetails
        let! _ = Saver.saveCards client Saver.SaverMode.Edit processed
        printfn "Done."
        return ()
    }

let renameSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        printfn "Renaming %s to %s..." oldName newName
        let! cardDetails = Scraper.getSetCardDetails cookie client oldName
        let processed = Processor.processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = Saver.saveCards client Saver.SaverMode.Edit processed
        printfn "Done."
        return ()
    }

let cloneSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        printfn "Cloning %s to %s..." oldName newName
        let! cardDetails = Scraper.getSetCardDetails cookie client oldName
        let processed = Processor.processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = Saver.saveCards client Saver.SaverMode.Create processed
        printfn "Done."
        return ()
    }

let deleteCard (client: HttpClient) (cookie: string) (setName : string) (cardName: string) : unit Task =
    task {
        printfn "Deleting %s - %s..." setName cardName
        let! cardInfos = Scraper.getSetCardInfos cookie client setName
        let card = cardInfos |> Seq.find (fun c -> c.Name = cardName)
        let! _ = Saver.deleteCard client card        
        printfn "Done."
        return ()
    }


let deleteSet (client: HttpClient) (cookie: string) (setName : string) : unit Task =
    task {
        printfn "Deleting %s..." setName
        let! cardInfos = Scraper.getSetCardInfos cookie client setName
        let! _ = Saver.deleteCards cardInfos client            
        printfn "Done."
        return ()
    }

let cloneCard (client: HttpClient) (cookie: string) (setName: string) (cardName: string) (newSetName: string) : unit Task =
    task {
        printfn "Cloning %s from %s to %s..." cardName setName newSetName
        let! cardInfos = Scraper.getSetCardInfos cookie client setName
        let card = cardInfos |> Seq.find (fun c -> c.Name = cardName)
        let! details = Scraper.getCardDetails cookie client card
        let details = Processor.processCard details
        let details = { details with Set = newSetName }
        let! _ = Saver.saveCards client Saver.SaverMode.Create [details]
        printfn "Done."
        return()
    }