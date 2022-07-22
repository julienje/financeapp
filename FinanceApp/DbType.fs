namespace FinanceApp

open System.Threading.Tasks
open FinanceApp.DomainType
open MongoDB.Bson

module DbType =
    [<CLIMutable>]
    type AccountDb =
        { _id: ObjectId
          Name: string
          Company: string
          OpenDate: string
          CloseDate: string }

    [<CLIMutable>]
    type BalanceAccountDb =
        { _id: ObjectId
          AccountId: ObjectId
          CheckDate: string
          AmountInChf: string }

    type GetAllDbAccount = Unit -> Task<AccountDb list>
    type GetDbAccountByNameAndCompany = string -> string -> Task<AccountDb list>
    type OpenDbAccount = AccountDb -> Task<AccountDb>
    type CloseDbAccount = ObjectId -> string -> Task<AccountDb option>
    type GetDbAccount = ObjectId -> Task<AccountDb option>
    type AddDbBalanceAccount = BalanceAccountDb -> Task<BalanceAccountDb>

    module AccountDb =
        let private failOnError aResult =
            match aResult with
            | Ok success -> success
            | Error error -> failwithf $"%A{error}"

        let toAccount (accountDb: AccountDb) : Account =
            let accountName =
                AccountName.create accountDb.Name |> failOnError

            let accountId =
                accountDb._id.ToString()
                |> AccountId.create
                |> failOnError

            let companyName =
                CompanyName.create accountDb.Company
                |> failOnError

            let openDate =
                accountDb.OpenDate
                |> OpenDate.create
                |> failOnError

            let closeDate =
                accountDb.CloseDate
                |> CloseDate.createOption
                |> failOnError

            { Id = accountId
              Name = accountName
              Company = companyName
              OpenDate = openDate
              CloseDate = closeDate }

        let fromOpenAccount (openAccount: OpenAccount) : AccountDb =
            { _id = ObjectId.Empty
              Name = openAccount.Name |> AccountName.value
              Company = openAccount.Company |> CompanyName.value
              OpenDate =
                (openAccount.OpenDate |> OpenDate.value)
                    .ToString()
              CloseDate = null }
