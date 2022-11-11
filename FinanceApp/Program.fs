module FinanceApp.App

open System.Text.Json
open System.Text.Json.Serialization
open FinanceApp
open FinanceApp.DomainType
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
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
                    let! toDomain = AddBalanceDto.toDomain accountId inputDto
                    return! Service.handleAddBalanceAsync MongoDb.findAccountAsync MongoDb.insertBalanceAsync toDomain
                }

            let resp =
                treatResponse context result AccountBalanceDto.fromDomain

            return! resp next context
        }

let webApp =
    choose [ GET
             >=> choose [ route "/accounts"
                          >=> fun next context ->
                                  task {
                                      let! accounts = Service.handleGetAllAccountAsync MongoDb.findAllAsync

                                      let dto =
                                          accounts |> List.map AccountDto.fromDomain

                                      return! json dto next context
                                  }
                          route "/wealth"
                          >=> fun next context ->
                                  task {

                                      let! wealth =
                                          Service.handleGetWealthAsync
                                              MongoDb.findActiveDbAccountAsync
                                              MongoDb.findLastBalanceAccount
                                              ExportDate.now

                                      let dto = wealth |> WealthDto.fromDomain

                                      return! json dto next context
                                  } ]
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
                                                      MongoDb.insertAccountAsync
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
                          routef "/accounts/%s/balances/new" newBalanceHandler ] ]

let configureCors (builder: CorsPolicyBuilder) =
    builder
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore

let configureApp (app: IApplicationBuilder) =
    // Add Giraffe to the ASP.NET Core pipeline
    app.UseCors(configureCors).UseGiraffe webApp

let configureServices (services: IServiceCollection) =
    // Add Giraffe dependencies
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())

    services
        .AddGiraffe()
        .AddSingleton(jsonOptions)
        .AddCors()
        .AddSingleton<Json.ISerializer, SystemTextJson.Serializer>()
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
