namespace FinanceApp

open System

module DomainType =
    type AccountId = private AccountId of string
    type AccountName = private AccountName of string
    type CompanyName = private CompanyName of string
    type OpenDate = private OpenDate of DateTime
    type CloseDate = private CloseDate of DateTime
    type Account =
        { Id: AccountId
          Name: AccountName
          Company: CompanyName
          OpenDate: OpenDate
          CloseDate: CloseDate option }
    module ConstrainedType =
        let createString fieldName ctor str =
            if String.IsNullOrEmpty(str) then
                Error ($"%s{fieldName} must not be null or empty")
            else
                Ok(ctor str)
        let createDate fieldName ctor (date:string) =
            let resp=
                match DateTime.TryParse date with
                | true, value -> Ok(ctor value)
                | _ -> Error ($"%s{fieldName} must be a valid datetime")
            resp
        let createDateOption fieldName ctor (date:string)=
            if String.IsNullOrEmpty(date) then
               Ok None
            else
                let resp=
                    match DateTime.TryParse date with
                    | true, value -> Ok(ctor value |> Some)
                    | _ -> Error ($"%s{fieldName} must be a valid datetime")
                resp
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
