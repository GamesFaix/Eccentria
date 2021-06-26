﻿// High-level commands for managing sets
module SetTools

open System.Net.Http
open FSharp.Control.Tasks
open System.Threading.Tasks
open Model

let autonumberSet (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        printfn "Auto-numbering %s..." setName
        let! cardDetails = MtgDesignReader.getSetCardDetails cookie client setName
        let processed = Processor.processCards cardDetails
        let! _ = MtgDesignWriter.saveCards client MtgDesignWriter.SaverMode.Edit processed
        printfn "Done."
        return ()
    }

let renameSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        printfn "Renaming %s to %s..." oldName newName
        let! cardDetails = MtgDesignReader.getSetCardDetails cookie client oldName
        let processed = Processor.processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = MtgDesignWriter.saveCards client MtgDesignWriter.SaverMode.Edit processed
        printfn "Done."
        return ()
    }

let cloneSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        printfn "Cloning %s to %s..." oldName newName
        let! cardDetails = MtgDesignReader.getSetCardDetails cookie client oldName
        let processed = Processor.processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = MtgDesignWriter.saveCards client MtgDesignWriter.SaverMode.Create processed
        printfn "Done."
        return ()
    }

let deleteCard (client: HttpClient) (cookie: string) (setName : string) (cardName: string) : unit Task =
    task {
        printfn "Deleting %s - %s..." setName cardName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let card = cardInfos |> Seq.find (fun c -> c.Name = cardName)
        let! _ = MtgDesignWriter.deleteCard client card        
        printfn "Done."
        return ()
    }


let deleteSet (client: HttpClient) (cookie: string) (setName : string) : unit Task =
    task {
        printfn "Deleting %s..." setName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let! _ = MtgDesignWriter.deleteCards cardInfos client            
        printfn "Done."
        return ()
    }

let cloneCard (client: HttpClient) (cookie: string) (setName: string) (cardName: string) (newSetName: string) : unit Task =
    task {
        printfn "Cloning %s from %s to %s..." cardName setName newSetName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let card = cardInfos |> Seq.find (fun c -> c.Name = cardName)
        let! details = MtgDesignReader.getCardDetails cookie client card
        let details = Processor.processCard details
        let details = { details with Set = newSetName }
        let! _ = MtgDesignWriter.saveCards client MtgDesignWriter.SaverMode.Create [details]
        printfn "Done."
        return()
    }

let private downloadCardImage (client: HttpClient) (card: CardInfo) : unit Task =
    task {
        printfn "Downloading %s..." card.Name
        let! bytes = MtgDesignReader.getCardImage client card
        let! _ = FileReaderWriter.saveCardImage bytes card
        return ()    
    }

let downloadSetImages (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        printfn "Downloading images for %s..." setName
        FileReaderWriter.deleteSetFolder setName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let! _ = cardInfos |> Utils.concurrentMap (downloadCardImage client)
        printfn "Done."
        return ()
    }

let createHtmlLayout (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        printfn "Creating HTML layout for %s..." setName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let html = Layouter.createHtmlLayout cardInfos
        let! _ = FileReaderWriter.saveHtmlLayout html setName
        printfn "Done."
        return ()
    }

let createPdfLayout (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        printfn "Creating PDF layout for %s..." setName
        let! cardInfos = MtgDesignReader.getSetCardInfos cookie client setName
        let html = Layouter.createHtmlLayout cardInfos
        let! bytes = Layouter.convertToPdf html 
        let! _ = FileReaderWriter.savePdfLayout bytes setName
        printfn "Done."       
        return ()
    }