open System

open FSharp.Data

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">

let rawStats = ActiveLoginNuget().Tables.``Version History``

// helper function to analyze version numbers from nuget
let getMinorVersion (v:string) =  
  System.Text.RegularExpressions.Regex(@"\d.\d").Match(v).Value

// group by minor version and calculate download count
let stats = 
  rawStats.Rows
  |> printfn "%A"
//  |> Seq.groupBy (fun r -> 
//      getMinorVersion r.Version)
//  |> Seq.map (fun (k, xs) -> 
//      k, xs |> Seq.sumBy (fun x -> x.Downloads))
//  |> Seq.iter (printfn "%A")
  
//html.Tables.``Version History``.Rows
//|> Array.iter (printfn "%A")
// |> Seq.sortBy (fun row -> row.Version)
// |> Seq.map (fun row -> row.Version, row.Downloads)
