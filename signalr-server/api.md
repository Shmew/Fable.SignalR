# API Reference

One thing to note: `Fable.SignalR.Saturn` is a superset of `Fable.SignalR.AspNetCore`,
so only those designated as being Saturn-only are available with both libraries.

## FableHub

The `FableHub` is your interface for interacting with the SignalR hub.

Signature:
```fsharp
type FableHub<'ClientApi,'ServerApi> =
    abstract Clients : IHubCallerClients<IFableHubCallerClients<'ServerApi>>
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Dispose : unit -> unit
    abstract Services : System.IServiceProvider
```

## SignalR.Config

Configuration options for customizing behavior of a SignalR hub.

Signature:
```fsharp
type Config<'ClientApi,'ServerApi> =
    { /// Customize hub endpoint conventions.
      EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option

      /// Options used to configure hub instances.
      HubOptions: (HubOptions -> unit) option

      /// Adds a logging filter with the given LogLevel.
      LogLevel: Microsoft.Extensions.Logging.LogLevel option

      /// Called when a new connection is established with the hub.
      OnConnected: (FableHub<'ClientApi,'ServerApi> -> Task<unit>) option

      /// Called when a connection with the hub is terminated.
      OnDisconnected: (exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) option }

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
      Invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi

      /// Optional hub configuration.
      Config: Config<'ClientApi,'ServerApi> option }
```

## SignalR.ConfigBuilder

A fluent builder for the [config](#signalrconfig).

Signature:
```fsharp
type ConfigBuilder<'ClientApi,'ServerApi> =
    /// Customize hub endpoint conventions.
    member EndpointConfig (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) 
        : ConfigBuilder

    /// Options used to configure hub instances.
    member HubOptions (f: HubOptions -> unit) : ConfigBuilder

    /// Adds a logging filter with the given LogLevel.
    member LogLevel (logLevel: Microsoft.Extensions.Logging.LogLevel) : ConfigBuilder

    /// Called when a new connection is established with the hub.
    member OnConnected (f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) : ConfigBuilder

    /// Called when a connection with the hub is terminated.
    member OnDisconnected (f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) : ConfigBuilder
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
    invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi
    
    /// Handler for streaming to the client.
    stream_from: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
        -> IAsyncEnumerable<'ServerStreamApi>
        
    ///  Handler for streaming from the client.
    stream_to: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task
    
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
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) 
    -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> 
    -> IAsyncEnumerable<'ServerStreamApi>,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
 streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
 config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> 
    -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) -> IServiceCollection

(endpoint: string,
 update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
 invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
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
