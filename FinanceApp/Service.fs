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

type GetTrend = GetAllDbAccount -> GetAllDbBalances -> Task<DatedAmount seq>

let private failOnError aResult =
    match aResult with
    | Ok success -> success
    | Error error -> failwithf $"%A{error}"

let handleGetTrendsAsync: GetTrend =
    fun getAllDbAccount getAllDbBalances ->
        task {
            let! accounts = getAllDbAccount ()
            let! balances = getAllDbBalances ()




            let balancesByAccount =
                balances
                |> Seq.map (fun x ->
                    {| AccountId = x.AccountId
                       CheckDate = CheckDate.value x.CheckDate
                       Amount = x.Amount |})
                |> Seq.groupBy (_.AccountId)
                |> Map.ofSeq

            let balanceWithCloseDate =
                accounts
                |> Seq.filter (fun x -> Option.isSome x.CloseDate)
                |> Seq.fold
                    (fun acc x ->
                        let id = x.Id
                        let balances = Map.tryFind id acc

                        let endFakeAccount =
                            {| AccountId = x.Id
                               CheckDate = CloseDate.value x.CloseDate.Value
                               Amount = ChfMoney.Zero |}

                        let temp = Seq.append balances.Value [ endFakeAccount ]
                        Map.add id temp acc)
                    balancesByAccount

            let months =
                balances
                |> Seq.map (fun x -> CheckDate.value x.CheckDate)
                |> Seq.sort
                |> Seq.distinctBy (fun x -> x.Year.ToString() + "." + x.Month.ToString())

            return
                months
                |> Seq.map (fun targetDate ->
                    let sum =
                        accounts
                        |> Seq.map (fun y ->
                            let accountId = y.Id

                            balanceWithCloseDate
                            |> Map.tryFind accountId
                            |> Option.map (fun x ->
                                x
                                |> Seq.sortBy (fun b -> b.CheckDate)
                                |> Seq.tryFindBack (fun balance -> balance.CheckDate <= targetDate)
                                |> Option.map (fun b -> b.Amount))
                            |> Option.flatten
                            |> Option.defaultValue ChfMoney.Zero)
                        |> Seq.sum

                    let myDate = targetDate.ToString() |> ExportDate.create |> failOnError
                    { Amount = sum; Date = myDate })
        }

let handleGetAllAccountAsync: AllAccount =
    fun getAllDbAccount -> task { return! getAllDbAccount () }

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
                return Ok newEntry
        }

let handleCloseAccountAsync: CloseAnAccount =
    fun closeDb input ->
        task {
            let objId = input.Id |> AccountId.value |> ObjectId.Parse

            let date = input.CloseDate |> CloseDate.value

            let! inserted = closeDb objId date

            match inserted with
            | None -> return Error "The account was not updatable"
            | Some value -> return Ok value
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

                return Ok newEntry
        }

let handleGetWealthAsync: ActualWealth =
    fun getActiveDbAccount getLastBalanceAccount exportDate ->
        task {
            let date = exportDate |> ExportDate.value

            let! accounts = getActiveDbAccount date

            let accountsById =
                accounts
                |> Seq.map (fun a -> a.Id, a)
                |> Map.ofSeq

            let details =
                accounts
                |> Seq.map (fun a-> a.Id |> AccountId.value|> ObjectId.Parse)
                |> Seq.map (fun a -> getLastBalanceAccount a date)
                |> Seq.map (_.Result)
                |> Seq.filter (_.IsSome)
                |> Seq.map (_.Value)
                |> Seq.map (fun b ->
                    { Amount = b.Amount
                      CheckDate = b.CheckDate
                      Account = accountsById[b.AccountId] })

            let total = details |> Seq.map (_.Amount) |> Seq.sum

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
                let! forDb = getAllDbBalancesForAnAccount objId
                return Ok forDb
        }

let handleDeleteBalanceAsync: DeleteBalance =
    fun deleteDbAccount balanceId ->
        task {
            let objId = balanceId |> AccountBalanceId.value |> ObjectId.Parse
            let! deleted = deleteDbAccount objId
            return deleted > 0
        }
