namespace FinanceApp.DtoTypes

open System
open System.Text.Json.Serialization
open FinanceApp.DomainType
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Core

[<JsonFSharpConverter>]
type AccountDto =
    { Id: string
      Name: string
      Company: string
      OpenDate: string
      CloseDate: string option }

[<JsonFSharpConverter>]
type OpenAccountDto =
    { Name: string
      Company: string
      OpenDate: string }

[<JsonFSharpConverter>]
type CloseAccountDto = { Id: string; CloseDate: string }

[<JsonFSharpConverter>]
type AddBalanceDto =
    { CheckDate: string
      AmountInChf: decimal }

[<JsonFSharpConverter>]
type AccountBalanceDto =
    { Id: string
      AccountId: string
      CheckDate: string
      AmountInChf: decimal }

[<JsonFSharpConverter>]
type WealthAccountDto =
    { AmountInChf: decimal
      AccountId: string
      AccountName: string
      AccountCompany: string
      CheckDate: string }

[<JsonFSharpConverter>]
type WealthDto =
    { AmountInChf: decimal
      ExportDate: string
      Details: WealthAccountDto seq }

[<JsonFSharpConverter>]
type TrendDto =
    { AmountInChf: decimal
      CheckDate: string }

[<JsonFSharpConverter>]
type CompanyDto =
    { Name: string }

module Utility =
    let convertDateTime (input: DateTime) : string = input.ToString("o")

module AccountDto =
    let fromDomain (input: Account) : AccountDto =
        { Id = input.Id |> AccountId.value
          Name = input.Name |> AccountName.value
          Company = input.Company |> CompanyName.value
          OpenDate = input.OpenDate |> OpenDate.value |> Utility.convertDateTime
          CloseDate =
            input.CloseDate
            |> Option.map CloseDate.value
            |> Option.map Utility.convertDateTime }

module AccountBalanceDto =
    let fromDomain (input: AccountBalance) : AccountBalanceDto =
        { Id = input.Id |> AccountBalanceId.value
          AccountId = input.AccountId |> AccountId.value
          CheckDate = input.CheckDate |> CheckDate.value |> Utility.convertDateTime
          AmountInChf = input.Amount |> ChfMoney.value |> decimal }

module OpenAccountDto =

    let toDomain (input: OpenAccountDto) : Result<OpenAccount, String> =
        result {
            let! nameResult = AccountName.create input.Name
            let! companyResult = CompanyName.create input.Company
            let! openDateResult = OpenDate.createFromString input.OpenDate

            let domain = OpenAccount.create nameResult companyResult openDateResult

            return domain
        }

module CloseAccountDto =
    let toDomain (input: CloseAccountDto) =
        result {
            let! id = AccountId.create input.Id
            let! closeDate = CloseDate.create input.CloseDate
            return CloseAccount.create id closeDate
        }

module AddBalanceDto =
    let toDomain accountId (input: AddBalanceDto) =
        result {
            let! accountId = AccountId.create accountId
            let! checkDate = CheckDate.createFromString input.CheckDate
            let! chfMoney = ChfMoney.create (input.AmountInChf * 1.0m<Chf>)
            return AddAccountBalance.create accountId checkDate chfMoney
        }

module WealthAccountDto =
    let fromDomain (domain: WealthAccount) : WealthAccountDto =
        { AmountInChf = domain.Amount |> ChfMoney.value |> decimal
          AccountId = domain.Account.Id |> AccountId.value
          AccountName = domain.Account.Name |> AccountName.value
          AccountCompany = domain.Account.Company |> CompanyName.value
          CheckDate = domain.CheckDate |> CheckDate.value |> Utility.convertDateTime }

module WealthDto =
    let fromDomain (domain: Wealth) =
        { AmountInChf = domain.Amount |> ChfMoney.value |> decimal
          ExportDate = domain.Date |> ExportDate.value |> Utility.convertDateTime
          Details = domain.Details |> Seq.map WealthAccountDto.fromDomain }

module TrendDto =
    let fromDomain (domain: Trend seq) =
        domain
        |> Seq.map (fun x ->
            { AmountInChf = x.Amount |> ChfMoney.value |> decimal
              CheckDate = x.Date |> TrendDate.value |> Utility.convertDateTime })
module CompanyDto=
    let fromDomain(domain: CompanyName seq)=
        domain |> Seq.map CompanyName.value
