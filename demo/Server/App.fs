namespace SignalRApp    

module App =
    open Fable.SignalR
    open Giraffe.ResponseWriters
    open Microsoft.Extensions.Logging
    open Saturn
    open System

    [<EntryPoint>]
    let main args =
        try
            let app =
                application {
                    use_signalr (
                        configure_signalr {
                            endpoint Endpoints.Root
                            send SignalRHub.send
                            invoke SignalRHub.update
                            stream_from SignalRHub.Stream.sendToClient
                            stream_to SignalRHub.Stream.getFromClient
                            with_log_level Microsoft.Extensions.Logging.LogLevel.None
                        }
                    )
                    logging (fun l -> l.AddFilter("Microsoft", LogLevel.Error) |> ignore)
                    error_handler (fun e log -> text e.Message)
                    url (sprintf "http://0.0.0.0:%i/" <| Env.getPortsOrDefault 8085us)
                    use_cors "Any" (fun policy -> 
                        policy
                            .WithOrigins("localhost", "http://localhost:8080")
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                        |> ignore
                    )
                    no_router
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
