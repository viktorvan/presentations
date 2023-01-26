#load ".paket/load/net48/main.group.fsx"

open FSharp.Data

// JSON

let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root 

let RecipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes : Recipe [] = RecipesJson.Load(RecipesFile)

let printList list =
    list
    |> Seq.iter (fun item -> printfn "%s" item)

let printRecipe (recipe:Recipe) =
    recipe.Name |> printfn "%s" 
    recipe.Description |> printfn "%s"
    recipe.Ingredients.Length |> printfn "The recipe has %i ingredients" 
    recipe.Method.Length |> printfn "The recipe has %i steps"
    printfn ""

recipes
|> Seq.take 2
|> Seq.iter printRecipe

recipes
|> Seq.maxBy (fun r -> r.Method.Length)
|> printRecipe

// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"
type DnRss = XmlProvider<DnRss>
type Rss = DnRss.Rss
type Item = DnRss.Item

let rss = DnRss.GetSample()

let printNews (item: Item) =
    printfn "%s - %s" (item.PubDate.ToString()) item.Title
    item.Description |> printfn "%s"
    printfn ""

rss.Channel.Items
|> Seq.take 2
|> Seq.map printNews

rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("viktor"))
|> function
| Some item -> printNews item
| None -> printfn "Not in the news today. Yay!!"

// HTKML

type ActiveIdentityNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">

let active = ActiveIdentityNuget.GetSample()

active.Html.Descendants() |> printfn "%A"

printfn "we depend on"
active.Lists.Dependencies.Values |> Seq.iter (fun d -> printfn "%s" d)

open XPlot.GoogleCharts
active.Tables.``Version History``.Rows
|> Seq.map (fun row -> row.Version, row.Downloads)
|> Seq.sortBy fst
|> Chart.Column
|> Chart.Show

// SQL

open FSharp.Data.SqlClient
let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken WHERE LastModified > @date", ConnString>
type Chicken = GetAllChickensSql.Record

let getAllChickens() =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute(System.DateTime(2019,9,1))
    |> Seq.toList

let printChicken (chicken:Chicken) =
    chicken.Name |> printfn "%s"
    chicken.Breed |> printfn "%s"
    chicken.ImageUrl |> Option.defaultValue "No image :(" |> printfn "%s"

getAllChickens()
|> Seq.take 3
|> Seq.iter printChicken

type GetTotalEggCountSql = SqlCommandProvider<"SELECT ChickenId, Sum(EggCount) AS Total FROM Egg GROUP BY ChickenId", ConnString>
type Total = GetTotalEggCountSql.Record

let getTotals() =
    use cmd = new GetTotalEggCountSql(ConnString)
    cmd.Execute()
    |> Seq.toList

getTotals()

type InsertEggsProc = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
type InsertEggsTVP = InsertEggsProc.InsertEggType

let newEggs =
    getAllChickens()
    |> Seq.map 
        (fun c -> 
            InsertEggsTVP
                (
                    c.Id,
                    System.DateTime.Today,
                    1,
                    System.DateTime.Now,
                    System.DateTime.Now
                ))

let insertEggs = new InsertEggsProc(ConnString)

let printCount() =
    getTotals() |> printfn "%A"
    printfn ""

printCount()
newEggs |> insertEggs.Execute
printCount()