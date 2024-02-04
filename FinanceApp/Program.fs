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
open Microsoft.Identity.Web

let treatDtoResponse (context: HttpContext) resp convertToDto : HttpHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        let dto = convertToDto res
        json dto
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let treatResultResponse (context: HttpContext) resp : HttpHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        text ""
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let convertSeq convertToDto seq = seq |> Seq.map convertToDto

let newBalanceHandler (accountId: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! inputDto = context.BindJsonAsync<AddBalanceDto>()

            let! result =
                taskResult {
                    let! toDomain = AddBalanceDto.toDomain accountId inputDto
                    return! Service.handleAddBalanceAsync MongoDb.findAccountAsync MongoDb.insertBalanceAsync toDomain
                }

            let resp = treatDtoResponse context result AccountBalanceDto.fromDomain

            return! resp next context
        }
let newInvestmentHandler (companyName: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! inputDto = context.BindJsonAsync<AddBalanceDto>()

            let! result =
                taskResult {
                    let! toDomain = InvestmentDto.toDomain accountId inputDto
                    return! Service.handleAddInvestmentAsync MongoDb.getAllCompanyAsync MongoDb.insertInvestmentAsync toDomain
                }

            let resp = treatDtoResponse context result AccountBalanceDto.fromDomain

            return! resp next context
        }
let getBalancesAccountHandler (accountId: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! accountIdDomain = AccountId.create accountId

                    return!
                        Service.handleGetAllBalanceForAnAccountAsync
                            MongoDb.findAccountAsync
                            MongoDb.findAllBalancesForAnAccountAsync
                            accountIdDomain
                }

            let resp =
                treatDtoResponse context result (convertSeq AccountBalanceDto.fromDomain)

            return! resp next context
        }

let deleteBalancesAccountHandler (balanceId: string) : HttpHandler =
    fun (next: HttpFunc) (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! idDomain = AccountBalanceId.create balanceId
                    return! Service.handleDeleteBalanceAsync MongoDb.deleteBalanceAsync idDomain
                }

            let resp = treatResultResponse context result
            return! resp next context
        }

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
                        let! accounts = Service.handleGetAllAccountAsync MongoDb.findAllAccountsAsync

                        let dto = accounts |> Seq.map AccountDto.fromDomain

                        return! json dto next context
                    }
                route "/wealth"
                >=> fun next context ->
                    task {
                        let! result =
                            taskResult {
                                let! date =
                                    match context.TryGetQueryStringValue "date" with
                                    | None -> Ok(ExportDate.now)
                                    | Some q -> ExportDate.create q

                                let! wealth =
                                    Service.handleGetWealthAsync
                                        MongoDb.findActiveDbAccountAsync
                                        MongoDb.findLastBalanceAccountAsync
                                        date

                                return wealth
                            }

                        let resp = treatDtoResponse context result WealthDto.fromDomain
                        return! resp next context
                    }
                route "/trend"
                >=> fun next context ->
                    task {
                        let! trend =
                            Service.handleGetTrendsAsync MongoDb.findAllAccountsAsync MongoDb.getAllBalancesAsync

                        let dto = trend |> TrendDto.fromDomain
                        return! json dto next context
                    }
                routef "/accounts/%s/balances" getBalancesAccountHandler
                route "/companies"
                >=> fun next context ->
                    task {
                        let! companies = Service.handleGetCompanyAsync MongoDb.getAllCompanyAsync
                        let dto = companies |> CompanyDto.fromDomain
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

                        let resp = treatDtoResponse context result AccountDto.fromDomain

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

                        let resp = treatDtoResponse context result AccountDto.fromDomain

                        return! resp next context
                    }
                routef "/accounts/%s/balances/new" newBalanceHandler
                routef "/companies/%s/investment/new" newInvestmentHandler ]
          DELETE >=> choose [ routef "/balances/%s" deleteBalancesAccountHandler ] ]

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app: IApplicationBuilder) =
    // let env = app.ApplicationServices.GetService<IHostEnvironment>()
    app.UseCors(configureCors).UseAuthentication().UseGiraffe webApp

let configureMicrosoftAccount (option: MicrosoftIdentityOptions) =
    option.Instance <- "https://login.microsoftonline.com"
    option.ClientId <- "1cfe66e3-db51-4082-93ad-0814bff72abf"
    option.TenantId <- "0829ce3c-dd9d-45a5-a7e4-b8fb69179085"

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
        .AddMicrosoftIdentityWebApi((fun o -> ()), configureMicrosoftAccount)
    |> ignore

[<EntryPoint>]
let main _ =
    Host
        .CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHostBuilder ->
            webHostBuilder.Configure(configureApp).ConfigureServices(configureServices)
            |> ignore)
        .Build()
        .Run()

    0
