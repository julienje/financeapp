module FinanceApp.MongoDb

open System
open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson
open MongoDB.Driver


let private mongo =
    let connectionString = Environment.GetEnvironmentVariable "CONNECTION_STRING"

    MongoClient connectionString

let private db = mongo.GetDatabase "financeDB"

let private accountCollection = db.GetCollection<AccountDb>("Accounts")

let private balanceCollection = db.GetCollection<BalanceAccountDb>("Balances")
let private investmentCollection = db.GetCollection<InvestmentDb>("Investment")

let private handleNull element transform =
    match box element with
    | null -> None
    | _ -> Some(element |> transform)

let findAllAccountsAsync: GetAllDbAccount =
    fun () ->
        task {
            let! find = accountCollection.FindAsync(Builders.Filter.Empty)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toAccount
        }

let insertAccountAsync: OpenDbAccount =
    fun openAccount ->
        task {
            let account = openAccount |> AccountDb.fromOpenAccount
            let! _ = accountCollection.InsertOneAsync(account)
            return account |> AccountDb.toAccount
        }

let getAccountByNameAndCompanyAsync: GetDbAccountByNameAndCompany =
    fun accountName companyName ->
        task {
            let name = accountName |> AccountName.value
            let company = companyName |> CompanyName.value
            let! find = accountCollection.FindAsync(fun a -> a.Name = name && a.Company = company)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toAccount
        }

let updateCloseDateAsync: CloseDbAccount =
    fun closeAccount ->
        task {
            let id = closeAccount.Id |> AccountId.value |> ObjectId.Parse
            let date = closeAccount.CloseDate |> CloseDate.value
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
    fun accountId ->
        task {
            let id = accountId |> AccountId.value |> ObjectId.Parse
            let! find = accountCollection.FindAsync(fun a -> a._id = id)
            let! account = find.SingleOrDefaultAsync()
            return handleNull account AccountDb.toAccount
        }

let insertBalanceAsync: AddDbBalanceAccount =
    fun addAccountBalance ->
        task {
            let balance = addAccountBalance |> BalanceAccountDb.fromAddAccountBalance
            let! _ = balanceCollection.InsertOneAsync(balance)
            return balance |> BalanceAccountDb.toBalanceAccount
        }

let findActiveDbAccountAsync: GetActiveDbAccount =
    fun exportDate ->
        task {
            let date = exportDate |> ExportDate.value

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
    fun accountId exportDate ->
        task {
            let date = exportDate |> ExportDate.value
            let accountIdDb = accountId |> AccountId.value |> ObjectId.Parse
            let before = Builders<BalanceAccountDb>.Filter.Lte((fun a -> a.CheckDate), date)

            let account =
                Builders<BalanceAccountDb>.Filter.Eq((fun a -> a.AccountId), accountIdDb)

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
            let accountIdDb = accountId |> AccountId.value |> ObjectId.Parse
            let sort = Builders<BalanceAccountDb>.Sort.Descending "CheckDate"
            let options = FindOptions<BalanceAccountDb>(Sort = sort)
            let! find = balanceCollection.FindAsync((fun a -> a.AccountId = accountIdDb), options)
            let! result = find.ToListAsync()
            return result |> Seq.map BalanceAccountDb.toBalanceAccount
        }

let deleteBalanceAsync: DeleteDbBalance =
    fun accountBalanceId ->
        task {
            let balanceId = accountBalanceId |> AccountBalanceId.value |> ObjectId.Parse
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

let getAllCompanyAsync: GetAllInvestmentDbCompany =
    fun () ->
        task {
            let field =
                ExpressionFieldDefinition<AccountDb, string>(fun (a: AccountDb) -> a.Company)

            let filter = Builders<AccountDb>.Filter.In((fun a -> a.Type), [ "3A"; "ETF" ])
            let! find = accountCollection.DistinctAsync(field, filter)
            let! result = find.ToListAsync()
            return result |> Seq.map AccountDb.toCompanyName
        }

let insertInvestmentAsync: AddDbInvestment =
    fun addInvestment ->
        task {
            let investment = addInvestment |> InvestmentDb.fromAddInvestment
            let! _ = investmentCollection.InsertOneAsync(investment)
            return investment |> InvestmentDb.toInvestment
        }

let findAllInvestment: GetAllDbInvestment =
    fun investmentDate ->
        task {
            let date = investmentDate |> InvestmentDate.value
            let before = Builders<InvestmentDb>.Filter.Lte((fun a -> a.InvestmentDate), date)
            let! find = investmentCollection.FindAsync(before)
            let! result = find.ToListAsync()
            return result |> Seq.map InvestmentDb.toInvestment
        }
