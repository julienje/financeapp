namespace FinanceApp

open System
open FinanceApp.DbType
open MongoDB.Driver

module MongoDb =
    let private mongo =
        MongoClient @"mongodb://localhost:27017"

    let private db =
        mongo.GetDatabase "financeDB"

    let private accountCollection =
        db.GetCollection<AccountDb>("Accounts")

    let private balanceCollection =
        db.GetCollection<BalanceAccountDb>("Balances")

    let private handleNull element =
        match box element with
        | null -> None
        | _ -> Some element

    let findAllAsync: GetAllDbAccount =
        fun () ->
            task {
                let! find = accountCollection.FindAsync(Builders.Filter.Empty)
                let! accounts = find.ToListAsync()
                return accounts |> Seq.toList
            }

    let insertAccountAsync: OpenDbAccount =
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
                    Builders<AccountDb>.Filter.Eq ((fun a -> a.CloseDate), Nullable())

                let filter =
                    Builders<AccountDb>.Filter.And (idFilter, nonClosedFilter)

                let update =
                    Builders<AccountDb>.Update.Set ((fun a -> a.CloseDate), Nullable date)

                let updateOption =
                    FindOneAndUpdateOptions<AccountDb, AccountDb>(ReturnDocument = ReturnDocument.After)

                let! update = accountCollection.FindOneAndUpdateAsync<AccountDb>(filter, update, updateOption)
                return handleNull update
            }

    let findAccountAsync: GetDbAccount =
        fun id ->
            task {
                let! find = accountCollection.FindAsync(fun a -> a._id = id)
                let! account = find.SingleAsync()
                return handleNull account
            }

    let insertBalanceAsync: AddDbBalanceAccount =
        fun balance ->
            task {
                let! _ = balanceCollection.InsertOneAsync(balance)
                return balance
            }

    let findActiveDbAccountAsync: GetActiveDbAccount =
        fun date ->
            task {
                let nonClosedFilter =
                    Builders<AccountDb>.Filter.Eq ((fun a -> a.CloseDate), Nullable())

                let closedButBefore =
                    Builders<AccountDb>.Filter.Gte ((fun a -> a.CloseDate), Nullable date)

                let closedFilter =
                    Builders<AccountDb>.Filter.Or (nonClosedFilter, closedButBefore)

                let alreadyOpen =
                    Builders<AccountDb>.Filter.Lte ((fun a -> a.OpenDate), date)

                let filter =
                    Builders<AccountDb>.Filter.And (alreadyOpen, closedFilter)

                let! find = accountCollection.FindAsync(filter)
                let! accounts = find.ToListAsync()
                return accounts |> Seq.toList
            }

    let findLastBalanceAccount: GetLastBalanceAccount =
        fun accountId date -> task { return None }
