namespace FinanceApp

open System
open FinanceApp.DbType
open FinanceApp.DomainType
open FinanceApp.DtoTypes
open MongoDB.Bson
open MongoDB.Driver

module Service =
    let private mongo =
        MongoClient @"mongodb://localhost:27017"

    let private db =
        mongo.GetDatabase "financeDB"

    let private collection =
        db.GetCollection<AccountDb>("Accounts")

    let convertToDto (input: Account) : AccountDto =
        { Id = input.Id
          Name = input.Name
          Company = input.Company
          OpenDate = input.OpenDate
          CloseDate = input.CloseDate }

    let convertToBd (input: OpenAccountDto) : AccountDb =
        { _id = ObjectId.Empty
          Name = input.Name
          Company = input.Company
          OpenDate = input.OpenDate.ToString()
          CloseDate = null }

    let convertToDomain (input: AccountDb) : Account =
        let closedDate =
            match DateTime.TryParse input.CloseDate with
            | true, value -> Some value
            | _ -> None

        { Account.Id = input._id.ToString()
          Name = input.Name
          Company = input.Company
          OpenDate = DateTime.Parse input.OpenDate
          CloseDate = closedDate }

    let handleGetAllAccountAsync () =
        task {
            let! accounts = MongoDb.findAllAsync collection

            return
                accounts
                |> List.map convertToDomain
                |> List.map convertToDto
        }

    let handleOpenAccountAsync input =
        task {
            let! accounts = MongoDb.getAccountByNameAndCompanyAsync collection input.Name input.Company

            match accounts.IsEmpty with
            | false -> return Error "The account with this name and company already exists"
            | true ->
                let forDb = convertToBd input
                let! newEntry = MongoDb.openAccountAsync collection forDb
                let domain = convertToDomain newEntry
                let dto = convertToDto domain
                return Ok dto
        }

    let handleCloseAccountAsync (input: CloseAccountDto) =
        task {
            let objId = ObjectId.Parse input.Id
            let date = input.CloseDate.ToString()
            let! inserted = MongoDb.updateCloseDateAsync collection objId date

            match inserted with
            | None -> return Error "The account was not updatable"
            | Some value ->
                let domain = convertToDomain value
                let dto = convertToDto domain
                return Ok dto
        }
