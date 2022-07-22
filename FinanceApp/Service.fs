namespace FinanceApp

open System.Threading.Tasks
open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson

module Service =

    type AllAccount = GetAllDbAccount -> Task<Account list>
    type OpenAnAccount = GetDbAccountByNameAndCompany -> OpenDbAccount -> OpenAccount -> Task<Result<Account, string>>
    type CloseAnAccount = CloseDbAccount -> CloseAccount -> Task<Result<Account, string>>
    type AddAnAccountBalance = AddAccountBalance -> Task<Result<AccountBalance, string>>

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
                    input.CloseDate |> CloseDate.value |> string

                let! inserted = closeDb objId date

                match inserted with
                | None -> return Error "The account was not updatable"
                | Some value ->
                    let domain = AccountDb.toAccount value
                    return Ok domain
            }

    let handleAddBalanceAsync: AddAnAccountBalance =
        fun input -> task { return Error "asdf" }
