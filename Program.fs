module Cart.Program

open System.Threading.Tasks
open Cart.Abstractions
open Cart.Handlers
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Oxpecker
open type Microsoft.AspNetCore.Http.TypedResults


let getEndpoints env = [
    subRoute "/cart" [
        GET [
            routef "/{%O:guid}" <| CartHandlers.getCart env
            routef "/{%O:guid}/total" <| CartHandlers.getCartTotal env
        ]
        POST [
            routef "/{%O:guid}" <| CartHandlers.addItemToCart env
        ]
    ]
]


let notFoundHandler (ctx: HttpContext) =
    let logger = ctx.GetLogger()
    logger.LogWarning("Unhandled 404 error")
    ctx.Write <| NotFound {| Error = "Resource was not found" |}
    
let errorHandler (ctx: HttpContext) (next: RequestDelegate) =
    task {
        try
            return! next.Invoke(ctx)
        with
        | :? ModelBindException
        | :? RouteParseException as ex ->
            let logger = ctx.GetLogger()
            logger.LogWarning(ex, "Unhandled 400 error")
            return! ctx.Write <| BadRequest {| Error = ex.Message |}
        | ex ->
            let logger = ctx.GetLogger()
            logger.LogError(ex, "Unhandled 500 error")
            ctx.SetStatusCode StatusCodes.Status500InternalServerError
            return! ctx.WriteText <| string ex
    }
    :> Task
    
let configureServices (services: IServiceCollection) =
    services
        .AddCors(fun options -> options.AddDefaultPolicy(fun policy ->
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader() |> ignore))
        .AddRouting()
        .AddOxpecker() |> ignore

type CartEnv(connectionString: string, logger: ILogger) =
    interface ICartDbEnv with
        member _.ConnectionString = connectionString
    interface IAppLogger with
        member _.Logger = logger
        
let configureApp (appBuilder: IApplicationBuilder) =
    let logger = appBuilder.ApplicationServices.GetRequiredService<ILogger>()
    let connectionString = "Host=localhost;Database=cartdb;Username=postgres;Password=admin;"
    let env = CartEnv(connectionString, logger)

    // Wrap env into OperationEnv so services can use the repository interfaces
    let operationEnv = OperationEnv(env)

    appBuilder
        .UseRouting()
        .UseCors()
        .Use(errorHandler)
        .UseOxpecker(getEndpoints operationEnv)
        .Run(notFoundHandler)
        
        
[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services
    let app = builder.Build()
    configureApp app
    app.Run()
    0