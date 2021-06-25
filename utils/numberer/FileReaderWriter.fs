﻿module FileReaderWriter

open FSharp.Control.Tasks
open System.Threading.Tasks
open System.IO
open System
open Model

let private rootDir = 
    let desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
    sprintf "%s/card-images" desktop

let private createDirectoryIfMissing (path: string) : unit =
    if Directory.Exists path then ()
    else Directory.CreateDirectory path |> ignore

let private saveFileBytes (bytes: byte[]) (path: string): unit Task =
    task {
        createDirectoryIfMissing <| Path.GetDirectoryName path
        return! File.WriteAllBytesAsync(path, bytes)
    }

let private saveFileText (text: string) (path: string): unit Task =
    task {    
        createDirectoryIfMissing <| Path.GetDirectoryName path
        return! File.WriteAllTextAsync(path, text)
    }
    
let private getSetDir (setName: string) : string =
    sprintf "%s/%s" rootDir setName

let private getCardImagePath (card: CardInfo) : string =
    sprintf "%s/%s.jpg" (getSetDir card.Set) (card.Name.Replace(" ", "-"))

let saveCardImage (bytes: byte[]) (card: CardInfo) : unit Task =
    saveFileBytes bytes (getCardImagePath card)

let saveHtmlLayout (html: string) (setName: string) : unit Task =
    saveFileText html (getSetDir setName)

