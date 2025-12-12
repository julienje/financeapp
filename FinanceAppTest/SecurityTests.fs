module SecurityTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Security.Claims
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Testcontainers.MongoDb
open Xunit

let options =
    JsonFSharpOptions.Default().WithSkippableOptionFields().ToJsonSerializerOptions()

let read (json: string) : 'a =
    JsonSerializer.Deserialize<'a>(json, options)

let write dto : string =
    JsonSerializer.Serialize<'a>(dto, options)

// ---------------------------------
// Helper functions
// ---------------------------------


let webApp () =
    task {
        let host =
            HostBuilder()
                .ConfigureWebHost(fun webHostBuilder ->
                    webHostBuilder
                        .UseTestServer()
                        .Configure(FinanceApp.App.configureApp)
                        .ConfigureServices(FinanceApp.App.configureServices)
                    |> ignore)
                .Build()

        do! host.StartAsync()
        return host
    }

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


[<Fact>]
let ``Security test`` () =
    use server = webApp().GetAwaiter().GetResult()
    use client = server.GetTestClient()

    let responseGet = client |> httpGet "/accounts"

    Assert.Equal(HttpStatusCode.Unauthorized, responseGet.StatusCode)

    let accountA =
        { Name = "AccountA"
          Type = None
          Company = "banka"
          OpenDate = "2022-01-01" }

    let responsePut = client |> httpPut "/accounts/new" (accountA |> write)
    Assert.Equal(HttpStatusCode.Unauthorized, responsePut.StatusCode)

    let responseDelete = client |> httpDelete $"/balances/1"
    Assert.Equal(HttpStatusCode.Unauthorized, responseDelete.StatusCode)
