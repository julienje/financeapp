module ServiceTests

open System
open FinanceApp
open FinanceApp.DbType
open FinanceApp.DomainType
open MongoDB.Bson
open Xunit

let accountAId = "000000000000000000000001"
let accountBId = "000000000000000000000002"

let AmountA = 12.5M<Chf>
let AmountB = 27.52M<Chf>
let AmountC = 57.67M<Chf>

let createAccount (id, name, closeDate) =
    { _id = ObjectId.Parse(id)
      Name = name
      Company = "My Company"
      OpenDate = DateTime.MinValue
      CloseDate = closeDate }

let createBalance (accountId, datetime, amount) =
    { _id = ObjectId.GenerateNewId()
      AccountId =ObjectId.Parse(accountId)
      CheckDate = datetime
      AmountInChf = decimal amount }

let dateToDateTime date = DateTime.Parse(date + " 12:00:00")

let mockedAllAccounts: GetAllDbAccount =
    fun () ->
        task {
            let closeDate = dateToDateTime "2023-06-01"
            let accountA = createAccount (accountAId, "A", Nullable())
            let accountB = createAccount (accountBId, "B", Nullable closeDate)
            return [ accountA; accountB ]
        }

let mockedAllBalances: GetAllDbBalances =
    fun () ->
        task {
            return
                [ createBalance (accountAId, dateToDateTime "2023-01-27", AmountA)

                  createBalance (accountAId, dateToDateTime "2023-02-27", AmountA)
                  createBalance (accountBId, dateToDateTime "2023-02-27", AmountB)

                  createBalance (accountAId, dateToDateTime "2023-03-27", AmountA)
                  createBalance (accountBId, dateToDateTime "2023-03-27", AmountC)

                  createBalance (accountAId, dateToDateTime "2023-04-27", AmountC)
                  //Let Account B blank to take previous month

                  createBalance (accountAId, dateToDateTime "2023-05-27", AmountA)
                  createBalance (accountBId, dateToDateTime "2023-05-27", AmountA)

                  createBalance (accountAId, dateToDateTime "2023-06-27", AmountA)

                  createBalance (accountAId, dateToDateTime "2023-07-27", AmountC) ]
        }

[<Fact>]
let ``Get Trends works correctly`` () =
    let respAsync = Service.handleGetTrendsAsync mockedAllAccounts mockedAllBalances
    let resp = respAsync.Result
    Assert.Equal(dateToDateTime "2023-07-27", ExportDate.value resp.Current.Date)
    Assert.Equal(AmountC, ChfMoney.value resp.Current.Amount)
    Assert.Equal(6, resp.Differences.Length)
    let a = resp.Differences.Head
    let b = resp.Differences.Tail.Head
    let c= resp.Differences.Tail.Tail.Tail.Head
    let d= resp.Differences.Tail.Tail.Tail.Tail.Head
    let e= resp.Differences.Tail.Tail.Tail.Tail.Tail.Head
    let f= resp.Differences.Tail.Tail.Tail.Tail.Tail.Tail.Head
    Assert.
