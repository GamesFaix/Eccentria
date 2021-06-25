module FileReaderWriter

open FSharp.Control.Tasks
open System.Threading.Tasks
open System.IO

let private createDirectoryIfMissing (path: string) : unit =
    if Directory.Exists path then ()
    else Directory.CreateDirectory path |> ignore

let saveFile (bytes: byte[]) (path: string): unit Task =
    task {
        createDirectoryIfMissing <| Path.GetDirectoryName path
        return! File.WriteAllBytesAsync(path, bytes)
    }