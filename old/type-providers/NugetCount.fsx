#r "nuget: FSharp.Data"
open FSharp.Data 

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">
let html = ActiveLoginNuget.GetSample()

html.Html.Descendants() |> printfn "%A"

printfn "we depend on"
html.Lists.Dependencies.Values 
|> Seq.iter (printfn "%s")

html.Tables.
|> printfn "%A"
// |> Seq.sortBy (fun row -> row.Version)
// |> Seq.map (fun row -> row.Version, row.Downloads)
// |> printfn "%A" 