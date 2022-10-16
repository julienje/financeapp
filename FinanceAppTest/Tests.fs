module Tests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Text.Json.Nodes
open DotNet.Testcontainers.Builders
open DotNet.Testcontainers.Configurations
open DotNet.Testcontainers.Containers
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Xunit

let newAccountPayload name = $"""
{{
  "Name": "{name}",
  "Company": "UBS",
  "OpenDate": "2022-01-01"
}}"""

let newBalancePayload amount = $"""
{{
  "CheckDate": "{DateTime.UtcNow}",
  "AmountInChf": {amount}
}}
"""

// ---------------------------------
// Helper functions
// ---------------------------------

let createHost () =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> FinanceApp.App.configureApp)
        .ConfigureServices(Action<IServiceCollection> FinanceApp.App.configureServices)

let runTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let httpGet (path: string) (client: HttpClient) = path |> client.GetAsync |> runTask

let httpPost (path: string) (payload: string) (client: HttpClient) =
    use content = new StringContent(payload)
    client.PostAsync(path,content) |> runTask
let httpPut (path: string) (payload: string) (client: HttpClient) =
    use content = new StringContent(payload)
    client.PutAsync(path,content) |> runTask

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

let shouldEqual expected actual = Assert.Equal(expected, actual)

let shouldContain (expected: string) (actual: string) = Assert.True(actual.Contains expected)

let shouldHaveId (actual:String): String =
    Assert.True(actual.Contains "Id")
    let parsed = actual |> JsonObject.Parse
    parsed["Id"].GetValue()

let shouldPropertyHasValue (property: String) expected (payload:String) =
    let parsed = payload |> JsonObject.Parse
    let result = parsed[property].GetValue<Decimal>()
    Assert.Equal(expected,result)

// ---------------------------------
// Tests
// ---------------------------------

type MongoDbFixture() =
    let config =
            new MongoDbTestcontainerConfiguration(Database = "db", Username = "unitest", Password = "1234")
    let myContainer =
            TestcontainersBuilder<MongoDbTestcontainer>()
                .WithDatabase(config)
                .Build()
    do
        printf "Passed here"
    member this.MyContainer = myContainer

    interface IDisposable with
        member this.Dispose() =
            config.Dispose()
            
    interface IAsyncLifetime with
        member this.DisposeAsync() = this.MyContainer.DisposeAsync().AsTask();
        member this.InitializeAsync() = this.MyContainer.StartAsync();
        



// Tests wit test container

type TestContainerTest(mongoDb: MongoDbFixture) =
    do
        Environment.SetEnvironmentVariable("CONNECTION_STRING",mongoDb.MyContainer.ConnectionString)
    
    [<Fact>]
    member this.``Smoke test`` () =
        use server = new TestServer(createHost ())
        use client = server.CreateClient()
        client
        |> httpGet "/accounts"
        |> ensureSuccess
        |> readText
        |> shouldEqual "[]"
        
        let newAccountA = (client
        |> httpPut "/accounts/new" (newAccountPayload "AccountA")
        |> ensureSuccess
        |> readText
        |> shouldHaveId)
        
        let newAccountb = (client
        |> httpPut "/accounts/new" (newAccountPayload "AccountB")
        |> ensureSuccess
        |> readText
        |> shouldHaveId)
        
        let amountA = 12.0m
        let amountB = 15.5m
        client
        |> httpPut $"/accounts/{newAccountA}/balances/new" (newBalancePayload amountA)
        |> ensureSuccess
        |> readText
        |> shouldHaveId
        |> ignore
        
        client
        |> httpPut $"/accounts/{newAccountb}/balances/new" (newBalancePayload amountB)
        |> ensureSuccess
        |> readText
        |> shouldHaveId
        |> ignore
        
        client
        |> httpGet "wealth"
        |> ensureSuccess
        |> readText
        |> shouldPropertyHasValue "AmountInChf" (amountA+amountB)
        
        ()
        
        

    interface IClassFixture<MongoDbFixture>
