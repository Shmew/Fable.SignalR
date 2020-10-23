namespace Fable.SignalR

open Fable.Remoting.Json
open Fable.SignalR.Shared
open Microsoft.AspNetCore.Http.Connections
open Microsoft.AspNetCore.Http.Connections.Client
open Microsoft.AspNetCore.SignalR.Client
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Newtonsoft.Json
open System

/// A builder for configuring HubConnection instances.
type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    internal (hub: IHubConnectionBuilder) =

    let mutable hub = hub
    let mutable handlers = Handlers.empty
    let mutable useMsgPack = false
    
    /// Configures logging for the HubConnection.
    member this.ConfigureLogging (f: ILoggingBuilder -> ILoggingBuilder) =
        hub <- hub.ConfigureLogging(Action<ILoggingBuilder> (f >> ignore))
        this

    /// Callback when the connection is closed.
    member this.OnClosed (callback: exn option -> Async<unit>) =
        handlers <- { handlers with OnClosed = Some callback }
        this

    /// Callback when the connection successfully reconnects.
    member this.OnReconnected (callback: string option -> Async<unit>) =
        handlers <- { handlers with OnReconnected = Some callback }
        this

    /// Callback when the connection starts reconnecting.
    member this.OnReconnecting (callback: exn option -> Async<unit>) =
        handlers <- { handlers with OnReconnecting = Some callback }
        this

    /// Enable MessagePack binary (de)serialization instead of JSON.
    member this.UseMessagePack () =
        useMsgPack <- true
        this

    /// Configures the HubConnection to use HTTP-based transports to connect 
    /// to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server 
    /// and client support.
    member this.WithUrl (url: string) =
        hub <- hub.WithUrl(url)
        this

    /// Configures the HubConnection to use HTTP-based transports to connect 
    /// to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server 
    /// and client support.
    member this.WithUrl (url: Uri) =
        hub <- hub.WithUrl(url)
        this

    /// Configures the HubConnection to use the specified HTTP-based transport
    /// to connect to the specified URL.
    member this.WithUrl (url: string, transportType: HttpTransportType) =
        hub <- hub.WithUrl(url, transportType)
        this

    /// Configures the HubConnection to use HTTP-based transports to connect to
    /// the specified URL.
    member this.WithUrl (url: string, options: HttpConnectionOptions -> unit) =
        hub <- hub.WithUrl(url, options)
        this
    
    /// Configures the HubConnection to use HTTP-based transports to connect to
    /// the specified URL.
    member this.WithUrl (url: Uri, options: HttpConnectionOptions -> unit) =
        hub <- hub.WithUrl(url, options)
        this

    /// Configures the HubConnection to use the specified Hub Protocol.
    member this.WithServices (f: IServiceCollection -> unit) =
        f hub.Services
        this
    
    /// Configures the HubConnection to use the specified Hub Protocol.
    member this.WithServices (f: IServiceCollection -> IServiceCollection) =
        f hub.Services |> ignore
        this

    /// Configures the HubConnection to automatically attempt to reconnect 
    /// if the connection is lost.
    /// 
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively 
    /// before trying up to 4 reconnect attempts.
    member this.WithAutomaticReconnect () = 
        hub <- hub.WithAutomaticReconnect()
        this

    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect 
    /// attempt. The length of the array represents how many failed reconnect attempts 
    /// it takes before the client will stop attempting to reconnect.
    member this.WithAutomaticReconnect (retryDelays: seq<TimeSpan>) =
        hub <- hub.WithAutomaticReconnect(Array.ofSeq retryDelays)
        this

    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    member this.WithAutomaticReconnect (reconnectPolicy: IRetryPolicy) =
        hub <- hub.WithAutomaticReconnect(reconnectPolicy)
        this

    member internal _.Build () =
        if useMsgPack then 
            hub.Services.AddSingleton<IHubProtocol,MsgPackProtocol.ClientFableHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>()
            |> ignore

            hub
        else 
            hub.AddNewtonsoftJsonProtocol(fun o -> 
                o.PayloadSerializerSettings.DateParseHandling <- DateParseHandling.None
                o.PayloadSerializerSettings.ContractResolver <- new Serialization.DefaultContractResolver()
                o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
        |> fun hub -> IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.Build())
        |> fun hub -> new HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub, handlers)

type SignalR =
    /// Starts a connection to a SignalR hub.
    static member Connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
        (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
            -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(HubConnectionBuilder()) 
        |> config 
        |> fun hubBuilder -> hubBuilder.Build()


[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fable.SignalR.DotNet.Elmish")>]
do ()
