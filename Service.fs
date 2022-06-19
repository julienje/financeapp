namespace FinanceApp

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
        { Id = input.Id |> AccountId.value
          Name = input.Name |> AccountName.value
          Company = input.Company |> CompanyName.value
          OpenDate = input.OpenDate |> OpenDate.value
          CloseDate = input.CloseDate |> Option.map CloseDate.value  }

    let convertToBd (input: OpenAccountDto) : AccountDb =
        { _id = ObjectId.Empty
          Name = input.Name
          Company = input.Company
          OpenDate = input.OpenDate.ToString()
          CloseDate = null }

    let handleGetAllAccountAsync () =
        task {
            let! accounts = MongoDb.findAllAsync collection

            return
                accounts
                |> List.map AccountDb.toAccount
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
                let domain = AccountDb.toAccount newEntry
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
                let domain = AccountDb.toAccount value
                let dto = convertToDto domain
                return Ok dto
        }
