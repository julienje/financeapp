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

let private failOnError aResult =
    match aResult with
    | Ok success -> success
    | Error error -> failwithf $"%A{error}"

let createAccount (id, name, closeDate) =
    { Id = id |> AccountId.create |> failOnError
      Name = name |> AccountName.create |> failOnError
      Company = "My Company" |> CompanyName.create |> failOnError
      OpenDate = DateTime.MinValue |> OpenDate.createFromDate
      CloseDate = closeDate |> CloseDate.createOption |>failOnError }

let createBalance (accountId, datetime, amount) =
    { Id = Guid.NewGuid().ToString() |> AccountBalanceId.create |> failOnError
      AccountId= accountId |> AccountId.create |> failOnError
      CheckDate= datetime |> CheckDate.createFromDate
      Amount= amount |> ChfMoney.create |> failOnError }

let dateToDateTime date = DateTime.Parse(date + " 12:00:00")

let mockedAllAccounts: GetAllDbAccount =
    fun () ->
        task {
            let closeDate = dateToDateTime "2023-06-01"
            let accountA = createAccount (accountAId, "A", "")
            let accountB = createAccount (accountBId, "B", "2023-06-01")
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

                  //Account B is closed
                  createBalance (accountAId, dateToDateTime "2023-06-27", AmountA)
                  createBalance (accountAId, dateToDateTime "2023-07-27", AmountC) ]
        }

[<Fact>]
let ``Get Trends works correctly`` () =
    let respAsync = Service.handleGetTrendsAsync mockedAllAccounts mockedAllBalances
    let resp = respAsync.Result
    Assert.Equal(7, Seq.length resp)
    let a = resp |> Seq.item 0
    let b = resp |> Seq.item 1
    let c= resp |> Seq.item 2
    let d= resp |> Seq.item 3
    let e= resp |> Seq.item 4
    let f= resp |> Seq.item 5
    let g= resp |> Seq.item 6
    Assert.Equal(dateToDateTime "2023-01-27",ExportDate.value a.Date)
    Assert.Equal(AmountA, ChfMoney.value a.Amount)
    Assert.Equal(dateToDateTime "2023-02-27",ExportDate.value b.Date)
    Assert.Equal(AmountA + AmountB, ChfMoney.value b.Amount)
    Assert.Equal(dateToDateTime "2023-03-27",ExportDate.value c.Date)
    Assert.Equal(AmountA+AmountC, ChfMoney.value c.Amount)
    Assert.Equal(dateToDateTime "2023-04-27",ExportDate.value d.Date)
    Assert.Equal(AmountC+AmountC, ChfMoney.value d.Amount)
    Assert.Equal(dateToDateTime "2023-05-27",ExportDate.value e.Date)
    Assert.Equal(AmountA+AmountA, ChfMoney.value e.Amount)
    Assert.Equal(dateToDateTime "2023-06-27",ExportDate.value f.Date)
    Assert.Equal(AmountA, ChfMoney.value f.Amount)
    Assert.Equal(dateToDateTime "2023-07-27",ExportDate.value g.Date)
    Assert.Equal(AmountC, ChfMoney.value g.Amount)
