module FinanceApp.MongoDb

open System
open FinanceApp.DbType
open MongoDB.Driver


let private mongo =
    let connectionString = Environment.GetEnvironmentVariable "CONNECTION_STRING"

    MongoClient connectionString

let private db = mongo.GetDatabase "financeDB"

let private accountCollection = db.GetCollection<AccountDb>("Accounts")

let private balanceCollection = db.GetCollection<BalanceAccountDb>("Balances")

let private handleNull element transform =
    match box element with
    | null -> None
    | _ -> Some (element |> transform)

let findAllAccountsAsync: GetAllDbAccount =
    fun () ->
        task {
            let! find = accountCollection.FindAsync(Builders.Filter.Empty)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toAccount
        }

let insertAccountAsync: OpenDbAccount =
    fun account ->
        task {
            let! _ = accountCollection.InsertOneAsync(account)
            return account |> AccountDb.toAccount
        }

let getAccountByNameAndCompanyAsync: GetDbAccountByNameAndCompany =
    fun name company ->
        task {
            let! find = accountCollection.FindAsync(fun a -> a.Name = name && a.Company = company)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toAccount
        }

let updateCloseDateAsync: CloseDbAccount =
    fun id date ->
        task {
            let idFilter = Builders<AccountDb>.Filter.Eq((fun a -> a._id), id)

            let nonClosedFilter =
                Builders<AccountDb>.Filter.Eq((fun a -> a.CloseDate), Nullable())

            let filter = Builders<AccountDb>.Filter.And(idFilter, nonClosedFilter)

            let update = Builders<AccountDb>.Update.Set((fun a -> a.CloseDate), Nullable date)

            let updateOption =
                FindOneAndUpdateOptions<AccountDb, AccountDb>(ReturnDocument = ReturnDocument.After)

            let! update = accountCollection.FindOneAndUpdateAsync<AccountDb>(filter, update, updateOption)
            return handleNull update AccountDb.toAccount
        }

let findAccountAsync: GetDbAccount =
    fun id ->
        task {
            let! find = accountCollection.FindAsync(fun a -> a._id = id)
            let! account = find.SingleOrDefaultAsync()
            return handleNull account AccountDb.toAccount
        }

let insertBalanceAsync: AddDbBalanceAccount =
    fun balance ->
        task {
            let! _ = balanceCollection.InsertOneAsync(balance)
            return balance |> BalanceAccountDb.toBalanceAccount
        }

let findActiveDbAccountAsync: GetActiveDbAccount =
    fun date ->
        task {
            let nonClosedFilter =
                Builders<AccountDb>.Filter.Eq((fun a -> a.CloseDate), Nullable())

            let closedButBefore =
                Builders<AccountDb>.Filter.Gte((fun a -> a.CloseDate), Nullable date)

            let closedFilter = Builders<AccountDb>.Filter.Or(nonClosedFilter, closedButBefore)

            let alreadyOpen = Builders<AccountDb>.Filter.Lte((fun a -> a.OpenDate), date)

            let filter = Builders<AccountDb>.Filter.And(alreadyOpen, closedFilter)

            let! find = accountCollection.FindAsync(filter)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toAccount
        }

let findLastBalanceAccountAsync: GetLastBalanceAccount =
    fun accountId date ->
        task {
            let before = Builders<BalanceAccountDb>.Filter.Lte((fun a -> a.CheckDate), date)

            let account =
                Builders<BalanceAccountDb>.Filter.Eq((fun a -> a.AccountId), accountId)

            let filter = Builders<BalanceAccountDb>.Filter.And(before, account)

            let sort = Builders<BalanceAccountDb>.Sort.Descending "CheckDate"

            let options = FindOptions<BalanceAccountDb>(Sort = sort)

            let! find = balanceCollection.FindAsync(filter, options)
            let! result = find.ToListAsync()

            let balancesList = result |> Seq.map BalanceAccountDb.toBalanceAccount

            return balancesList |> Seq.tryHead
        }

let findAllBalancesForAnAccountAsync: GetAllDbBalancesForAnAccount =
    fun accountId ->
        task {
            let sort = Builders<BalanceAccountDb>.Sort.Descending "CheckDate"
            let options = FindOptions<BalanceAccountDb>(Sort = sort)
            let! find = balanceCollection.FindAsync((fun a -> a.AccountId = accountId), options)
            let! result = find.ToListAsync()
            return result |> Seq.map BalanceAccountDb.toBalanceAccount
        }

let deleteBalanceAsync: DeleteDbBalance =
    fun balanceId ->
        task {
            let! find = balanceCollection.DeleteOneAsync(fun a -> a._id = balanceId)
            return find.DeletedCount
        }

let getAllBalancesAsync: GetAllDbBalances =
    fun () ->
        task {
            let! find = balanceCollection.FindAsync(Builders.Filter.Empty)
            let! result = find.ToListAsync()
            return result |> Seq.map BalanceAccountDb.toBalanceAccount
        }
