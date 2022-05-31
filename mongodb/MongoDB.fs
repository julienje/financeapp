namespace FinanceApp

module MongoDB

open MongoDB.Bson
open MongoDB.Driver
open System

[<CLIMutable>]
type Account =
  {
    _id: ObjectId
    Name: string
    Company: String
  }

let readAll =
    let connectionString = @"mongodb://localhost:27017"
    let client = new MongoClient(connectionString)
    let database = client.GetDatabase("financeDB")
    let collection = database.GetCollection<Account>("Accounts")
    let listAsync = 
        collection
            .Find(fun _ -> true)
            .ToListAsync()
            .Result;
    use names = client.ListDatabaseNames()
