namespace SignalRApp

module Api =
    open Fable.Remoting.Server
    open Fable.Remoting.Giraffe
    open Giraffe
    open Microsoft.AspNetCore.Http
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.FSharp.Quotations
    open Saturn
    open SignalRApp.Api
    
    let getRoute<'u> (basePath: string) (expr: Expr<IApi -> Async<'u>>) =
        match expr with
        | Patterns.ProxyLambda (name, []) ->
            Api.path () name |> sprintf "%s%s" basePath
        | _ -> failwithf "Invalid path given for quotation: %A" expr

    let private auth : Reader<HttpContext,string -> string -> Async<Result<string,string>>> =
        reader {
            let! ctx = resolve<HttpContext>()
            let issuer = ctx.RequestServices.GetRequiredService<Auth.JwtIssuer>()

            return 
                fun username password ->
                    async {
                        let id = username.GetHashCode() |> string

                        if username <> "admin" || password <> "admin" then
                            return Error "Invalid credentials"
                        else
                            return!
                                async {
                                    let! token =
                                        Auth.Token.generateClaimsIdentity username id
                                        |> Auth.Token.generateEncodedToken issuer username 
                                        
                                    return Ok token
                                }
                    }
        }

    let private api : Reader<HttpContext,IApi> = 
        reader {
            let! _ = resolve<HttpContext>()
            let! auth = auth

            return {
                login = auth
            }
        }

    let router : HttpHandler =
        Remoting.createApi()
        |> Remoting.fromReader api
        |> Remoting.withRouteBuilder Api.path
        |> Remoting.buildHttpHandler
