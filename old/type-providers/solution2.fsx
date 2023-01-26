#load ".paket/load/net48/main.group.fsx"

open FSharp.Data

// JSON
let [<Literal>] Schema = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"
type RecipesJson = JsonProvider<Schema>
type Recipe = RecipesJson.Root

let printList index item  =
    printfn "\t%i. %s" (index + 1) item

let printRecipe (recipe: Recipe) =
    recipe.Name |> printfn "Name: %s"
    recipe.Ingredients.Length |> printfn "Number of ingredients: %i"
    recipe.Ingredients |> Seq.iteri printList
    recipe.Method.Length |> printfn "Number of steps to make: %i"
    recipe.Method |> Seq.iteri printList
    printfn ""

let full = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes : Recipe [] = RecipesJson.Load(full)

recipes
|> Seq.tryFind 
    (fun r -> 
        r.Ingredients |> Seq.exists (fun i -> i.ToLower().Contains "chestnut") 
        && 
        r.Method |> Seq.exists (fun m -> m.ToLower().Contains("grill")))

recipes
|> Seq.take 1
|> Seq.iter printRecipe


// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"
type Dn = XmlProvider<DnRss>
type Rss = Dn.Rss

let rss = Dn.Load("https://www.dn.se/rss")

let printNewsItem (item:Dn.Item) =
    printfn "\n%s - %s" (item.PubDate.ToString()) item.Title
    item.Description |> printfn "%s"
    printfn ""

rss.Channel.Items
|> Seq.take 5
|> Seq.iter printNewsItem

rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("viktor"))
|> function
| Some item -> printNewsItem item
| None -> printfn "\nNot in the news today. Yay!\n"

// HTML

type ActiveLoginNuget = HtmlProvider<"https://nuget.org/packages/ActiveLogin.Identity.Swedish">
let nuget = ActiveLoginNuget.GetSample()    


printfn "we depend on:"
nuget.Lists.Dependencies.Values |> Seq.iter (printfn "%s")

open XPlot.GoogleCharts
nuget.Tables.``Version History``.Rows
|> Seq.map (fun row -> row.Version, row.Downloads)
|> Seq.sortBy fst
|> Chart.Column
|> Chart.Show


// SQL

open FSharp.Data.SqlClient
let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken", ConnString>
type Chicken = GetAllChickensSql.Record

let getAllChickens() : Chicken list =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute()
    |> Seq.toList

let printChicken (chicken:Chicken) =
    chicken.Name |> printfn "%s"
    chicken.Breed |> printfn "%s"
    chicken.ImageUrl |> Option.defaultValue "no images" |> printfn "%s"

getAllChickens()
|> List.take 3
|> List.iter printChicken

type GetTotalEggCount = SqlCommandProvider<"SELECT ChickenId, Sum(EggCount) AS Total From Egg WHERE Date >= '2019-10-06' GROUP BY ChickenId", ConnString>
type TotalCount = GetTotalEggCount.Record

let getTotalCount() : TotalCount list =
    use cmd = new GetTotalEggCount(ConnString)
    cmd.Execute()
    |> Seq.toList

let printCount() =
    getTotalCount()
    |> printfn "%A"
    printfn ""

// type InsertEggProc = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
// type InsertEggTVP = InsertEggProc.InsertEggType

type Db = SqlProgrammabilityProvider<ConnString>
type InsertEggProc = Db.dbo.InsertEgg
type InsertEggTVP = Db.dbo.``User-Defined Table Types``.InsertEggType

let newEggs =
    getAllChickens()
    |> List.map (fun c -> InsertEggTVP(c.Id, System.DateTime.Today, 1, System.DateTime.Now, System.DateTime.Now))

let insertEggs = new InsertEggProc(ConnString)

printCount()
insertEggs.Execute newEggs
printCount()