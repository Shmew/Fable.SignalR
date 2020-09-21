namespace SignalRApp    

module App =
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Logging
    open Saturn
    open System

    module Setup =
        open SignalRApp.Auth

        let url = sprintf "http://0.0.0.0:%i/" <| Env.getPortsOrDefault 8085us

        let jwtIssuer =
            { Audience = sprintf "http://localhost:%i" <| Env.getPortsOrDefault 8085us
              Issuer = "SignalRApp Api"
              NotBefore = DateTime.Now
              RequiredHttpsMetadata = false
              ValidFor = TimeSpan.FromDays(1.) }
            |> JwtIssuer.create

        let services (services: IServiceCollection) =
            services
            |> Ticker.Create

    [<EntryPoint>]
    let main args =
        try
            let app =
                application {
                    use_signalr (
                        configure_signalr {
                            endpoint Endpoints.Root
                            send SignalRHub.send
                            invoke SignalRHub.invoke
                            stream_from SignalRHub.Stream.sendToClient
                            use_bearer_auth
                            with_after_routing (fun builder ->
                                builder
                                    .UseAuthentication()
                                    .UseAuthorization()
                            )
                            with_endpoint_config (fun builder ->
                                builder.RequireAuthorization()
                            )
                            use_messagepack
                        }
                    )
                    config_auth Setup.jwtIssuer
                    service_config Setup.services
                    url Setup.url
                    use_router Api.router
                    use_static (Env.clientPath args)
                    use_developer_exceptions
                }
            printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
            run app
            0 // return an integer exit code
        with e ->
            let color = Console.ForegroundColor
            Console.ForegroundColor <- System.ConsoleColor.Red
            Console.WriteLine(e.Message)
            Console.ForegroundColor <- color
            1 // return an integer exit code
