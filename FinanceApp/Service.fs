module FinanceApp.Service

open System
open System.Threading.Tasks
open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson

type AllAccount = GetAllDbAccount -> Task<Account seq>
type OpenAnAccount = GetDbAccountByNameAndCompany -> OpenDbAccount -> OpenAccount -> Task<Result<Account, string>>
type CloseAnAccount = CloseDbAccount -> CloseAccount -> Task<Result<Account, string>>

type AddAnAccountBalance =
    GetDbAccount -> AddDbBalanceAccount -> AddAccountBalance -> Task<Result<AccountBalance, string>>

type ActualWealth = GetActiveDbAccount -> GetLastBalanceAccount -> ExportDate -> Task<Wealth>

type AllBalanceForAnAccount =
    GetDbAccount -> GetAllDbBalancesForAnAccount -> AccountId -> Task<Result<AccountBalance seq, string>>

type DeleteBalance = DeleteDbBalance -> AccountBalanceId -> Task<Boolean>

type GetTrend = GetAllDbAccount -> GetAllDbBalances -> Task<Trend>

let formatMyDate (value: DateTime) =
    $"%d{value.Year}.%02d{value.Month}"

let handleGetTrendsAsync: GetTrend =
    fun getAllDbAccount getAllDbBalances ->
        task {
            let! accounts = getAllDbAccount ()
            let! balances = getAllDbBalances ()

            //For every account add the close date a 0 wealth
            //Get (or generate?) every moment that we want to check
            //For every moment and for every account sum the wealth

            let balancesByAccount =
                balances
                |> Seq.groupBy (fun x-> x.AccountId.ToString())
                |> Map.ofSeq

            accounts
            |> Seq.filter (fun x-> x.CloseDate.HasValue)
            |> Seq.iter (fun x-> balancesByAccount.TryGetValue x )



            let months = balances

            let balancesByMonth =
                balances
                |> Seq.groupBy (fun x -> x.CheckDate.Year.ToString() + "." + x.CheckDate.Month.ToString())
                |> Seq.sortBy fst

            let balancesPerAccount=
                balances
                |> Seq.groupBy (fun x -> x.AccountId)

            let tempCurrent =
                { Amount = ChfMoney.Zero
                  Date = ExportDate.now }

            return
                { Current = tempCurrent
                  Differences = [] }
        }

let handleGetAllAccountAsync: AllAccount =
    fun getAllDbAccount ->
        task {
            let! accounts = getAllDbAccount ()
            return accounts |> Seq.map AccountDb.toAccount
        }

let handleOpenAccountAsync: OpenAnAccount =
    fun getDbAccount openDbAccount input ->
        task {
            let accountName = input.Name |> AccountName.value

            let inputCompany = input.Company |> CompanyName.value

            let! accounts = getDbAccount accountName inputCompany

            match Seq.isEmpty accounts with
            | false -> return Error "The account with this name and company already exists"
            | true ->
                let forDb = AccountDb.fromOpenAccount input
                let! newEntry = openDbAccount forDb
                let domain = AccountDb.toAccount newEntry
                return Ok domain
        }

let handleCloseAccountAsync: CloseAnAccount =
    fun closeDb input ->
        task {
            let objId = input.Id |> AccountId.value |> ObjectId.Parse

            let date = input.CloseDate |> CloseDate.value

            let! inserted = closeDb objId date

            match inserted with
            | None -> return Error "The account was not updatable"
            | Some value ->
                let domain = AccountDb.toAccount value
                return Ok domain
        }

let handleAddBalanceAsync: AddAnAccountBalance =
    fun getDbAccount addDbBalanceAccount input ->
        task {
            let objId = input.AccountId |> AccountId.value |> ObjectId.Parse

            let! account = getDbAccount objId

            match account with
            | None -> return Error "The account doesn't exit"
            | Some _ ->
                let forDb = BalanceAccountDb.fromAddAccountBalance input

                let! newEntry = addDbBalanceAccount forDb

                let domain = BalanceAccountDb.toBalanceAccount newEntry

                return Ok domain
        }

let handleGetWealthAsync: ActualWealth =
    fun getActiveDbAccount getLastBalanceAccount exportDate ->
        task {
            let date = exportDate |> ExportDate.value

            let! accounts = getActiveDbAccount date

            let accountsById =
                accounts
                |> Seq.map AccountDb.toAccount
                |> Seq.map (fun a -> a.Id, a)
                |> Map.ofSeq

            let details =
                accounts
                |> Seq.map (fun a -> getLastBalanceAccount a._id date)
                |> Seq.map (fun t -> t.Result)
                |> Seq.filter (fun o -> o.IsSome)
                |> Seq.map (fun o -> o.Value)
                |> Seq.map BalanceAccountDb.toBalanceAccount
                |> Seq.map (fun b ->
                    { Amount = b.Amount
                      CheckDate = b.CheckDate
                      Account = accountsById[b.AccountId] })

            let total = details |> Seq.map (fun d -> d.Amount) |> Seq.sum

            return
                { Amount = total
                  Date = exportDate
                  Details = details }
        }

let handleGetAllBalanceForAnAccountAsync: AllBalanceForAnAccount =
    fun getDbAccount getAllDbBalancesForAnAccount accountId ->
        task {
            let objId = accountId |> AccountId.value |> ObjectId.Parse
            let! account = getDbAccount objId

            match account with
            | None -> return Error "The account doesn't exit"
            | Some value ->
                let! forDb = getAllDbBalancesForAnAccount value._id
                let domain = forDb |> Seq.map BalanceAccountDb.toBalanceAccount
                return Ok domain
        }

let handleDeleteBalanceAsync: DeleteBalance =
    fun deleteDbAccount balanceId ->
        task {
            let objId = balanceId |> AccountBalanceId.value |> ObjectId.Parse
            let! deleted = deleteDbAccount objId
            return deleted > 0
        }
