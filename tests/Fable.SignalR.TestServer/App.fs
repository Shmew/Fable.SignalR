namespace SignalRApp    

module App =
    open Fable.SignalR
    open Fable.SignalR.Akka
    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.DependencyInjection
    open Saturn
    open System

    let app =
        application {
            use_signalr (
                configure_signalr {
                    endpoint Endpoints.Root
                    send SignalRHub.send
                    invoke SignalRHub.invoke
                    stream_from SignalRHub.Stream.sendToClient
                    stream_to SignalRHub.Stream.getFromClient
                    with_log_level Microsoft.Extensions.Logging.LogLevel.None
                    with_hub_options (fun ho -> ho.EnableDetailedErrors <- Nullable<bool>(true))
                }
            )
            use_signalr (
                configure_signalr {
                    endpoint Endpoints.Root2
                    send SignalRHub2.send
                    invoke SignalRHub2.invoke
                    stream_from SignalRHub2.Stream.sendToClient
                    stream_to SignalRHub2.Stream.getFromClient
                    with_log_level Microsoft.Extensions.Logging.LogLevel.None
                    with_hub_options (fun ho -> ho.EnableDetailedErrors <- Nullable<bool>(true))
                    use_messagepack
                }
            )
            use_signalr (
                configure_signalr {
                    endpoint Endpoints.RootAkka
                    send SignalRHub.send
                    invoke SignalRHub.invoke
                    stream_from SignalRHub.Stream.sendToClient
                    stream_to SignalRHub.Stream.getFromClient
                    use_server_builder (fun builder -> builder.AddAkkaClustering())
                    with_log_level Microsoft.Extensions.Logging.LogLevel.None
                    with_hub_options (fun ho -> ho.EnableDetailedErrors <- Nullable<bool>(true))
                }
            )
            use_signalr (
                configure_signalr {
                    endpoint Endpoints.RootAkka2
                    send SignalRHub2.send
                    invoke SignalRHub2.invoke
                    stream_from SignalRHub2.Stream.sendToClient
                    stream_to SignalRHub2.Stream.getFromClient
                    use_server_builder (fun builder -> builder.AddAkkaClustering())
                    with_log_level Microsoft.Extensions.Logging.LogLevel.None
                    with_hub_options (fun ho -> ho.EnableDetailedErrors <- Nullable<bool>(true))
                    use_messagepack
                }
            )
            service_config (fun s -> s.AddSingleton<RandomStringGen>())
            logging (fun l -> l.AddFilter("Microsoft", LogLevel.Error) |> ignore)
            url (sprintf "http://localhost:%i/" <| Env.getPortsOrDefault 8085us)
            use_cors "Any" (fun policy -> 
                policy
                    .WithOrigins("http://localhost", "http://127.0.0.1:80")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                |> ignore
            )
            no_router
            use_developer_exceptions
        }

    [<EntryPoint>]
    let main _ =
        try
            printfn "Working directory - %s" (System.IO.Directory.GetCurrentDirectory())
            run app
            0 // return an integer exit code
        with e ->
            let color = Console.ForegroundColor
            Console.ForegroundColor <- System.ConsoleColor.Red
            Console.WriteLine(e.Message)
            Console.ForegroundColor <- color
            1 // return an integer exit code
