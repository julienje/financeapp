namespace FinanceApp

open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson
open MongoDB.Driver

module Service =
    let private mongo =
        MongoClient @"mongodb://localhost:27017"

    let private db =
        mongo.GetDatabase "financeDB"

    let private collection =
        db.GetCollection<AccountDb>("Accounts")

    let handleGetAllAccountAsync () =
        task {
            let! accounts = MongoDb.findAllAsync collection

            return accounts |> List.map AccountDb.toAccount
        }

    let handleOpenAccountAsync (input: OpenAccount) =
        task {
            let accountName =
                input.Name |> AccountName.value

            let inputCompany =
                input.Company |> CompanyName.value

            let! accounts = MongoDb.getAccountByNameAndCompanyAsync collection accountName inputCompany

            match accounts.IsEmpty with
            | false -> return Error "The account with this name and company already exists"
            | true ->
                let forDb = AccountDb.fromOpenAccount input
                let! newEntry = MongoDb.openAccountAsync collection forDb
                let domain = AccountDb.toAccount newEntry
                return Ok domain
        }

    let handleCloseAccountAsync (input: CloseAccount) =
        task {
            let objId = input.Id |> AccountId.value |> ObjectId.Parse
            let date = (input.CloseDate |> CloseDate.value).ToString()
            let! inserted = MongoDb.updateCloseDateAsync collection objId date

            match inserted with
            | None -> return Error "The account was not updatable"
            | Some value ->
                let domain = AccountDb.toAccount value
                return Ok domain
        }
