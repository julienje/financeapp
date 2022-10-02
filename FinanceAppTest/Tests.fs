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
            new MongoDbTestcontainerConfiguration(Database = "db", Username = null, Password = null)
    let myContainer =
            TestcontainersBuilder<MongoDbTestcontainer>()
                .WithDatabase(config)
                .Build()

    interface IDisposable with
        member _.Dispose() =
            //CLEAN UP TEST DATA OR WHATEVER YOU NEED TO CLEANUP YOUR TESTS
            ()



// Tests wit test container

type TestContainerTest() =
    [<Fact>]
    member _.``Can create a start node``() =
        //DO THE TEST STUFF
        "INPUT" |> shouldEqual "RESULT"

    interface IClassFixture<MongoDbFixture>

[<Fact>]
let ``Route /accounts/new get account`` () =
    use server = new TestServer(createHost ())
    use client = server.CreateClient()

    client
    |> httpGet "/accounts"
    |> ensureSuccess
    |> readText
    |> shouldEqual "Hello world, from Giraffe!"
