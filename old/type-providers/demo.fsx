open System
#load ".paket/load/net48/main.group.fsx"
// fsi.ShowDeclarationValues <- false;;

// JSON

open FSharp.Data

let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"
let [<Literal>] RecipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root

let printRecipe (recipe:Recipe) =
    recipe.Name |> printfn "%s"
    recipe.Rating |> Option.map string |> Option.defaultValue "no rating" |> printfn "%s"
    recipe.Author |> printfn "%s"
    recipe.Description |> printfn "%s"
    recipe.Ingredients.Length |> printfn "The recipe has %i ingredients"
    recipe.Ingredients |> Seq.iter (printfn "%s")
    recipe.Method.Length |> printfn "%i steps"
    printfn ""

let recipes = RecipesJson.Load(RecipesFile)

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

let rss = Dn.Load("https://www.dn.se/rss")

let printItem (item:Item) =
    item.Title |> printfn "%s"
    item.PubDate.ToString() |> printfn "%s"
    item.Description |> printfn "%s"
    printfn ""

rss.Channel.Items
|> Seq.take 3
|> Seq.iter printItem

rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("katt"))
|> function
| Some item -> printItem item
| None -> printfn "Not in the news today"


// HTML

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">

let html = ActiveLoginNuget.GetSample()

html.Html.Descendants() |> printfn "%A"

printfn "we depend on"
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

type GetAllChickensSql = SqlCommandProvider<
                            "
                            SELECT * FROM Chicken
                            ", ConnString>

type Chicken = GetAllChickensSql.Record

let printChicken (chicken:Chicken) =
    chicken.Name |> printfn "Hönan heter %s"
    chicken.Breed |> printfn "Rasen är %s"
    chicken.ImageUrl |> Option.defaultValue "ingen bild" |> printfn "Söt höna: %s"
    printfn ""

let getAllChickens() : Chicken list =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute()
    |> Seq.toList

getAllChickens()
|> Seq.take 3
|> Seq.iter printChicken

type GetTotalEggCountSql = SqlCommandProvider<"
                            SELECT c.Name, SUM(EggCount) AS Total
                            FROM Egg e
                            INNER JOIN Chicken c on c.Id = e.ChickenId
                            GROUP BY c.Name
                            ", ConnString>

type Total = GetTotalEggCountSql.Record

let getTotal() =
    use cmd = new GetTotalEggCountSql(ConnString)
    cmd.Execute()
    |> Seq.toList


let printTotal() =
    getTotal()
    |> Seq.iter (printfn "%A")
    printfn ""

printTotal()

type InsertEggsProc = SqlCommandProvider<
                            "
                            EXEC dbo.InsertEgg @eggs
                            ", ConnString>

type InsertEggsTVP = InsertEggsProc.InsertEggType

let insertEggs eggs =
    use cmd = new InsertEggsProc(ConnString)
    cmd.Execute eggs

let newEggs =
    getAllChickens()
    |> Seq.map (fun chicken ->
        InsertEggsTVP(chicken.Id, DateTime.Today, 1, DateTime.Now, DateTime.Now))

printfn "före"
printTotal()
insertEggs newEggs
printfn "efter"
printTotal()