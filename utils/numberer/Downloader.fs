module Downloader

open Model
open FSharp.Control.Tasks
open System.Threading.Tasks
open System.Net.Http
open System.IO

let private createDirectoryIfMissing (path: string) : unit =
    if Directory.Exists path then ()
    else Directory.CreateDirectory path |> ignore

let private saveFile (bytes: byte[]) (path: string): unit Task =
    task {
        createDirectoryIfMissing <| Path.GetDirectoryName path
        return! File.WriteAllBytesAsync(path, bytes)
    }

let downloadCardImage (client: HttpClient) (card: CardInfo) (outputPath: string) : unit Task =
    task {
        let url = sprintf "https://mtg.design/i/%s.jpg" card.Id
        let! response = client.GetAsync url
        let! bytes = response.Content.ReadAsByteArrayAsync()
        let! _ = saveFile bytes outputPath
        return ()
    }