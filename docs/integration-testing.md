# Integration Testing

If you plan to run tests that use [jsdom](https://github.com/jsdom/jsdom),
such as with [Fable.Jester](https://github.com/Shmew/Fable.Jester/) there is
some configuration you will want to do so that your test environment can 
properly connect to the server.

You can see a full example of how to do this in the [project repo](https://github.com/Shmew/Fable.SignalR/tree/master/tests).

## Create or modify the CORS policy

The jsdom environment will refuse your connection if this type of policy is
not in place:

```fsharp
application {
    ...
    use_cors "Any" (fun policy -> 
        policy
            .WithOrigins("http://localhost", "http://127.0.0.1:80")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
        |> ignore
    )
    ...
}
```

## Reduce logging (optional)

SignalR can at times be quite verbose, especially if you're running
your tests in the same console as the server.

### On the server

```fsharp
application {
    use_signalr (
        configure_signalr {
            ...
            with_log_level Microsoft.Extensions.Logging.LogLevel.None
        }
    )
    logging (fun l -> l.AddFilter("Microsoft", LogLevel.Error) |> ignore)
    ...
}
```

### On the client

```fsharp
let hub =
    React.useSignalR<Action,Response>(fun hub -> 
        hub ...
            .configureLogging(LogLevel.None)
```
