open FinanceApp
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe

let webApp =
    choose [ GET
             >=> route "/accounts"
             >=> fun next context ->
                     task {
                         let! accounts = Service.handleGetAllAccountAsync ()
                         return! json accounts next context
                     }
             POST
             >=> route "/accounts/new"
             >=> fun next context ->
                     task {
                         let! openAccountDto = context.BindJsonAsync<OpenAccountDto>()
                         let! openAccount = Service.handleOpenAccountAsync openAccountDto
                         return! json openAccount next context
                     } ]

let configureApp (app: IApplicationBuilder) =
    // Add Giraffe to the ASP.NET Core pipeline
    app.UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    // Add Giraffe dependencies
    services.AddGiraffe() |> ignore

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
