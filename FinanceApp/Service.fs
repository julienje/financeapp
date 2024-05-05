module FinanceApp.Service

open System
open System.Threading.Tasks
open FinanceApp.DbType
open FinanceApp.DomainType

type AllAccount = GetAllDbAccount -> Task<Account seq>
type OpenAnAccount = GetDbAccountByNameAndCompany -> OpenDbAccount -> OpenAccount -> Task<Result<Account, string>>
type CloseAnAccount = CloseDbAccount -> CloseAccount -> Task<Result<Account, string>>

type AddAnAccountBalance =
    GetDbAccount -> AddDbBalanceAccount -> AddAccountBalance -> Task<Result<AccountBalance, string>>

type ActualWealth = GetActiveDbAccount -> GetLastBalanceAccount -> ExportDate -> Task<Wealth>

type AllBalanceForAnAccount =
    GetDbAccount -> GetAllDbBalancesForAnAccount -> AccountId -> Task<Result<AccountBalance seq, string>>

type DeleteBalance = DeleteDbBalance -> AccountBalanceId -> Task<Boolean>

type GetTrend = GetAllDbAccount -> GetAllDbBalances -> Task<Trend seq>

type AllInvestmentCompany = GetAllInvestmentDbCompany -> Task<CompanyName seq>
type AddAnInvestment = GetAllInvestmentDbCompany -> AddDbInvestment -> AddInvestment -> Task<Result<Investment, string>>
type GetProfit = GetAllDbInvestment -> GetActiveDbAccount -> GetLastBalanceAccount -> ProfitDate -> Task<Profit>

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
                |> Seq.sortDescending
                |> Seq.distinctBy (fun x -> x.Year.ToString() + "." + x.Month.ToString())
                |> Seq.sort

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
                                |> Seq.sortBy (_.CheckDate)
                                |> Seq.tryFindBack (fun balance -> balance.CheckDate <= targetDate)
                                |> Option.map (_.Amount))
                            |> Option.flatten
                            |> Option.defaultValue ChfMoney.Zero)
                        |> Seq.sum

                    let myDate = targetDate.ToString() |> TrendDate.create |> failOnError
                    { Amount = sum; Date = myDate })
        }

let handleGetAllAccountAsync: AllAccount =
    fun getAllDbAccount -> task { return! getAllDbAccount () }

let handleOpenAccountAsync: OpenAnAccount =
    fun getDbAccount openDbAccount input ->
        task {
            let! accounts = getDbAccount input.Name input.Company

            match Seq.isEmpty accounts with
            | false -> return Error "The account with this name and company already exists"
            | true ->
                let! newEntry = openDbAccount input
                return Ok newEntry
        }

let handleCloseAccountAsync: CloseAnAccount =
    fun closeDb input ->
        task {
            let! inserted = closeDb input

            match inserted with
            | None -> return Error "The account was not updatable"
            | Some value -> return Ok value
        }

let handleAddBalanceAsync: AddAnAccountBalance =
    fun getDbAccount addDbBalanceAccount input ->
        task {
            let! account = getDbAccount input.AccountId

            match account with
            | None -> return Error "The account doesn't exit"
            | Some _ ->
                let! newEntry = addDbBalanceAccount input
                return Ok newEntry
        }

let handleGetWealthAsync: ActualWealth =
    fun getActiveDbAccount getLastBalanceAccount exportDate ->
        task {
            let! accounts = getActiveDbAccount exportDate

            let accountsById = accounts |> Seq.map (fun a -> a.Id, a) |> Map.ofSeq

            let details =
                accounts
                |> Seq.map (_.Id)
                |> Seq.map (fun a -> getLastBalanceAccount a exportDate)
                |> Seq.map (_.Result) // TODO JJ Don't use Result in task
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
            let! account = getDbAccount accountId

            match account with
            | None -> return Error "The account doesn't exit"
            | Some _ ->
                let! forDb = getAllDbBalancesForAnAccount accountId
                return Ok forDb
        }

let handleDeleteBalanceAsync: DeleteBalance =
    fun deleteDbAccount balanceId ->
        task {
            let! deleted = deleteDbAccount balanceId
            return deleted > 0
        }

let handleGetInvestmentCompanyAsync: AllInvestmentCompany = fun db -> task { return! db () }

let handleAddInvestmentAsync: AddAnInvestment =
    fun getInvestmentCompany addDbInvestment addInvestment ->
        task {
            let! companies = getInvestmentCompany ()

            match companies |> Seq.contains addInvestment.Company with
            | false -> return Error "The company cannot be invested"
            | true ->
                let! investment = addDbInvestment addInvestment
                return Ok investment
        }

let handleGetInvestmentAsync: GetProfit =
    fun getInvestmentDb getActiveDbAccount getLastBalanceAccount profitDate ->
        task {
            let investmentDate = profitDate |> ProfitDate.value |> InvestmentDate.createFromDate
            let exportDate = profitDate |> ProfitDate.value |> ExportDate.createFromDate
            let! investment = getInvestmentDb investmentDate
            let! accounts = getActiveDbAccount exportDate

            let wealth =
                accounts
                |> Seq.filter (fun a -> a.Type <> Unknown)
                |> Seq.map (fun a -> (a, getLastBalanceAccount a.Id exportDate))
                |> Seq.map (fun (a, b) -> (a, b.Result)) //TODO JJ Use Result is not ok
                |> Seq.filter (fun (_, b) -> b.IsSome)
                |> Seq.map (fun (a, b) -> (a, b.Value))
                |> Seq.map (fun (a, b) ->
                    { Amount = b.Amount
                      CheckDate = b.CheckDate
                      Account = a })

            let totalInvestment = investment |> Seq.sumBy (_.Amount)
            let totalWealth = wealth |> Seq.sumBy (_.Amount)

            let profit =
                { Investment = totalInvestment
                  Wealth = totalWealth }

            let companyProfit =
                investment
                |> Seq.groupBy (_.Company)
                |> Seq.map (fun (c, i) ->
                    let wealth = wealth |> Seq.filter (fun a -> a.Account.Company = c)
                    let profitMoney =
                        { Investment = i |> Seq.sumBy (_.Amount)
                          Wealth = wealth |> Seq.sumBy (_.Amount) }
                    { CompanyName = c
                      Profit = profitMoney
                      Details = wealth })
            return
                { Profit = profit
                  Details = companyProfit
                  Date = profitDate }
        }
