module ServiceTests

open System
open FinanceApp
open FinanceApp.DbType
open MongoDB.Bson
open Xunit

let accountAId = "3231fc4a-3297-4ae4-85d5-3163147e0e1c"
let accountBId ="00db8520-70b9-11ee-b962-0242ac120002"

let createAccount (id, name) =
    {
        _id= ObjectId.Parse(id)
        Name= name
        Company= "My Company"
        OpenDate= DateTime.MinValue
        CloseDate=  Nullable() }

let mockedAllAccounts: GetAllDbAccount =
 fun () ->
        task {
            let accountA = createAccount(accountAId, "A")
            let accountB = createAccount(accountBId, "B")
            return [accountA; accountB]
        }
let mockedAllBalances : GetAllDbBalances =
     fun () ->
        task {
            let balance= {
                  _id= ObjectId.Parse(Guid.NewGuid().ToString())
                  AccountId= ObjectId.Parse(accountAId)
                  CheckDate= DateTime.Now
                  AmountInChf= 12.0m
            }
            return [balance]
        }

[<Fact>]
let ``My test`` () =
    let resp = task {
        return! Service.handleGetTrendsAsync mockedAllAccounts mockedAllBalances
    }
    Assert.True(true)
    Assert.NotNull(resp)
