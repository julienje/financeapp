namespace FinanceApp

open System
open FinanceApp.DomainType
open MongoDB.Bson
open DomainType

module DbType =
    [<CLIMutable>]
    type AccountDb =
        { _id: ObjectId
          Name: string
          Company: string
          OpenDate: string
          CloseDate: string }
        
    module AccountDb =
        let failOnError aResult =
            match aResult with
            | Ok success -> success 
            | Error error -> failwithf $"%A{error}"
        let toAccount (accountDb: AccountDb) :Account =
           
            let accountName : AccountName = AccountName.create accountDb.Name |> failOnError
            
            let accountId : AccountId = accountDb._id.ToString() |> AccountId.create |> failOnError
            let companyName : CompanyName = CompanyName.create accountDb.Company |> failOnError
            let openDate : OpenDate = accountDb.OpenDate |> OpenDate.create  |> failOnError
            let closeDate: CloseDate option = accountDb.CloseDate |> CloseDate.createOption |> failOnError
            {
            Id = accountId
            Name = accountName
            Company = companyName
            OpenDate = openDate
            CloseDate = closeDate
            }
