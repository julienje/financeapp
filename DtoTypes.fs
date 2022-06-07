namespace FinanceApp

open System
open System.Text.Json.Serialization

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
          OpenDate: DateTime }

    [<JsonFSharpConverter>]
    type CloseAccountDto = { Id: string; CloseDate: DateTime }
