module Tests

open System
open System.IO
open System.Net
open System.Net.Http
open DotNet.Testcontainers.Builders
open DotNet.Testcontainers.Configurations
open DotNet.Testcontainers.Containers
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Xunit


// ---------------------------------
// Helper functions (extend as you need)
// ---------------------------------

let createHost () =
    WebHostBuilder()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> FinanceApp.App.configureApp)
        .ConfigureServices(Action<IServiceCollection> FinanceApp.App.configureServices)

let runTask task =
    task |> Async.AwaitTask |> Async.RunSynchronously

let httpGet (path: string) (client: HttpClient) = path |> client.GetAsync |> runTask

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
    member this.``Route /accounts/new get account`` () =
        use server = new TestServer(createHost ())
        use client = server.CreateClient()
        client
        |> httpGet "/accounts"
        |> ensureSuccess
        |> readText
        |> shouldEqual "[]"

    interface IClassFixture<MongoDbFixture>
