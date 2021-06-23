// High-level commands for managing sets
module SetTools

open System.Net.Http
open Scraper
open Processor
open Saver
open FSharp.Control.Tasks
open System.Threading.Tasks

let autonumberSet (client: HttpClient) (cookie: string) (setName: string) : unit Task =
    task {
        let! cardDetails = getSetCards setName cookie client

        let processed = processCards cardDetails

        let! _ = saveCards processed SaverMode.Edit client
            
        printfn "Done."
        return ()
    }

let renameSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        let! cardDetails = getSetCards oldName cookie client

        let processed = processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = saveCards processed SaverMode.Edit client
        
        printfn "Done."
        return ()
    }

let cloneSet (client: HttpClient) (cookie: string) (oldName : string) (newName: string) : unit Task =
    task {
        let! cardDetails = getSetCards oldName cookie client

        let processed = processCards cardDetails |> List.map (fun c -> { c with Set = newName })
        let! _ = saveCards processed SaverMode.Create client
        
        printfn "Done."
        return ()
    }

let deleteSet (client: HttpClient) (cookie: string) (setName : string) : unit Task =
    task {
        let! cardDetails = getSetCards setName cookie client    
        let! _ = deleteCards cardDetails client            
        printfn "Done."
        return ()
    }
