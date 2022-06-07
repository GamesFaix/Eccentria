module MtgDesign

open FSharp.Control.Tasks
open System.Net.Http
open System.Text.RegularExpressions

let private client = new HttpClient()

let getCardNames (setCode: string) = task {
    printfn "Parsing card names..."
    let! html = client.GetStringAsync($"https://mtg.design/u/tautologist/{setCode}")
    let pattern = Regex "<li class=\"lazy\" id=\"(.*)\">"
    let matches = pattern.Matches html 

    return matches
        |> Seq.map (fun m -> m.Groups.[1].Value)
        |> Seq.toList
}