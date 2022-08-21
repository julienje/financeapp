namespace FinanceApp

open System.Threading.Tasks
open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson

module Service =

    type AllAccount = GetAllDbAccount -> Task<Account list>
    type OpenAnAccount = GetDbAccountByNameAndCompany -> OpenDbAccount -> OpenAccount -> Task<Result<Account, string>>
    type CloseAnAccount = CloseDbAccount -> CloseAccount -> Task<Result<Account, string>>

    type AddAnAccountBalance =
        GetDbAccount -> AddDbBalanceAccount -> AddAccountBalance -> Task<Result<AccountBalance, string>>

    type ActualWealth = GetActiveDbAccount -> GetLastBalanceAccount -> ExportDate -> Task<Wealth>

    let handleGetAllAccountAsync: AllAccount =
        fun getAllDbAccount ->
            task {
                let! accounts = getAllDbAccount ()
                return accounts |> List.map AccountDb.toAccount
            }

    let handleOpenAccountAsync: OpenAnAccount =
        fun getDbAccount openDbAccount input ->
            task {
                let accountName =
                    input.Name |> AccountName.value

                let inputCompany =
                    input.Company |> CompanyName.value

                let! accounts = getDbAccount accountName inputCompany

                match accounts.IsEmpty with
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
                let objId =
                    input.Id |> AccountId.value |> ObjectId.Parse

                let date =
                    input.CloseDate |> CloseDate.value

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
                let objId =
                    input.AccountId
                    |> AccountId.value
                    |> ObjectId.Parse

                let! account = getDbAccount objId

                match account with
                | None -> return Error "The account doesn't exit"
                | Some value ->
                    let forDb =
                        BalanceAccountDb.fromAddAccountBalance input

                    let! newEntry = addDbBalanceAccount forDb

                    let domain =
                        BalanceAccountDb.toBalanceAccount newEntry

                    return Ok domain
            }

    let handleGetWealthAsync: ActualWealth =
        fun getActiveDbAccount getLastBalanceAccount exportDate ->
            task {
                let date = exportDate |> ExportDate.value

                let! accounts = getActiveDbAccount date

                let details =
                    accounts
                    |> List.map (fun a -> getLastBalanceAccount a._id date)
                    |> List.map (fun t -> t.Result)
                    |> List.filter (fun o -> o.IsSome)
                    |> List.map (fun o -> o.Value)
                    |> List.map BalanceAccountDb.toBalanceAccount
                    |> List.map (fun b ->
                        { Amount = b.Amount
                          CheckDate = b.CheckDate
                          AccountId = b.AccountId })

                let total =
                    details
                    |> List.map (fun d -> d.Amount)
                    |> List.sum

                return
                    { Amount = total
                      Date = exportDate
                      Details = details }
            }
