#load ".paket/load/net48/main.group.fsx"
// fsi.ShowDeclarationValues <- false

open FSharp.Data

// JSON

let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root 

let RecipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes : Recipe [] = RecipesJson.Load(RecipesFile) 

let printRecipe (recipe:Recipe) =
    recipe.Name |> printfn "%s"
    recipe.Description |> printfn "%s"
    // recipe.Rating |> Option.map string |> Option.defaultValue "N/A" |> printfn "Rating: %s"
    recipe.Author |> printfn "%s"
    recipe.Ingredients.Length |> printfn "The recipe has %i ingredients"
    recipe.Ingredients |> Seq.iter (printfn "%s")
    recipe.Method.Length |> printfn "The recipe has %i steps"
    printfn ""

recipes
|> Seq.take 2
|> Seq.iter printRecipe

recipes
|> Seq.minBy (fun recipe -> recipe.Ingredients.Length)
|> printRecipe

// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"
type Rss = XmlProvider<DnRss>
type Item = Rss.Item

let rss = Rss.Load(DnRss)

let printNews (item:Rss.Item) =
    printfn "%s - %s" (item.PubDate.ToString()) item.Title
    printfn "%s" item.Description
    printfn ""

rss.Channel.Items
|> Seq.take 2
|> Seq.iter printNews

rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("sas"))
|> function
| Some item -> printNews item
| None -> printfn "Not in the news today"


// HTML

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">
let html = ActiveLoginNuget.GetSample()
html.Html.Descendants() |> printfn "%A"

printfn "we depend on:"
html.Lists.Dependencies.Values |> Seq.iter (printfn "%s")

open XPlot.GoogleCharts
html.Tables.``Version History``.Rows
|> Seq.sortBy (fun row -> row.Version)
|> Seq.map (fun row -> row.Version, row.Downloads)
|> Chart.Column
|> Chart.Show

// SQL

let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

open FSharp.Data.SqlClient
type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken ", ConnString>
type Chicken = GetAllChickensSql.Record

let getAllChickens() =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute()
    |> Seq.toList

let printChicken (chicken: Chicken) =
    chicken.Name |> printfn "The chicken is called %s"
    chicken.Breed |> printfn "The breed is %s"
    chicken.ImageUrl |> Option.defaultValue "no image" |> printfn "%s"

getAllChickens()
|> Seq.take 3
|> Seq.iter printChicken

type GetTotalEggCountSql = SqlCommandProvider<
                            "
                            SELECT c.Name, SUM(EggCount) As Total
                            FROM Egg e
                            INNER JOIN Chicken c on c.Id = e.ChickenId
                            GROUP BY c.Name
                            ", ConnString>

type Total = GetTotalEggCountSql.Record

let getTotal() =
    use cmd = new GetTotalEggCountSql(ConnString)
    cmd.Execute()
    |> Seq.toList

let printCount() =
    getTotal()
    |> Seq.iter (printfn "%A")
    printfn ""

printCount()

type InsertEggs = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
type InsertEggTVP = InsertEggs.InsertEggType


let insertEggs eggs =
    use cmd = new InsertEggs(ConnString)
    cmd.Execute eggs

let newEggs =
    getAllChickens()
    |> Seq.map  
        (fun chicken -> 
            InsertEggTVP(chicken.Id, System.DateTime.Today, 1, System.DateTime.Now, System.DateTime.Now))

printCount()
newEggs |> insertEggs
printCount()