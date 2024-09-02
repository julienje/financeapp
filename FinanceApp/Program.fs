module FinanceApp.App

open System.Net
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
open Oxpecker
open FsToolkit.ErrorHandling
open Microsoft.Identity.Web

let asdf (context: HttpContext) call convertToDto : EndpointHandler =
    call context
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        let dto = convertToDto res
        json dto
    | Error e ->
        context.SetStatusCode 400
        json { Error = e }

let treatDtoResponse (context: HttpContext) resp convertToDto : EndpointHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        let dto = convertToDto res
        json dto
    | Error e ->
        context.SetStatusCode 400
        json { Error = e }

let treatResultResponse (context: HttpContext) resp : EndpointHandler =
    match resp with
    | Ok res ->
        context.SetStatusCode 200
        text ""
    | Error e ->
        context.SetStatusCode 400
        text $"""{{ "error": "{e}"}}"""

let convertSeq convertToDto seq = seq |> Seq.map convertToDto

let newBalanceHandler (accountId: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! inputDto = context.BindJson<AddBalanceDto>()

            let! result =
                taskResult {
                    let! toDomain = AddBalanceDto.toDomain accountId inputDto
                    return! Service.handleAddBalanceAsync MongoDb.findAccountAsync MongoDb.insertBalanceAsync toDomain
                }

            let resp = treatDtoResponse context result AccountBalanceDto.fromDomain
            return! resp
        }

let newInvestmentHandler (companyName: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! inputDto = context.BindJson<AddInvestmentDto>()

            let! result =
                taskResult {
                    let! toDomain = InvestmentDto.toDomain companyName inputDto

                    return!
                        Service.handleAddInvestmentAsync
                            MongoDb.getAllInvestmentCompanyAsync
                            MongoDb.insertInvestmentAsync
                            toDomain
                }

            let resp = treatDtoResponse context result InvestmentDto.fromDomain

            return! resp
        }

let getBalancesAccountHandler (accountId: string) : EndpointHandler =
    fun (context: HttpContext) ->
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

            let resp = treatDtoResponse context result (convertSeq AccountBalanceDto.fromDomain)

            return! resp
        }

let deleteBalancesAccountHandler (balanceId: string) : EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! result =
                taskResult {
                    let! idDomain = AccountBalanceId.create balanceId
                    return! Service.handleDeleteBalanceAsync MongoDb.deleteBalanceAsync idDomain
                }

            let resp = treatResultResponse context result
            return! resp
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

                    return wealth
                }

            let resp = treatDtoResponse context result WealthDto.fromDomain
            return! resp
        }

let handleGetTrend: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! trend = Service.handleGetTrendsAsync MongoDb.findAllAccountsAsync MongoDb.getAllBalancesAsync

            let dto = trend |> TrendDto.fromDomain
            return! context.WriteJson dto
        }

let handleGetInvestmentCompanies: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! companies = Service.handleGetInvestmentCompanyAsync MongoDb.getAllInvestmentCompanyAsync
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

                    return profit
                }

            let resp = treatDtoResponse context result ProfitDto.fromDomain
            return! resp
        }

let handlePutNewAccounts: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! inputDto = context.BindJson<OpenAccountDto>()

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

            return! resp
        }

let handlePutCloseAccounts: EndpointHandler =
    fun (context: HttpContext) ->
        task {
            let! inputDto = context.BindJson<CloseAccountDto>()

            let! result =
                taskResult {
                    let! toDomain = CloseAccountDto.toDomain inputDto

                    let! closeAccount = Service.handleCloseAccountAsync MongoDb.updateCloseDateAsync toDomain

                    return closeAccount
                }

            let resp = treatDtoResponse context result AccountDto.fromDomain

            return! resp
        }

let webApp =
    [ GET
          [ route "/accounts" <| handleGetAccounts
            route "/wealth" <| handleGetWealth
            route "/trend" <| handleGetTrend
            routef "/accounts/%s/balances" getBalancesAccountHandler
            route "/investment/companies" <| handleGetInvestmentCompanies
            route "/investment/profit" <| handleGetInvestmentProfit ]
      PUT
          [ route "/accounts/new" <| handlePutNewAccounts
            route "/accounts/close" <| handlePutCloseAccounts
            routef "/accounts/%s/balances/new" newBalanceHandler
            routef "/investment/companies/%s/new" newInvestmentHandler ]
      DELETE [ routef "/balances/%s" deleteBalancesAccountHandler ] ]

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
    services
        .AddRouting()
        .AddOxpecker()
        .AddCors()
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
