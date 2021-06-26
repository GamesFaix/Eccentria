module FileReaderWriter

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
    
let getSetDir (setName: string) : string =
    sprintf "%s/%s" rootDir setName

let getCardFileName (card: CardInfo): string =
    sprintf "%s.jpg" (card.Name.Replace(" ", "-").Replace("?", "-"))
    
let getCardImagePath (card: CardInfo) : string =
    sprintf "%s/%s" (getSetDir card.Set) (getCardFileName card)

let getHtmlLayoutPath (setName: string) : string =
    sprintf "%s/layout.html" (getSetDir setName)

let saveCardImage (bytes: byte[]) (card: CardInfo) : unit Task =
    saveFileBytes bytes (getCardImagePath card)

let saveHtmlLayout (html: string) (setName: string) : unit Task =
    saveFileText html (getHtmlLayoutPath setName)

let private deleteFolderIfExists (path: string) : unit =
    if Directory.Exists path 
    then Directory.Delete(path, true)
    else ()

let deleteSetFolder (setName: string) : unit =
    deleteFolderIfExists (getSetDir setName)