namespace FinanceApp

open System

module DomainType =
    type Account =
        { Id: string
          Name: string
          Company: string
          OpenDate: DateTime
          CloseDate: DateTime option }
