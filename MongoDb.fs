namespace FinanceApp

open FinanceApp.DbType
open MongoDB.Driver

module MongoDb =
    let findAsync (collection: IMongoCollection<AccountDb>) =
        task {
            let! accounts =
                collection
                    .Find(Builders.Filter.Empty)
                    .ToListAsync()

            return accounts |> Seq.toList
        }

    let openAccountAsync (collection: IMongoCollection<AccountDb>) account =
        task { return collection.InsertOneAsync(account) }
