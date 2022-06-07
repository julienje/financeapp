open System.Text.Json
open System.Text.Json.Serialization
open FinanceApp
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

let private treatResponse (context: HttpContext) resp : HttpHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        json res
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let webApp =
    choose [ GET
             >=> route "/accounts"
             >=> fun next context ->
                     task {
                         let! accounts = Service.handleGetAllAccountAsync ()
                         return! json accounts next context
                     }
             PUT
             >=> route "/accounts/new"
             >=> fun next context ->
                     task {
                         let! openAccountDto = context.BindJsonAsync<OpenAccountDto>()
                         let! openAccount = Service.handleOpenAccountAsync openAccountDto
                         let resp = treatResponse context openAccount
                         return! resp next context
                     }
             >=> route "/accounts/close"
             >=> fun next context ->
                     task {
                         let! closeAccountDto = context.BindJsonAsync<CloseAccountDto>()
                         let! openAccount = Service.handleOpenAccountAsync closeAccountDto
                         let resp = treatResponse context openAccount
                         return! resp next context
                     } ]

let configureApp (app: IApplicationBuilder) =
    // Add Giraffe to the ASP.NET Core pipeline
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())
    services.AddSingleton(jsonOptions) |> ignore

    services.AddSingleton<Json.ISerializer, SystemTextJson.Serializer>()
    |> ignore

[<EntryPoint>]
let main _ =
    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(configureApp)
                .ConfigureServices(configureServices)
            |> ignore)
        .Build()
        .Run()

    0
