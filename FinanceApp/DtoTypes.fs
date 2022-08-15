namespace FinanceApp

open System
open System.Text.Json.Serialization
open FinanceApp.DomainType
open FsToolkit.ErrorHandling
open Microsoft.FSharp.Core

module DtoTypes =
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

    type WealthDto = { AmountInChf: decimal }


    module AccountDto =
        let fromDomain (input: Account) : AccountDto =
            { Id = input.Id |> AccountId.value
              Name = input.Name |> AccountName.value
              Company = input.Company |> CompanyName.value
              OpenDate = input.OpenDate |> OpenDate.value |> string
              CloseDate =
                input.CloseDate
                |> Option.map CloseDate.value
                |> Option.map string }

    module AccountBalanceDto =
        let fromDomain (input: AccountBalance) : AccountBalanceDto =
            { Id = input.Id |> AccountBalanceId.value
              AccountId = input.AccountId |> AccountId.value
              CheckDate = input.CheckDate |> CheckDate.value |> string
              AmountInChf = input.Amount |> ChfMoney.value |> decimal }

    module OpenAccountDto =

        let toDomain (input: OpenAccountDto) : Result<OpenAccount, String> =
            result {
                let! nameResult = AccountName.create input.Name
                let! companyResult = CompanyName.create input.Company
                let! openDateResult = OpenDate.createFromString input.OpenDate

                let domain =
                    OpenAccount.create nameResult companyResult openDateResult

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

    module WealthDto =
        let fromDomain (domain: Wealth) =
            { AmountInChf = domain.Amount |> ChfMoney.value |> decimal }
