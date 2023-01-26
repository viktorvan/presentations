#load ".paket/load/net48/main.group.fsx"
// fsi.ShowDeclarationValues <- false

// JSON

open FSharp.Data

let [<Literal>] RecipesSchemaFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"

type RecipesJson = JsonProvider<RecipesSchemaFile>
type Recipe = RecipesJson.Root

let printRecipe (recipe:Recipe) =
    recipe.Name |> printfn "%s"
    recipe.Author |> printfn "By: %s"
    recipe.Description |> printfn "%s"
    recipe.Ingredients.Length |> printfn "Number of ingredients %i"
    recipe.Ingredients |> printfn "%A"
    recipe.Method.Length |> printfn "Number of steps %i"
    printfn ""

let RecipesFile = __SOURCE_DIRECTORY__ + "/ChristmasRecipesFull.json"
let recipes : Recipe [] = RecipesJson.Load(RecipesFile)

recipes
|> Seq.take 5
|> Seq.iter printRecipe

recipes
|> Seq.minBy (fun recipe -> recipe.Ingredients.Length)
|> printRecipe

// XML

let [<Literal>] DnRss = __SOURCE_DIRECTORY__ + "/dn-rss.xml"

type Dn = XmlProvider<DnRss>
type Rss = Dn.Rss
type Item = Dn.Item

let printNews (item: Item) =
    printfn "%s - %s" (item.PubDate.ToString()) item.Title
    item.Description |> printfn "%s"
    printfn ""

let rss = Dn.Load("https://www.dn.se/rss")

rss.Channel.Items
|> Seq.take 2
|> Seq.iter printNews

rss.Channel.Items
|> Seq.tryFind (fun item -> item.Description.ToLower().Contains("dn"))
|> function
| Some item -> printNews item
| None -> printfn "Not in the news today. Yay!!"


// HTMP

type ActiveLoginNuget = HtmlProvider<"https://www.nuget.org/packages/ActiveLogin.Identity.Swedish">

let html = ActiveLoginNuget.GetSample()

printfn "we depend on:"
html.Lists.Dependencies.Values |> Seq.iter (printfn "%s")

open XPlot.GoogleCharts
html.Tables.``Version History``.Rows
|> Seq.sortBy (fun row -> row.Version)
|> Seq.map (fun row -> row.Version, row.Downloads)
|> Chart.Column
|> Chart.Show

// SQL

open FSharp.Data.SqlClient

let [<Literal>] ConnString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"
type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken ", ConnString>
type Chicken = GetAllChickensSql.Record

let printChicken (chicken: Chicken) =
    chicken.Name |> printfn "The chicken is named %s"
    chicken.Breed |> printfn "The breed is %s"
    chicken.ImageUrl |> Option.defaultValue "No image :(" |> printfn "%s"

let getAllChickens() : Chicken list =
    use cmd = new GetAllChickensSql(ConnString)
    cmd.Execute()
    |> Seq.toList

getAllChickens()
|> List.take 3
|> List.iter printChicken

type GetTotalEggCount = SqlCommandProvider<"
                        SELECT c.Name, SUM(EggCount) AS Total
                        FROM Egg e
                        INNER JOIN Chicken c on c.Id = e.ChickenId
                        WHERE c.Name = @name
                        GROUP BY c.Name
                        ", ConnString, SingleRow=true>

type Total = GetTotalEggCount.Record

let getTotal name =
    use cmd = new GetTotalEggCount(ConnString)
    cmd.Execute name
    // |> Seq.toList

let printTotal name =  
    getTotal name
    // |> List.iter (printfn "%A")
    |> printfn "%A"
    printfn ""


// type InsertEggsProc = SqlCommandProvider<"EXEC dbo.InsertEgg @eggs", ConnString>
// type InsertEggTVP = InsertEggsProc.InsertEggType

type Db = SqlProgrammabilityProvider<ConnString>
type InsertEggsProc = Db.dbo.InsertEgg
type InsertEggTVP = Db.dbo.``User-Defined Table Types``.InsertEggType

let newEggs() =
    getAllChickens()
    |> Seq.map (fun chicken -> InsertEggTVP(chicken.Id, System.DateTime.Today, 1, System.DateTime.Now, System.DateTime.Now))

let insertEggs eggs =
    use cmd = new InsertEggsProc(ConnString)
    cmd.Execute eggs

printTotal()
newEggs()
|> insertEggs
printTotal()


