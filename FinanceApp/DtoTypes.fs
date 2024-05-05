namespace FinanceApp.DtoTypes

open System
open System.Runtime.InteropServices.JavaScript
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
type CompanyDto = { Name: string }

[<JsonFSharpConverter>]
type AddInvestmentDto =
    { AmountInChf: decimal
      InvestmentDate: string }

[<JsonFSharpConverter>]
type InvestmentDto =
    { Id: string
      CompanyName: string
      AmountInChf: decimal
      InvestmentDate: string }

[<JsonFSharpConverter>]
type ProfitMoneyDto =
    { InvestmentInChf: decimal
      WealthInChf: decimal }

[<JsonFSharpConverter>]
type CompanyProfitDto =
    { Profit: ProfitMoneyDto
      Company: string
      Details: WealthAccountDto seq }

[<JsonFSharpConverter>]
type ProfitDto =
    { Profit: ProfitMoneyDto
      Details: CompanyProfitDto seq
      ProfitDate: string }

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

module CompanyDto =
    let fromDomain (domain: CompanyName seq) = domain |> Seq.map (fun x-> { Name = x |> CompanyName.value } )

module InvestmentDto =
    let toDomain companyName (input: AddInvestmentDto) =
        result {
            let! companyName = CompanyName.create companyName
            let! date = InvestmentDate.create input.InvestmentDate
            let! chfMoney = ChfMoney.create (input.AmountInChf * 1.0m<Chf>)

            return
                { Amount = chfMoney
                  Company = companyName
                  Date = date }
        }

    let fromDomain (investment: Investment) : InvestmentDto =
        { Id = investment.Id |> InvestmentId.value
          CompanyName = investment.Company |> CompanyName.value
          AmountInChf = investment.Amount |> ChfMoney.value |> decimal
          InvestmentDate = investment.Date |> InvestmentDate.value |> Utility.convertDateTime }

module ProfitDto =
    let convertProfitMoney (profitMoney: ProfitMoney) : ProfitMoneyDto =
        { InvestmentInChf = profitMoney.Investment |> ChfMoney.value |> decimal
          WealthInChf = profitMoney.Wealth |> ChfMoney.value |> decimal }

    let fromDomain (profit: Profit) : ProfitDto =
        let profitMoney = convertProfitMoney profit.Profit

        let companyDetails =
            profit.Details
            |> Seq.map (fun companyProfit ->
                let wealthAccount =
                    companyProfit.Details
                    |> Seq.map(WealthAccountDto.fromDomain)
                { Profit = companyProfit.Profit |> convertProfitMoney
                  Company = companyProfit.CompanyName |> CompanyName.value
                  Details = wealthAccount })

        { Profit = profitMoney
          Details = companyDetails
          ProfitDate = profit.Date |> ProfitDate.value |> Utility.convertDateTime }
