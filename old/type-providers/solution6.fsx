#load ".paket/load/net48/main.group.fsx" 
// fsi.ShowDeclarationValues <- false

open System
open FSharp.Data

// JSON

let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json" 

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root

let RecipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes = RecipesJson.Load(RecipesFile)

let printRecipe (recipe:Recipe) =
    recipe.Name |> printfn "%s" 
    // recipe.Rating |> Option.map string |> Option.defaultValue "No rating" |> printfn "%s"
    recipe.Description |> printfn "%s"
    recipe.Author |> printfn "Made by: %s"
    recipe.Ingredients.Length |> printfn "Number of ingredients %i"
    recipe.Ingredients |> Seq.iter (printfn "%s")
    recipe.Method.Length |> printfn "Number of steps %i"
    printfn ""

recipes
|> Seq.take 2
|> Seq.iter printRecipe

recipes
|> Seq.minBy (fun recipe -> recipe.Ingredients.Length)
|> printRecipe

// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"

type Dn = XmlProvider<DnRss>
type Rss = Dn.Rss
type Item = Dn.Item

let printItem (item:Item) =
    printfn "%s - %s" (item.PubDate.ToString()) item.Title
    item.Description |> printfn "%s"
    printfn ""

let rss = Dn.Load("https://www.dn.se/rss")

rss.Channel.Items
|> Seq.take 2
|> Seq.iter printItem


rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("viktor"))
|> function
| Some item -> printItem item
| None -> printfn "Not in the news today."

// HTML

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish"> 

let html = ActiveLoginNuget.GetSample()

html.Html.Descendants() |> printfn "%A"

printfn "depends on"
html.Lists.Dependencies.Values
|> Seq.iter (printfn "%s")


open XPlot.GoogleCharts

html.Tables.``Version History``.Rows
|> Seq.sortBy (fun row -> row.Version)
|> Seq.map (fun row -> row.Version, row.Downloads)
|> Chart.Column
|> Chart.Show


// SQL
open FSharp.Data.SqlClient

let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

type GetAllChickens = SqlCommandProvider<"SELECT * FROM Chicken", ConnString>
type Chicken = GetAllChickens.Record


let printChicken (chicken:Chicken) =
    chicken.Name |> printfn "The chicken's name is %s"
    chicken.Breed |> printfn "The breed is %s"
    chicken.ImageUrl |> Option.defaultValue "no image" |> printfn "Image: %s"
    printfn ""

let getAllChickens() : Chicken list =
    use cmd = new GetAllChickens(ConnString)
    cmd.Execute()
    |> Seq.toList

getAllChickens()
|> Seq.take 3
|> Seq.iter printChicken

type GetTotalEggCount = SqlCommandProvider<"
                        SELECT c.Name, SUM(EggCount) AS Total
                        FROM Egg e
                        INNER JOIN Chicken c on c.Id = e.ChickenId
                        GROUP BY c.Name
                        ", ConnString>

type Total = GetTotalEggCount.Record

let getTotal() : Total list =
    use cmd = new GetTotalEggCount(ConnString)
    cmd.Execute()
    |> Seq.toList

let printTotal() =
    getTotal()
    |> Seq.iter (printfn "%A")
    printfn ""

type InsertEggsProc = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
type InsertEggsTVP = InsertEggsProc.InsertEggType

let newEggs =
    getAllChickens()
    |> Seq.map  
        (fun chicken ->
            InsertEggsTVP
                (
                    chicken.Id,
                    DateTime.Today,
                    1,
                    DateTime.Now,
                    DateTime.Now
                ))

let insertEggs eggs =
    use cmd = new InsertEggsProc(ConnString)
    cmd.Execute eggs


printfn "before"
printTotal()
newEggs |> insertEggs
printfn "after"
printTotal()
