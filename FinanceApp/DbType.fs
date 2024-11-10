module FinanceApp.DbType

open System
open System.Threading.Tasks
open FinanceApp.DomainType
open MongoDB.Bson

[<CLIMutable>]
type AccountDb =
    { _id: ObjectId
      Name: string
      Company: string
      OpenDate: DateTime
      Type: string
      CloseDate: Nullable<DateTime> }

[<CLIMutable>]
type BalanceAccountDb =
    { _id: ObjectId
      AccountId: ObjectId
      CheckDate: DateTime
      AmountInChf: decimal }

[<CLIMutable>]
type InvestmentDb =
    { _id: ObjectId
      CompanyName: string
      InvestmentDate: DateTime
      AmountInChf: decimal }

type GetAllDbAccount = Unit -> Task<Account seq>
type GetDbAccountByNameAndCompany = AccountName -> CompanyName -> Task<Account seq>
type OpenDbAccount = OpenAccount -> Task<Account>
type CloseDbAccount = CloseAccount -> Task<Account option>
type GetDbAccount = AccountId -> Task<Account option>
type AddDbBalanceAccount = AddAccountBalance -> Task<AccountBalance>
type GetActiveDbAccount = ExportDate -> Task<Account seq>
type GetLastBalanceAccount = AccountId -> ExportDate -> Task<AccountBalance option>
type GetAllDbBalancesForAnAccount = AccountId -> Task<AccountBalance seq>
type GetAllDbBalances = Unit -> Task<AccountBalance seq>
type DeleteDbBalance = AccountBalanceId -> Task<int64>
type GetAllInvestmentDbCompany = Unit -> Task<CompanyName seq>
type AddDbInvestment = AddInvestment -> Task<Investment>
type GetAllDbInvestment = InvestmentDate -> Task<Investment seq>
type GetAllDbInvestmentForACompany = CompanyName -> Task<Investment seq>
let private failOnError aResult =
    match aResult with
    | Ok success -> success
    | Error error -> failwithf $"%A{error}"

module AccountDb =
    let convertTypeFromDb (accountType: string) =
        match accountType with
        | null -> Unknown
        | "3A" -> ThirdPillarA
        | "ETF" -> ExchangeTradedFund
        | "AvailableToTrade" -> AvailableToTrade
        | _ -> Unknown

    let convertTypeFromDomain (accountType: AccountType) =
        match accountType with
        | ExchangeTradedFund -> "ETF"
        | ThirdPillarA -> "3A"
        | Unknown -> "Unknown"
        | AvailableToTrade -> "AvailableToTrade"

    let toAccount (accountDb: AccountDb) : Account =
        let accountName = AccountName.create accountDb.Name |> failOnError
        let accountId = accountDb._id.ToString() |> AccountId.create |> failOnError
        let companyName = CompanyName.create accountDb.Company |> failOnError
        let openDate = accountDb.OpenDate |> OpenDate.createFromDate
        let closeDate = accountDb.CloseDate |> CloseDate.createFromNullableDate
        let accountType = convertTypeFromDb accountDb.Type

        { Id = accountId
          Name = accountName
          Company = companyName
          OpenDate = openDate
          Type = accountType
          CloseDate = closeDate }

    let fromOpenAccount (openAccount: OpenAccount) : AccountDb =
        { _id = ObjectId.Empty
          Name = openAccount.Name |> AccountName.value
          Company = openAccount.Company |> CompanyName.value
          OpenDate = openAccount.OpenDate |> OpenDate.value
          Type = openAccount.Type |> convertTypeFromDomain
          CloseDate = Nullable() }

    let toCompanyName name : CompanyName =
        name |> CompanyName.create |> failOnError

module BalanceAccountDb =
    let fromAddAccountBalance (addAccountBalance: AddAccountBalance) : BalanceAccountDb =
        { _id = ObjectId.Empty
          AccountId = addAccountBalance.AccountId |> AccountId.value |> ObjectId.Parse
          CheckDate = addAccountBalance.CheckDate |> CheckDate.value
          AmountInChf = addAccountBalance.Amount |> ChfMoney.value |> decimal }

    let toBalanceAccount (balanceAccount: BalanceAccountDb) =
        let id = balanceAccount._id.ToString() |> AccountBalanceId.create |> failOnError

        let accountId =
            balanceAccount.AccountId.ToString() |> AccountId.create |> failOnError

        let checkDate = balanceAccount.CheckDate |> CheckDate.createFromDate

        let amount =
            (balanceAccount.AmountInChf * 1.0m<Chf>) |> ChfMoney.create |> failOnError

        { Id = id
          AccountId = accountId
          CheckDate = checkDate
          Amount = amount }

module InvestmentDb =
    let fromAddInvestment (addInvestment: AddInvestment) : InvestmentDb =
        { _id = ObjectId.Empty
          CompanyName = addInvestment.Company |> CompanyName.value
          InvestmentDate = addInvestment.Date |> InvestmentDate.value
          AmountInChf = addInvestment.Amount |> ChfMoney.value |> decimal }

    let toInvestment (db: InvestmentDb) : Investment =
        { Id = db._id.ToString() |> InvestmentId.create |> failOnError
          Amount = (db.AmountInChf * 1.0m<Chf>) |> ChfMoney.create |> failOnError
          Company = db.CompanyName |> CompanyName.create |> failOnError
          Date = db.InvestmentDate |> InvestmentDate.createFromDate }
