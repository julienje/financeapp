module FinanceApp.mongodb.MongoDB

open MongoDB.Driver
open System

let readAll =
    let connectionString = @"mongodb://localhost:27017"
    let client = new MongoClient(connectionString)
    let database = client.GetDatabase("financeDB")
    use names = client.ListDatabaseNames()
    while names.MoveNext() do
        let join = String.Join(" ", names.Current)
        printfn $"Name is %s{join}"
    let dbs = client.ListDatabaseNames
    2
