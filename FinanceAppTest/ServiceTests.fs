module ServiceTests

open System
open FinanceApp
open FinanceApp.DbType
open MongoDB.Bson
open Xunit

let accountAId = ObjectId.GenerateNewId()
let accountBId = ObjectId.GenerateNewId()

let createAccount (id, name, closeDate) =
    { _id = id
      Name = name
      Company = "My Company"
      OpenDate = DateTime.MinValue
      CloseDate = closeDate }

let createBalance (accountId, datetime, amount) =
    { _id = ObjectId.GenerateNewId()
      AccountId = accountId
      CheckDate = datetime
      AmountInChf = amount }

let dateToDateTime (date) = DateTime.Parse(date + " 12:00:00")

let mockedAllAccounts: GetAllDbAccount =
    fun () ->
        task {
            let closeDate = dateToDateTime ("2023-06-01")
            let accountA = createAccount (accountAId, "A", Nullable())
            let accountB = createAccount (accountBId, "B", Nullable closeDate)
            return [ accountA; accountB ]
        }

let mockedAllBalances: GetAllDbBalances =
    fun () -> task { return [
        createBalance (accountAId, dateToDateTime ("2023-01-27"), 12.0M);
        createBalance (accountAId, dateToDateTime ("2023-02-27"), 12.0M);
        createBalance (accountAId, dateToDateTime ("2023-03-27"), 12.0M)
    ] }

[<Fact>]
let ``My test`` () =
    let respAsync = Service.handleGetTrendsAsync mockedAllAccounts mockedAllBalances
    let resp = respAsync.Wait()

    Assert.True(true)
    Assert.NotNull(respAsync)
