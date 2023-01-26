#load ".paket/load/net48/main.group.fsx"

open FSharp.Data
open System.Data.SqlClient
open XPlot.GoogleCharts

[<Literal>]
let ConnectionString = "Data Source=.;Initial Catalog=ChickenCheck;User ID=sa;Password=hWfQm@s62[CJX9ypxRd8"

type GetAllChickensSql = SqlCommandProvider<"SELECT * FROM Chicken", ConnectionString>
type Chicken = GetAllChickensSql.Record

let getAllChickens() : Chicken list =
    use cmd = new GetAllChickensSql(ConnectionString)
    cmd.Execute() 
    |> Seq.toList

let printChicken (chicken: Chicken) =
    printfn "Hönan heter %s och är av rasen %s. Uppdaterades senast: %O" 
        chicken.Name 
        chicken.Breed 
        chicken.LastModified

type GetAllEggsSql = SqlCommandProvider<"SELECT * FROM Egg", ConnectionString>

let getAllEggs() =
    use cmd = new GetAllEggsSql(ConnectionString)
    cmd.Execute()
    |> Seq.toList

type GetChickensByDateSql = SqlCommandProvider<"SELECT * FROM Chicken Where Created > @date", ConnectionString>

let getChickensByDate() =
    use cmd = new GetChickensByDateSql(ConnectionString)
    cmd.Execute(System.DateTime.Today.AddDays(-70.))

// type InsertEggProc = SqlCommandProvider<"exec InsertEgg @input", ConnectionString> 
// type InsertEggTVP = InsertEggProc.InsertEggType

type DB = SqlProgrammabilityProvider<ConnectionString>
type InsertEggProc = DB.dbo.InsertEgg
type InsertEggTVP = DB.dbo.``User-Defined Table Types``.InsertEggType

let runWithTransaction (conn: SqlConnection) onError f =
    conn.Open()
    let tran = conn.BeginTransaction()
    try
        f conn tran
        tran.Commit()
    with exn -> 
        tran.Rollback()
        onError exn

let insertEggs eggs =
    use conn = new SqlConnection(ConnectionString)
    
    let insertEggsTran conn tran =
        failwith "boom no database"
        use cmd = new InsertEggProc(conn, transaction = tran)
        cmd.Execute eggs |> ignore

    runWithTransaction 
        conn 
        (printfn "Exception: %O") 
        insertEggsTran

// getAllEggs() |> Seq.length |> printfn "We start with %i eggs"

// let newEggs = 
//     getAllChickens()
//     |> List.map (fun c -> Inser|EggTVP(c.Id, System.DateTime.Today, 2, System.DateTime.Now, System.DateTime.Now))

// newEggs
// |> insertEggs

// getAllEggs() |> Seq.length |> printfn "After inserting we have %i eggs"

// **********

// json

let [<Literal>] RecipesSchema = __SOURCE_DIRECTORY__ + "/ChristmasRecipesSchema.json"
type RecipesJson = JsonProvider<RecipesSchema> 
type Recipes = RecipesJson.Root

let mostIngredients =
    // RecipesJson.Load "ChristmasRecipesFull.json" 
    let groupedByNumIngredients =
        RecipesJson.GetSamples()
        |> Array.groupBy (fun r -> r.Ingredients.Length)
    let maxNumIngredients =
        groupedByNumIngredients
        |> Array.map fst 
        |> Array.max

    printfn "Max number of ingredients: %i" maxNumIngredients

let [<Literal>] XmlSchema = __SOURCE_DIRECTORY__ + "/dn-rss.xml"

type DN = XmlProvider<XmlSchema>
type Item = DN.Item
let xml = DN.GetSample()

let printItem (item:Item) =
    printfn "The article called %s was written by %i writers, and has %s description\n" item.Title (item.Creators.Length) (if item.Description.IsSome then "a" else "no")

xml.Channel.Items |> Array.take 10 |> Array.map printItem

xml.Channel.Items |> Array.tryFind (fun i -> i.Title.Contains("Babblarna")) |> Option.iter printItem

//---

type Blog = XmlProvider<"https://viktorvan.github.io/feed.xml">
type Entry = Blog.Entry
let blog = Blog.GetSample()

blog.Entries |> Array.filter (fun e -> e.Title.Value.Contains("C#") |> not) |> Array.length |> printfn "%i articles are about C#"

// html

type ActiveLoginNuget = HtmlProvider<"https://www.nuget.org/packages/ActiveLogin.Identity.Swedish/">
let rawStats = ActiveLoginNuget().Tables.``Version History``

let parseVersion version = System.Text.RegularExpressions.Regex(@"\d.\d.\d").Match(version).Value

let stats = 
    rawStats.Rows
    // |> Seq.groupBy (fun row -> row.Version |> parseVersion)
    // |> Seq.map (fun (k,v) -> k, v |> Seq.sumBy (fun row -> row.Downloads))
    |> Seq.map (fun row -> row.Version, row.Downloads)

Chart.Column stats |> Chart.Show