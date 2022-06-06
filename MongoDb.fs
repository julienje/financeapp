namespace FinanceApp

open System.Threading.Tasks
open FinanceApp.DbType
open MongoDB.Driver

module MongoDb =

    let findAsync (collection: IMongoCollection<AccountDb>) =
        task {
            let! find = collection.FindAsync(Builders.Filter.Empty)
            let! accounts = find.ToListAsync()
            return accounts |> Seq.toList
        }

    let openAccountAsync (collection: IMongoCollection<AccountDb>) account : Task<AccountDb> =
        task {
            let! _ = collection.InsertOneAsync(account)
            return account
        }

    let getAccountByNameAndCompanyAsync (collection: IMongoCollection<AccountDb>) name company =
        task {
            let! find =
                collection.FindAsync (fun a ->
                    a.Name = name && a.Company = company)

            let! accounts = find.ToListAsync()
            return accounts |> Seq.toList
        }
