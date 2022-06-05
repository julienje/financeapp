namespace FinanceApp

open System

module DtoTypes =
    type AccountDto =
        { Id: string
          Name: string
          Company: string
          OpenDate: DateTime
          CloseDate: DateTime option }

    type OpenAccountDto =
        { Name: string
          Company: string
          OpenDate: DateTime }

    type CloseAccountDto = { Id: string; CloseDate: DateTime }
