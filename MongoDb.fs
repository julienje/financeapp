namespace FinanceApp

open System.Threading.Tasks
open FinanceApp.DbType
open MongoDB.Driver

module MongoDb =

    let findAllAsync (collection: IMongoCollection<AccountDb>) =
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
            let! find = collection.FindAsync(fun a -> a.Name = name && a.Company = company)

            let! accounts = find.ToListAsync()
            return accounts |> Seq.toList
        }

    let updateCloseDateAsync (collection: IMongoCollection<AccountDb>) id date =
        task {
            let idFilter =
                Builders<AccountDb>.Filter.Eq ((fun a -> a._id), id)
            
            let nonClosedFilter=
                Builders<AccountDb>.Filter.Eq ((fun a -> a.CloseDate), null)
                
            let filter = Builders<AccountDb>.Filter.And(idFilter, nonClosedFilter)

            let update =
                Builders<AccountDb>.Update.Set ((fun a -> a.CloseDate), date)
                
            let updateOption = FindOneAndUpdateOptions<AccountDb,AccountDb>(ReturnDocument = ReturnDocument.After)
            
            let! update = collection.FindOneAndUpdateAsync<AccountDb>(filter, update,updateOption)

            let resp =
                match box update with
                | null -> None
                | _ -> Some update

            return resp
        }
