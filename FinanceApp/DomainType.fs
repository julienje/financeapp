namespace FinanceApp

open System
open Microsoft.FSharp.Core

module DomainType =
    [<Measure>]
    type Chf

    type AccountId = private AccountId of string
    type AccountBalanceId = private AccountBalanceId of string
    type AccountName = private AccountName of string
    type CompanyName = private CompanyName of string
    type OpenDate = private OpenDate of DateTime
    type CloseDate = private CloseDate of DateTime

    type ChfMoney =
        private
        | ChfMoney of decimal<Chf>
        static member (+)(left: ChfMoney, right: ChfMoney) = left + right
        static member Zero = ChfMoney 0m<Chf>

    type CheckDate = private CheckDate of DateTime
    type ExportDate = private ExportDate of DateTime

    type Account =
        { Id: AccountId
          Name: AccountName
          Company: CompanyName
          OpenDate: OpenDate
          CloseDate: CloseDate option }

    type OpenAccount =
        { Name: AccountName
          Company: CompanyName
          OpenDate: OpenDate }

    type CloseAccount = { Id: AccountId; CloseDate: CloseDate }

    type AddAccountBalance =
        { AccountId: AccountId
          CheckDate: CheckDate
          Amount: ChfMoney }

    type AccountBalance =
        { Id: AccountBalanceId
          AccountId: AccountId
          CheckDate: CheckDate
          Amount: ChfMoney }

    type WealthAccount =
        { Amount: ChfMoney
          AccountId: AccountId
          CheckDate: CheckDate }

    type Wealth =
        { Amount: ChfMoney
          Date: ExportDate
          Details: WealthAccount list }

    module ConstrainedType =
        let createString fieldName ctor str =
            if String.IsNullOrEmpty(str) then
                Error($"%s{fieldName} must not be null or empty")
            else
                Ok(ctor str)

        let createDate fieldName ctor (date: string) =
            let resp =
                match DateTime.TryParse date with
                | true, value -> Ok(ctor value)
                | _ -> Error($"%s{fieldName} must be a valid datetime")

            resp

        let createDateOption fieldName ctor date =
            if String.IsNullOrEmpty(date) then
                Ok None
            else
                let resp =
                    match DateTime.TryParse date with
                    | true, value -> Ok(ctor value |> Some)
                    | _ -> Error($"%s{fieldName} must be a valid datetime")

                resp

        let createDecimal fieldName ctor amount = Ok(ctor amount)

    module AccountId =
        let value (AccountId str) = str

        let create str =
            ConstrainedType.createString "AccountId" AccountId str

    module AccountName =
        let value (AccountName str) = str

        let create str =
            ConstrainedType.createString "AccountName" AccountName str

    module CompanyName =
        let value (CompanyName str) = str

        let create str =
            ConstrainedType.createString "CompanyName" CompanyName str

    module OpenDate =
        let value (OpenDate date) = date

        let create date =
            ConstrainedType.createDate "OpenDate" OpenDate date

    module CloseDate =
        let value (CloseDate date) = date

        let createOption date =
            ConstrainedType.createDateOption "CloseDate" CloseDate date

        let create date =
            ConstrainedType.createDate "CloseDate" CloseDate date

    module OpenAccount =
        let create accountName companyName openDate =
            { Name = accountName
              Company = companyName
              OpenDate = openDate }

    module CloseAccount =
        let create accountId closeDate =
            { Id = accountId
              CloseDate = closeDate }

    module AddAccountBalance =
        let create accountId checkDate chfMoney : AddAccountBalance =
            { AccountId = accountId
              CheckDate = checkDate
              Amount = chfMoney }

    module CheckDate =
        let value (CheckDate date) = date

        let create date =
            ConstrainedType.createDate "CheckDate" CheckDate date

    module ChfMoney =
        let value (ChfMoney amount) = amount

        let create amount =
            ConstrainedType.createDecimal "ChfMoney" ChfMoney amount


    module AccountBalanceId =
        let value (AccountBalanceId str) = str

        let create str =
            ConstrainedType.createString "AccountBalanceId" AccountBalanceId str

    module ExportDate =
        let value (ExportDate date) = date

        let create date =
            ConstrainedType.createDate "ExportDate" ExportDate date

        let now = ExportDate DateTime.Now
