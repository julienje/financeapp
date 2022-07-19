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
          OpenDate: DateTime
          CloseDate: DateTime option }

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

    module AccountDto =
        let fromDomain (input: Account) : AccountDto =
            { Id = input.Id |> AccountId.value
              Name = input.Name |> AccountName.value
              Company = input.Company |> CompanyName.value
              OpenDate = input.OpenDate |> OpenDate.value
              CloseDate = input.CloseDate |> Option.map CloseDate.value }

    module OpenAccountDto =

        let toDomain (input: OpenAccountDto) : Result<OpenAccount, String> =
            result {
                let! nameResult = AccountName.create input.Name
                let! companyResult = CompanyName.create input.Company
                let! openDateResult = OpenDate.create input.OpenDate

                let domain =
                    OpenAccount.create nameResult companyResult openDateResult

                return domain
            }

    module CloseAccountDto =
        let toDomain (input) =
            result {
                let! id = AccountId.create input.Id
                let! closeDate = CloseDate.create input.CloseDate
                return CloseAccount.create id closeDate
            }

    module AddBalanceDto =
        let toDomain accountId (input:AddBalanceDto) =
            result {
                let! accountId = AccountId.create accountId
                let! checkDate = CheckDate.create input.CheckDate
                let! chfMoney = ChfMoney.create (input.AmountInChf * 1.0m<Chf>)
                return AddAccountBalance.create accountId checkDate chfMoney
            }
