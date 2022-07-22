namespace FinanceApp

open FinanceApp.DbType
open MongoDB.Driver

module MongoDb =
    let private mongo =
        MongoClient @"mongodb://localhost:27017"

    let private db =
        mongo.GetDatabase "financeDB"

    let private accountCollection =
        db.GetCollection<AccountDb>("Accounts")

    let findAllAsync: GetAllDbAccount =
        fun () ->
            task {
                let! find = accountCollection.FindAsync(Builders.Filter.Empty)
                let! accounts = find.ToListAsync()
                return accounts |> Seq.toList
            }

    let openAccountAsync: OpenDbAccount =
        fun account ->
            task {
                let! _ = accountCollection.InsertOneAsync(account)
                return account
            }

    let getAccountByNameAndCompanyAsync: GetDbAccountByNameAndCompany =
        fun name company ->
            task {
                let! find = accountCollection.FindAsync(fun a -> a.Name = name && a.Company = company)
                let! accounts = find.ToListAsync()
                return accounts |> Seq.toList
            }

    let updateCloseDateAsync: CloseDbAccount =
        fun id date ->
            task {
                let idFilter =
                    Builders<AccountDb>.Filter.Eq ((fun a -> a._id), id)

                let nonClosedFilter =
                    Builders<AccountDb>.Filter.Eq ((fun a -> a.CloseDate), null)

                let filter =
                    Builders<AccountDb>.Filter.And (idFilter, nonClosedFilter)

                let update =
                    Builders<AccountDb>.Update.Set ((fun a -> a.CloseDate), date)

                let updateOption =
                    FindOneAndUpdateOptions<AccountDb, AccountDb>(ReturnDocument = ReturnDocument.After)

                let! update = accountCollection.FindOneAndUpdateAsync<AccountDb>(filter, update, updateOption)

                let resp =
                    match box update with
                    | null -> None
                    | _ -> Some update

                return resp
            }
