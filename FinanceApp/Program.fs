module FinanceApp.App

open System.Text.Json.Serialization
open FinanceApp
open FinanceApp.DomainType
open FinanceApp.DtoTypes
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Oxpecker
open FsToolkit.ErrorHandling
open Microsoft.Identity.Web


let convertResponse (context: HttpContext) resp =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        context.WriteJson res
    | Error e ->
        context.SetStatusCode 400
        context.WriteJson { Error = e }

let convertSeq convertToDto seq = seq |> Seq.map convertToDto

let newBalanceHandler (accountId: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! inputDto = context.BindJson<AddBalanceDto>()
                    let! toDomain = AddBalanceDto.toDomain accountId inputDto

                    let! result =
                        Service.handleAddBalanceAsync
                            MongoDb.findAccountAsync
                            MongoDb.insertBalanceAsync
                            toDomain

                    return result |> AccountBalanceDto.fromDomain
                }
            return! convertResponse context result
        }

let newInvestmentHandler (companyName: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! inputDto = context.BindJson<AddInvestmentDto>()
                    let! toDomain = InvestmentDto.toDomain companyName inputDto

                    let! result =
                        Service.handleAddInvestmentAsync
                            MongoDb.getAllInvestmentCompanyAsync
                            MongoDb.insertInvestmentAsync
                            toDomain

                    return result |> InvestmentDto.fromDomain
                }

            return! convertResponse context result
        }

let handleGetInvestmentPerCompany (company : string) : EndpointHandler =
    fun(context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! companyName = CompanyName.create company

                    let! result=
                        Service.handleGetInvestmentPerCompanyAsync
                            MongoDb.findAllInvestmentForACompany
                            companyName
                    return result |> convertSeq InvestmentDto.fromDomain
                }

            return! convertResponse context result
        }

let getBalancesAccountHandler (accountId: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! accountIdDomain = AccountId.create accountId

                    let! result=
                        Service.handleGetAllBalanceForAnAccountAsync
                            MongoDb.findAccountAsync
                            MongoDb.findAllBalancesForAnAccountAsync
                            accountIdDomain
                    return result |> convertSeq AccountBalanceDto.fromDomain
                }

            return! convertResponse context result
        }

let deleteBalancesAccountHandler (balanceId: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! idDomain = AccountBalanceId.create balanceId
                    return! Service.handleDeleteBalanceAsync MongoDb.deleteBalanceAsync idDomain
                }

            return! convertResponse context result
        }

let handleGetAccounts: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! accounts = Service.handleGetAllAccountAsync MongoDb.findAllAccountsAsync
            let dto = accounts |> Seq.map AccountDto.fromDomain
            return! context.WriteJson dto
        }

let handleGetWealth: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! date =
                        match context.TryGetQueryValue "date" with
                        | None -> Ok(ExportDate.now)
                        | Some q -> ExportDate.create q

                    let! wealth =
                        Service.handleGetWealthAsync
                            MongoDb.findActiveDbAccountAsync
                            MongoDb.findLastBalanceAccountAsync
                            date

                    return wealth |> WealthDto.fromDomain
                }

            return! convertResponse context result
        }

let handleGetTrend: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! trend =
                Service.handleGetTrendsAsync
                    MongoDb.findAllAccountsAsync
                    MongoDb.getAllBalancesAsync

            let dto = trend |> TrendDto.fromDomain
            return! context.WriteJson dto
        }

let handleGetInvestmentCompanies: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! companies =
                Service.handleGetInvestmentCompanyAsync MongoDb.getAllInvestmentCompanyAsync

            let dto = companies |> CompanyDto.fromDomain
            return! context.WriteJson dto
        }

let handleGetInvestmentProfit: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! date =
                        match context.TryGetQueryValue "date" with
                        | None -> Ok(ProfitDate.now)
                        | Some q -> ProfitDate.create q

                    let! profit =
                        Service.handleGetInvestmentAsync
                            MongoDb.findAllInvestment
                            MongoDb.findActiveDbAccountAsync
                            MongoDb.findLastBalanceAccountAsync
                            date

                    return profit |> ProfitDto.fromDomain
                }

            return! convertResponse context result
        }

let handlePutNewAccounts: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! inputDto = context.BindJson<OpenAccountDto>()
                    let! toDomain = OpenAccountDto.toDomain inputDto

                    let! domain =
                        Service.handleOpenAccountAsync
                            MongoDb.getAccountByNameAndCompanyAsync
                            MongoDb.insertAccountAsync
                            toDomain

                    return domain |> AccountDto.fromDomain
                }

            return! convertResponse context result
        }

let handlePutCloseAccounts: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! inputDto = context.BindJson<CloseAccountDto>()

            let! result =
                taskResult {
                    let! toDomain = CloseAccountDto.toDomain inputDto

                    let! closeAccount =
                        Service.handleCloseAccountAsync MongoDb.updateCloseDateAsync toDomain

                    return closeAccount |> AccountDto.fromDomain
                }

            return! convertResponse context result
        }

let webApp =
    [ GET
          [ route "/accounts" handleGetAccounts
            route "/wealth" handleGetWealth
            route "/trend" handleGetTrend
            routef "/accounts/{%s}/balances" getBalancesAccountHandler
            route "/investment/companies" handleGetInvestmentCompanies
            routef "/investment/companies/{%s}" handleGetInvestmentPerCompany
            route "/investment/profit" handleGetInvestmentProfit ]
      PUT
          [ route "/accounts/new" handlePutNewAccounts
            route "/accounts/close" handlePutCloseAccounts
            routef "/accounts/{%s}/balances/new" newBalanceHandler
            routef "/investment/companies/{%s}/new" newInvestmentHandler
          ]
      DELETE [ routef "/balances/{%s}" deleteBalancesAccountHandler ]
      ]

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app: IApplicationBuilder) =
    // let env = app.ApplicationServices.GetService<IHostEnvironment>()
    app.UseCors(configureCors).UseAuthentication().UseRouting().UseOxpecker(webApp)
    |> ignore

let configureMicrosoftAccount (option: MicrosoftIdentityOptions) =
    option.Instance <- "https://login.microsoftonline.com"
    option.ClientId <- "1cfe66e3-db51-4082-93ad-0814bff72abf"
    option.TenantId <- "0829ce3c-dd9d-45a5-a7e4-b8fb69179085"

let configureServices (services: IServiceCollection) =
    let options = JsonFSharpOptions.Default().WithSkippableOptionFields().ToJsonSerializerOptions()
    services
        .AddRouting()
        .AddOxpecker()
        .AddCors()
        .AddSingleton<IJsonSerializer>(SystemTextJsonSerializer(options))
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi((fun o -> ()), configureMicrosoftAccount)
    |> ignore

[<EntryPoint>]
let main _ =
    let builder = WebApplication.CreateBuilder()
    configureServices builder.Services
    let app = builder.Build()
    configureApp app
    app.Run()
    0