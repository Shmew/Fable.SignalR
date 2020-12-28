# API Reference

One thing to note: `Fable.SignalR.Saturn` is a superset of `Fable.SignalR.AspNetCore`,
so only those designated as being Saturn-only are available with both libraries.

## FableHub

The `FableHub` is your interface for interacting with the SignalR hub.

There are three types of `FableHub`:
 * `FableHub` 
   - A generic hub context that is unable to dispatch messages.
   - Provided for hub *invocations*.
 * `FableHub<'ClientApi,'ServerApi>` 
   - The full hub context that can dispatch messages to users.
 * `FableHubCaller`
   - The external hub context.
   - Used to call hub members externally.

### IFableHubCallerClients

The interface that defines the actions a hub context can make.

For the most part you don't need to know that this exists, as the framework handles this for you.

Signature:
```fsharp
type IFableHubCallerClients<'ServerApi> =
    abstract Send: 'ServerApi -> Task
    abstract Invoke: {| connectionId: string; invocationId: System.Guid; message: 'ServerApi |} -> Task
```

### Generic FableHub

A generic hub context that is unable to dispatch messages for hub *invocations*.

This does not let you dispatch messages because an invocation is a traditional RPC
abstraction over the SignalR hub.

Signature:
```fsharp
type FableHub =
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Dispose : unit -> unit
    abstract Services : System.IServiceProvider
```

### FableHub<'ClientApi,'ServerApi>

The full hub context that can dispatch messages to users.

Signature:
```fsharp
type FableHub<'ClientApi,'ServerApi> =
    abstract Clients : IHubCallerClients<IFableHubCallerClients<'ServerApi>>
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Dispose : unit -> unit
    abstract Services : System.IServiceProvider
```

### FableHubCaller

The external hub context that can be required via DI to call your hub
from an external source.

Signature:
```fsharp
type FableHubCaller<'ClientApi,'ServerApi> =
    abstract Clients : IHubClients<IFableHubCallerClients<'ServerApi>>
    abstract Groups : IGroupManager
```

## SignalR.Config

Configuration options for customizing behavior of a SignalR hub.

Signature:
```fsharp
type Config<'ClientApi,'ServerApi> =
    { /// App configuration after app.UseRouting() is called.
      AfterUseRouting: (IApplicationBuilder -> IApplicationBuilder) option
      
      /// App configuration before app.UseRouting() is called.
      BeforeUseRouting: (IApplicationBuilder -> IApplicationBuilder) option
      
      /// Inject a Websocket middleware to support bearer tokens.
      ///
      /// Default: false
      EnableBearerAuth: bool 
    
      /// Customize hub endpoint conventions.
      EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option

      /// Options used to configure hub instances.
      HubOptions: (HubOptions -> unit) option

      /// Adds a logging filter with the given LogLevel.
      LogLevel: Microsoft.Extensions.Logging.LogLevel option

      /// Disable app.UseRouting() configuration from this library.
      ///
      /// *You must configure this yourself if you do this!*
      ///
      /// Default: false
      NoRouting: bool

      /// Called when a new connection is established with the hub.
      OnConnected: (FableHub<'ClientApi,'ServerApi> -> Task<unit>) option

      /// Called when a connection with the hub is terminated.
      OnDisconnected: (exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) option

      /// Enable MessagePack binary (de)serialization instead of JSON.
      UseMessagePack: bool

      /// Configure the SignalR server.
      UseServerBuilder: (ISignalRServerBuilder -> ISignalRServerBuilder) option }

    /// Creates an empty record.
    static member Default () : Config<'ClientApi,'ServerApi>
```

## SignalR.Settings

SignalR hub settings.

Signature:
```fsharp
type Settings<'ClientApi,'ServerApi when 'ClientApi> =
    { /// The endpoint used to communicate with the hub.
      EndpointPattern: string

      /// Handler for client message sends.
      Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task

      /// Handler for client invocations.
      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>

      /// Optional hub configuration.
      Config: Config<'ClientApi,'ServerApi> option }
```

## SignalR.ConfigBuilder

A fluent builder for the [config](#signalrconfig).

Signature:
```fsharp
type ConfigBuilder<'ClientApi,'ServerApi>
    (endpoint: string, 
     send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task,
     invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
     ?config: Config<'ClientApi,'ServerApi>)

type ConfigBuilder<'ClientApi,'ServerApi> (settings: Settings<'ClientApi,'ServerApi>)

    /// App configuration after app.UseRouting() is called.
    member AfterUseRouting (appConfig: IApplicationBuilder -> IApplicationBuilder)
        : ConfigBuilder
    
    /// App configuration after app.UseRouting() is called.
    member AfterUseRouting (appConfig: IApplicationBuilder -> IApplicationBuilder)
        : ConfigBuilder

    /// App configuration before app.UseRouting() is called.
    member BeforeUseRouting (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) 
        : ConfigBuilder

    /// Inject a Websocket middleware to support bearer tokens.
    member EnableBearerAuth () : ConfigBuilder

    /// Options used to configure hub instances.
    member HubOptions (f: HubOptions -> unit) : ConfigBuilder

    /// Adds a logging filter with the given LogLevel.
    member LogLevel (logLevel: Microsoft.Extensions.Logging.LogLevel) : ConfigBuilder

    /// Disable app.UseRouting() configuration.
    ///
    /// *You must configure this yourself if you do this!*
    member this.NoRouting () : ConfigBuilder

    /// Called when a new connection is established with the hub.
    member OnConnected (f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) : ConfigBuilder

    /// Called when a connection with the hub is terminated.
    member OnDisconnected (f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) : ConfigBuilder

    /// Enable MessagePack binary (de)serialization instead of JSON.
    member UseMessagePack () : ConfigBuilder

    /// Configure the SignalR server.
    member UseServerBuilder (handler: ISignalRServerBuilder -> ISignalRServerBuilder) : ConfigBuilder

    /// Returns the SignalR settings.
    member Build () : SignalR.Settings
```

## configure_signalr

<Note type="warning">Saturn only</Note>

Computation expression to build a configuration to feed into
the Saturn `use_signalr` operation.

Has the following operations:
```fsharp
configure_signalr {
    /// The endpoint used to communicate with the hub.
    endpoint: string

    /// Handler for client message sends.
    send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task

    /// Handler for client invocations.
    invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
    
    /// Handler for streaming to the client.
    stream_from: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
        -> IAsyncEnumerable<'ServerStreamApi>
        
    ///  Handler for streaming from the client.
    stream_to: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task
    
    /// Disable app.UseRouting() configuration.
    ///
    /// *You must configure this yourself if you do this!*
    no_routing

    /// Inject a Websocket middleware to support bearer tokens.
    use_bearer_auth

    /// Enable MessagePack binary (de)serialization instead of JSON.
    use_messagepack

    /// Configure the SignalR server.
    use_server_builder: ISignalRServerBuilder -> ISignalRServerBuilder

    /// App configuration after app.UseRouting() is called.
    with_after_routing: IApplicationBuilder -> IApplicationBuilder

    /// App configuration before app.UseRouting() is called.
    with_before_routing: IApplicationBuilder -> IApplicationBuilder

    /// Customize hub endpoint conventions.
    with_endpoint_config: HubEndpointConventionBuilder -> HubEndpointConventionBuilder
    
    /// Options used to configure hub instances.
    with_hub_options: HubOptions -> unit
    
    /// Adds a logging filter with the given LogLevel.
    with_log_level: Microsoft.Extensions.Logging.LogLevel
    
    /// Called when a new connection is established with the hub.
    with_on_connected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
    
    /// Called when a connection with the hub is terminated.
    with_on_disconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
}

```

# Type Extensions

## IHostBuilder.SignalRLogLevel

Adds a logging filter for SignalR with the given log level threshold.

Signature:
```fsharp
(logLevel: Microsoft.Extensions.Logging.LogLevel) -> IHostBuilder
(settings: SignalR.Settings<'ClientApi,'ServerApi>) -> IHostBuilder
```

## IServiceCollection.AddSignalR

Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.

Signature:
```fsharp
(settings: SignalR.Settings<'ClientApi,'ServerApi>) -> IServiceCollection

(settings: SignalR.Settings<'ClientApi,'ServerApi>, 
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>) -> IServiceCollection

(settings: SignalR.Settings<'ClientApi,'ServerApi>, 
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(settings: SignalR.Settings<'ClientApi,'ServerApi>, 
 streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(endpoint: string, 
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task, 
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
 streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection
```

## IApplicationBuilder.UseSignalR

Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.

Signature:
```fsharp
(settings: SignalR.Settings<'ClientApi,'ServerApi>) -> IApplicationBuilder

(settings: SignalR.Settings<'ClientApi,'ServerApi>, 
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>) -> IApplicationBuilder

(settings: SignalR.Settings<'ClientApi,'ServerApi>,
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IApplicationBuilder

(settings: SignalR.Settings<'ClientApi,'ServerApi>, 
 streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IApplicationBuilder
```

## Application.ApplicationBuilder

<Note type="warning">Saturn only</Note>

Extends the Saturn `application` computation expression to add the `use_signalr`
custom operation.
