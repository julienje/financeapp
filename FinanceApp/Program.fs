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
open FsToolkit.ErrorHandling

let private treatResponse (context: HttpContext) resp convertToDto : HttpHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        let dto = convertToDto res
        json dto
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let private newBalanceHandler (accountId: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! inputDto = context.BindJsonAsync<AddBalanceDto>()
            let! result =
                taskResult {
                    let! toDomain= AddBalanceDto.toDomain accountId inputDto
                    return toDomain
                }
            return! json result next context
        }

let webApp =
    choose [ GET
             >=> route "/accounts"
             >=> fun next context ->
                     task {
                         let! accounts = Service.handleGetAllAccountAsync MongoDb.findAllAsync

                         let dto =
                             accounts |> List.map AccountDto.fromDomain

                         return! json dto next context
                     }
             PUT
             >=> choose [ route "/accounts/new"
                          >=> fun next context ->
                                  task {
                                      let! inputDto = context.BindJsonAsync<OpenAccountDto>()

                                      let! result =
                                          taskResult {
                                              let! toDomain = OpenAccountDto.toDomain inputDto

                                              let! domain =
                                                  Service.handleOpenAccountAsync
                                                      MongoDb.getAccountByNameAndCompanyAsync
                                                      MongoDb.openAccountAsync
                                                      toDomain

                                              return domain
                                          }

                                      let resp =
                                          treatResponse context result AccountDto.fromDomain

                                      return! resp next context
                                  }
                          route "/accounts/close"
                          >=> fun next context ->
                                  task {
                                      let! inputDto = context.BindJsonAsync<CloseAccountDto>()

                                      let! result =
                                          taskResult {
                                              let! toDomain = CloseAccountDto.toDomain inputDto

                                              let! closeAccount =
                                                  Service.handleCloseAccountAsync MongoDb.updateCloseDateAsync toDomain

                                              return closeAccount
                                          }

                                      let resp =
                                          treatResponse context result AccountDto.fromDomain

                                      return! resp next context
                                  }
                          routef "/accounts/%s/balance/new" newBalanceHandler ] ]

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
