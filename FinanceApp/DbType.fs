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
          AmountInChf: decimal }

    type GetAllDbAccount = Unit -> Task<AccountDb list>
    type GetDbAccountByNameAndCompany = string -> string -> Task<AccountDb list>
    type OpenDbAccount = AccountDb -> Task<AccountDb>
    type CloseDbAccount = ObjectId -> string -> Task<AccountDb option>
    type GetDbAccount = ObjectId -> Task<AccountDb option>
    type AddDbBalanceAccount = BalanceAccountDb -> Task<BalanceAccountDb>
    type GetActiveDbAccount = string -> Task<AccountDb list>
    type GetLastBalanceAccount = ObjectId -> string -> Task<BalanceAccountDb option>

    let private failOnError aResult =
        match aResult with
        | Ok success -> success
        | Error error -> failwithf $"%A{error}"

    module AccountDb =
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

    module BalanceAccountDb =
        let fromAddAccountBalance (addAccountBalance: AddAccountBalance) : BalanceAccountDb =
            { _id = ObjectId.Empty
              AccountId =
                addAccountBalance.AccountId
                |> AccountId.value
                |> ObjectId.Parse
              CheckDate =
                addAccountBalance.CheckDate
                |> CheckDate.value
                |> string
              AmountInChf =
                addAccountBalance.Amount
                |> ChfMoney.value
                |> decimal }

        let toBalanceAccount (balanceAccount: BalanceAccountDb) =
            let id =
                balanceAccount._id.ToString()
                |> AccountBalanceId.create
                |> failOnError

            let accountId =
                balanceAccount.AccountId.ToString()
                |> AccountId.create
                |> failOnError

            let checkDate =
                balanceAccount.CheckDate
                |> CheckDate.create
                |> failOnError

            let amount =
                (balanceAccount.AmountInChf * 1.0m<Chf>)
                |> ChfMoney.create
                |> failOnError

            { Id = id
              AccountId = accountId
              CheckDate = checkDate
              Amount = amount }
