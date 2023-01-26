#load ".paket/load/net48/main.group.fsx"

open FSharp.Data

// JSON


let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root 

let recipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes : Recipe [] = RecipesJson.Load(recipesFile)

let printRecipe (recipe: Recipe) =
    printfn "Name: %s" recipe.Name
    printfn "Url: %s" recipe.Url
    printfn "By: %s" recipe.Author
    printfn "Ingredients: %A" recipe.Ingredients
    printfn "Method: %A" recipe.Method
    printfn ""
    printfn ""

recipes
|> Array.take 2
|> Array.map printRecipe

recipes
|> Array.maxBy (fun r -> r.Ingredients.Length)
|> (fun r -> printfn "The recipe %s has %i ingredients!!!" r.Name r.Ingredients.Length)

recipes
|> Array.filter (fun r -> r.Ingredients |> Array.exists (fun i -> i.ToLower().Contains("cinnamon")))
|> Array.length


// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"
type Dn = XmlProvider<DnRss>
type Item = Dn.Item
type Rss = Dn.Rss

let dn = Dn.Load("https://www.dn.se/rss")

let printItem (item:Item) =
    printfn "%s-%s" (item.PubDate.ToString()) item.Title
    printfn "CLICK HERE!!!: %s" item.Link
    printfn "%s" (item.Description)

dn.Channel.Items
|> Seq.map (fun item -> item.Title)
|> Seq.iter (printfn "%s")

dn.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("active"))
|> function
| Some item -> printItem item
| None -> printfn "inte i nyheterna idag :("


// HTML

type ActiveLoginHtml = HtmlProvider<"https://www.nuget.org/packages/ActiveLogin.Identity.Swedish/">

let active = ActiveLoginHtml.GetSample()

active.Lists.Dependencies.Values
|> Seq.iter (printfn "%s")

open XPlot.GoogleCharts
active.Tables.``Version History``.Rows
|> Seq.map (fun r -> r.Version, r.Downloads)
|> Chart.Column
|> Chart.Show


// SQL

open FSharp.Data.SqlClient

let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken WHERE Created > @date", ConnString>
type Chicken = GetAllChickensSql.Record

let getAllChickens() =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute(System.DateTime(2019,9,1))
    |> Seq.toList

let printChicken (chicken: Chicken) =
    printfn "The chicken is called %s" chicken.Name
    printfn "The breed is %s" chicken.Breed
    chicken.ImageUrl 
    |> Option.defaultValue "no image :("
    |> printfn "Here's a cute picture %s"
    printfn ""

getAllChickens()
|> Seq.take 3
|> Seq.iter printChicken

type GetTotalEggCountSql = SqlCommandProvider<"SELECT ChickenId, Sum(EggCount) AS Total FROM Egg GROUP BY ChickenId", ConnString>
type TotalCount = GetTotalEggCountSql.Record

let getTotalEggCount() =
    use cmd = new GetTotalEggCountSql(ConnString)
    cmd.Execute()
    |> Seq.toList

let printCount() =
    getTotalEggCount()
    |> printfn "%A"
    printfn ""

type InsertEggProc = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
type InsertEggTVP = InsertEggProc.InsertEggType

// type Db = SqlProgrammabilityProvider<ConnString>
// type InsertEggProc = Db.dbo.InsertEgg
// type InsertEggTVP = Db.dbo.``User-Defined Table Types``.InsertEggType

let insertEggs = new InsertEggProc(ConnString)

let newEggs : seq<InsertEggTVP> =
    getAllChickens()
    |> Seq.map 
        (fun c -> 
            InsertEggTVP(
                c.Id, 
                System.DateTime.Today, 
                1, 
                System.DateTime.Now, 
                System.DateTime.Now))


printCount()

newEggs
|> insertEggs.Execute

printCount()

