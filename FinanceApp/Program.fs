module FinanceApp.App

open System.Text.Json
open System.Text.Json.Serialization
open FinanceApp
open FinanceApp.DomainType
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FsToolkit.ErrorHandling
open Microsoft.Extensions.Logging
open Microsoft.Identity.Web
open Microsoft.IdentityModel.Logging

let treatResponse (context: HttpContext) resp convertToDto : HttpHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        let dto = convertToDto res
        json dto
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let newBalanceHandler (accountId: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! inputDto = context.BindJsonAsync<AddBalanceDto>()

            let! result =
                taskResult {
                    let! toDomain = AddBalanceDto.toDomain accountId inputDto
                    return! Service.handleAddBalanceAsync MongoDb.findAccountAsync MongoDb.insertBalanceAsync toDomain
                }

            let resp = treatResponse context result AccountBalanceDto.fromDomain

            return! resp next context
        }

// let notLoggedIn = RequestErrors.UNAUTHORIZED "Bearer" "JJ" "You must be logged in."

let mustBeLoggedIn =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let webApp =
    mustBeLoggedIn
    >=> choose
            [ GET
              >=> choose
                      [ route "/accounts"
                        >=> fun next context ->
                                task {
                                    let! accounts = Service.handleGetAllAccountAsync MongoDb.findAllAsync

                                    let dto = accounts |> List.map AccountDto.fromDomain

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
              >=> choose
                      [ route "/accounts/new"
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

                                    let resp = treatResponse context result AccountDto.fromDomain

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

                                    let resp = treatResponse context result AccountDto.fromDomain

                                    return! resp next context
                                }
                        routef "/accounts/%s/balances/new" newBalanceHandler ] ]

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.UseCors(configureCors).UseAuthentication().UseGiraffe webApp

let configureMicrosoftAccount (option: MicrosoftIdentityOptions) =
    option.Instance <- "https://login.microsoftonline.com"
    option.ClientId <- "1cfe66e3-db51-4082-93ad-0814bff72abf"
    option.TenantId <- "0829ce3c-dd9d-45a5-a7e4-b8fb69179085"

let configureBearer (_: JwtBearerOptions) = ()

let configureLogging (builder: ILoggingBuilder) =
    // Set a logging filter (optional)
    let filter (_: LogLevel) = true
    IdentityModelEventSource.ShowPII <- true
    printf $"pii is %b{IdentityModelEventSource.ShowPII}"
    // Configure the logging factory
    builder
        .AddFilter(filter) // Optional filter
        .AddConsole() // Set up the Console logger
        .AddDebug() // Set up the Debug logger
    // Add additional loggers if wanted...
    |> ignore


let configureServices (services: IServiceCollection) =
    // Add Giraffe dependencies
    let jsonOptions = JsonSerializerOptions()
    jsonOptions.Converters.Add(JsonFSharpConverter())
    services
        .AddGiraffe()
        .AddSingleton(jsonOptions)
        .AddCors()
        .AddSingleton<Json.ISerializer, SystemTextJson.Serializer>()
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(
            configureBearer,
            configureMicrosoftAccount,
            JwtBearerDefaults.AuthenticationScheme,
            true
        )
        .EnableTokenAcquisitionToCallDownstreamApi(fun x -> ())
        .AddInMemoryTokenCaches()
    |> ignore

[<EntryPoint>]
let main _ =
    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
            |> ignore)
        .Build()
        .Run()

    0
