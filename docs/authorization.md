# Authorizization for SignalR Endpoints

## Configuration

The main difference between an unauthorized configuration
and one that has authorization is to either setup the 
application-wide authorization within the SignalR settings, 
or add application routing outside of the SignalR pipeline 
yourself via a configuration flag.

### Injecting authorization

You will want to call `app.UseAuthorization()` inside the configuration 
field `AfterUseRouting`.

#### ASP.NET Core/Giraffe

```fsharp
let mySignalRConfig =
    { EndpointPattern = Endpoints.Root
      Send = SignalRHub.send
      Invoke = SignalRHub.invoke 
      Config = 
          { SignalR.Config.Default() with 
                AfterUseRouting = 
                    Some (fun app -> app.UseAuthorization())

                // If you use bearer auth
                EnableBearerAuth = true
                
                EndpointConfig = 
                    Some (fun builder -> builder.RequireAuthorization()) }

// or using the ConfigBuilder
let mySignalRConfig =
    SignalR.ConfigBuilder(Endpoints.Root, SignalRHub.send, SignalRHub.invoke)
        .AfterUseRouting(fun app -> app.UseAuthorization())
        // If you use bearer auth
        .EnableBearerAuth()
        .EndpointConfig(fun builder -> builder.RequireAuthorization())
        .Build()
```

#### Saturn

```fsharp
configure_signalr {
    endpoint Endpoints.Root
    send SignalRHub.send
    invoke SignalRHub.invoke
    // If you use bearer auth
    use_bearer_auth
    with_after_routing (fun app -> app.UseAuthorization())
    with_endpoint_config (fun builder -> builder.RequireAuthorization())
}
```

### Manual Routing

This process is somewhat simple, but decouples the fact that
SignalR needs `app.UseRouting()` in order to function.

#### ASP.NET Core/Giraffe

```fsharp
let mySignalRConfig =
    { EndpointPattern = Endpoints.Root
      Send = SignalRHub.send
      Invoke = SignalRHub.invoke 
      Config = 
          { SignalR.Config.Default() with 
                NoRouting = true

                // If you use bearer auth
                EnableBearerAuth = true

                EndpointConfig = 
                    Some (fun builder -> builder.RequireAuthorization()) }

// or using the ConfigBuilder
let mySignalRConfig =
    SignalR.ConfigBuilder(Endpoints.Root, SignalRHub.send, SignalRHub.invoke)
        .NoRouting()
        // If you use bearer auth
        .EnableBearerAuth()
        .EndpointConfig(fun builder -> builder.RequireAuthorization())
        .Build()
```

#### Saturn

```fsharp
configure_signalr {
    endpoint Endpoints.Root
    send SignalRHub.send
    invoke SignalRHub.invoke
    no_routing
    // If you use bearer auth
    use_bearer_auth
    with_endpoint_config (fun builder -> builder.RequireAuthorization())
}
```

You will need to ensure that you place `app.UseRouting()` **before**
`app.UseAuthorization()` and that both occur **before** the SignalR
configuration takes place.

## On the client

Configuration on the client is quite easy. 

For example when using bearer authentication you will need to 
provide an access token factory to the http builder and that's it!

You need to ensure you enable bearer authentication in the server configuration
as it injects a middleware to handle the query string format that websockets uses
to authenticate. If you do not, your transport **will be downgraded**.

```fsharp
hub.withUrl(Endpoints.Root, fun builder -> builder.accessTokenFactory(myAccessTokenFunction))
    ...
```

## On the .NET client

The above notes apply for the .NET client as well.

```fsharp
hub.WithUrl(Endpoints.Root, fun o -> 
        o.AccessTokenProvider <- fun () -> myAccessTokenFunction |> Async.StartAsTask
    )
    ...
```
