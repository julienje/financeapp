module Tests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Security.Claims
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open System.Threading.Tasks
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Testcontainers.MongoDb
open Xunit

let newAccountPayload name =
    $"""
{{
  "Name": "{name}",
  "Company": "UBS",
  "OpenDate": "2022-01-01"
}}"""

let newAccountWitTypePayload name accountType =
    $"""
{{
  "Name": "{name}",
  "Type": "{accountType}",
  "Company": "UBS",
  "OpenDate": "2022-01-01"
}}"""

let newBalancePayload amount =
    $"""
{{
  "CheckDate": "{DateTime.UtcNow}",
  "AmountInChf": {amount}
}}
"""

let newInvestmentPayload amount =
    $"""
{{
  "InvestmentDate": "{DateTime.UtcNow}",
  "AmountInChf": {amount}
}}
"""

let closeAccountPayload accountId =
    $"""
{{
  "Id": "{accountId}",
  "CloseDate": "{DateTime.UtcNow}"
}}
"""

let options = JsonFSharpOptions.Default().WithSkippableOptionFields().ToJsonSerializerOptions()

let read (json:string) : 'a =
    JsonSerializer.Deserialize<'a>(json, options)

// ---------------------------------
// Helper functions
// ---------------------------------

let scheme = "TestScheme"

type TestAuthHandler(options, logger, encoder) =

    inherit AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)

    override this.HandleAuthenticateAsync() =
        let aClaim = Claim(ClaimTypes.Name, "Test user")
        let claims = [| aClaim |]
        let identity = ClaimsIdentity(claims, "Test")
        let principal = ClaimsPrincipal(identity)
        let ticket = AuthenticationTicket(principal, scheme)
        let result = AuthenticateResult.Success(ticket)
        Task.FromResult(result)

let configureTestServices (services: IServiceCollection) =
    services
        .AddAuthentication(scheme)
        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(scheme, (fun o -> ()))
    |> ignore

let createHost () =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(FinanceApp.App.configureApp)
        .ConfigureServices(FinanceApp.App.configureServices)
        .ConfigureServices(configureTestServices)

let runTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let httpGet (path: string) (client: HttpClient) = path |> client.GetAsync |> runTask

let httpDelete (path: string) (client: HttpClient) = path |> client.DeleteAsync |> runTask

let httpPost (path: string) (payload: string) (client: HttpClient) =
    use content = new StringContent(payload, Encoding.UTF8, "application/json")
    client.PostAsync(path, content) |> runTask

let httpPut (path: string) (payload: string) (client: HttpClient) =
    use content = new StringContent(payload, Encoding.UTF8, "application/json")
    client.PutAsync(path, content) |> runTask

let isStatus (code: HttpStatusCode) (response: HttpResponseMessage) =
    Assert.Equal(code, response.StatusCode)
    response

let ensureSuccess (response: HttpResponseMessage) =
    if not response.IsSuccessStatusCode then
        response.Content.ReadAsStringAsync()
        |> runTask
        |> failwithf "Error code %A with text %A" response.StatusCode
    else
        response

let readText (response: HttpResponseMessage) =
    response.Content.ReadAsStringAsync() |> runTask

let shouldEqual (expected: string) (actual: string) = Assert.Equal(expected, actual)

let shouldContain (expected: string) (actual: string) = Assert.True(actual.Contains expected)

let shouldHaveId (actual: String) : String =
    Assert.True(actual.Contains "Id")
    let parsed = actual |> JsonObject.Parse
    parsed["Id"].GetValue()

let shouldPropertyHasValue (property: String) expected (payload: String) =
    let parsed = payload |> JsonObject.Parse
    let result = parsed[property].GetValue<Decimal>()
    Assert.Equal(expected, result)

let shouldJsonArrayLengthBe expected (payload: String) =
    let parsed = payload |> JsonObject.Parse
    let result = parsed.AsArray().Count
    Assert.Equal(expected, result)

type MongoDbFixture() =

    let myContainer =
        MongoDbBuilder().WithUsername("unitest").WithPassword("1234").Build()

    member this.MyContainer = myContainer

    interface IAsyncLifetime with
        member this.DisposeAsync() =
            this.MyContainer.DisposeAsync().AsTask()

        member this.InitializeAsync() = this.MyContainer.StartAsync()

// Tests wit test container

type TestContainerTest(mongoDb: MongoDbFixture) =
    do
        Environment.SetEnvironmentVariable(
            "CONNECTION_STRING",
            mongoDb.MyContainer.GetConnectionString()
        )

    [<Fact>]
    member this.``Smoke test``() =
        use server = new TestServer(createHost ())
        use client = server.CreateClient()

        client |> httpGet "/accounts" |> ensureSuccess |> readText |> shouldEqual "[]"

        let newAccountA =
            (client
             |> httpPut "/accounts/new" (newAccountPayload "AccountA")
             |> ensureSuccess
             |> readText
             |> shouldHaveId)

        let newAccountB =
            (client
             |> httpPut "/accounts/new" (newAccountWitTypePayload "AccountB" "ETF")
             |> ensureSuccess
             |> readText
             |> shouldHaveId)

        let amountA = 12.0m
        let amountB = 15.5m

        let balanceAccountA =
            (client
             |> httpPut $"/accounts/{newAccountA}/balances/new" (newBalancePayload amountA)
             |> ensureSuccess
             |> readText
             |> shouldHaveId)

        client
        |> httpPut $"/accounts/{newAccountB}/balances/new" (newBalancePayload amountB)
        |> ensureSuccess
        |> readText
        |> shouldHaveId
        |> ignore

        client
        |> httpGet "wealth"
        |> ensureSuccess
        |> readText
        |> shouldPropertyHasValue "AmountInChf" (amountA + amountB)

        client
        |> httpGet "wealth?date=2021-06-01"
        |> ensureSuccess
        |> readText
        |> shouldPropertyHasValue "AmountInChf" 0.0m

        client
        |> httpGet $"/accounts/{newAccountA}/balances"
        |> ensureSuccess
        |> readText
        |> shouldJsonArrayLengthBe 1

        client
        |> httpDelete $"/balances/{balanceAccountA}"
        |> ensureSuccess
        |> readText
        |> ignore

        client
        |> httpGet $"/accounts/{newAccountA}/balances"
        |> ensureSuccess
        |> readText
        |> shouldJsonArrayLengthBe 0

        let investmentA = 12.0m

        client
        |> httpGet $"/investment/companies"
        |> ensureSuccess
        |> readText
        |> shouldJsonArrayLengthBe 1

        client
        |> httpPut $"/investment/companies/UBS/new" (newInvestmentPayload investmentA)
        |> ensureSuccess
        |> readText
        |> shouldHaveId
        |> ignore

        let profit : ProfitDto= client |> httpGet $"/investment/profit" |> ensureSuccess |> readText |> read
        Assert.Equal(profit.Profit.InvestmentInChf, investmentA)
        Assert.Equal(profit.Profit.WealthInChf, amountB)
        Assert.Equal(profit.Details |> Seq.length,1)
        let details = profit.Details |> Seq.head
        Assert.Equal(details.Profit.InvestmentInChf,investmentA)
        Assert.Equal(details.Profit.WealthInChf,amountB)

        let closeAccount : AccountDto= client |> httpPut $"/accounts/close" (closeAccountPayload newAccountA) |> ensureSuccess |> readText |> read
        Assert.True(closeAccount.CloseDate.IsSome)

        ()



    interface IClassFixture<MongoDbFixture>
