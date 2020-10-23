namespace Fable.SignalR

open Fable.Core
open System.ComponentModel

/// A builder for configuring HubConnection instances.
type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    internal (hub: IHubConnectionBuilder<'ClientApi,'ServerApi>) =

    let mutable hub = hub
    let mutable handlers = Handlers.empty
    let mutable useMsgPack = false

    /// Configures console logging for the HubConnection.
    member this.configureLogging (logLevel: LogLevel) = 
        hub <- hub.configureLogging(logLevel)
        this
    /// Configures custom logging for the HubConnection.
    member this.configureLogging (logger: ILogger) = 
        hub <- hub.configureLogging(logger)
        this
    /// Configures custom logging for the HubConnection.
    member this.configureLogging (logLevel: string) = 
        hub <- hub.configureLogging(logLevel)
        this

    /// Callback when the connection is closed.
    member this.onClose (callback: exn option -> unit) =
        handlers <- { handlers with onClose = Some callback }
        this

    /// Callback when a new message is recieved.
    member this.onMessage (callback: 'ServerApi -> unit) = 
        handlers <- { handlers with onMessage = Some (unbox callback) }
        this
    
    /// Callback when the connection successfully reconnects.
    member this.onReconnected (callback: string option -> unit) =
        handlers <- { handlers with onReconnected = Some callback }
        this

    /// Callback when the connection starts reconnecting.
    member this.onReconnecting (callback: exn option -> unit) =
        handlers <- { handlers with onReconnecting = Some callback }
        this

    /// Enable MessagePack binary (de)serialization instead of JSON.
    member this.useMessagePack () =
        useMsgPack <- true
        this

    /// Configures the HubConnection to use HTTP-based transports to connect 
    /// to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server 
    /// and client support.
    member this.withUrl (url: string) = 
        hub <- hub.withUrl(url)
        this

    /// Configures the HubConnection to use the specified HTTP-based transport
    /// to connect to the specified URL.
    member this.withUrl (url: string, transportType: TransportType) = 
        hub <- hub.withUrl(url, transportType)
        this

    /// Configures the HubConnection to use HTTP-based transports to connect to
    /// the specified URL.
    member this.withUrl (url: string, options: Http.ConnectionBuilder -> Http.ConnectionBuilder) = 
        hub <- hub.withUrl(url, (Http.ConnectionBuilder() |> options).build())
        this

    /// Configures the HubConnection to use the specified Hub Protocol.
    member this.withHubProtocol (protocol: IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) = 
        hub <- hub.withHubProtocol(protocol)
        this

    /// Configures the HubConnection to automatically attempt to reconnect 
    /// if the connection is lost.
    /// 
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively 
    /// before trying up to 4 reconnect attempts.
    member this.withAutomaticReconnect () = 
        hub <- hub.withAutomaticReconnect()
        this

    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect 
    /// attempt. The length of the array represents how many failed reconnect attempts 
    /// it takes before the client will stop attempting to reconnect.
    member this.withAutomaticReconnect (retryDelays: int list) = 
        hub <- hub.withAutomaticReconnect(ResizeArray retryDelays)
        this

    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    member this.withAutomaticReconnect (reconnectPolicy: RetryPolicy) = 
        hub <- hub.withAutomaticReconnect(reconnectPolicy)
        this

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    #if FABLE_COMPILER
    member inline _.build () : HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    #else
    member _.build () : HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    #endif
        if useMsgPack then Protocol.MsgPack.create<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>()
        else Protocol.Json.create()
        |> fun protocol -> hub.withHubProtocol(protocol).build()
        |> fun hub -> new HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub, handlers)

[<Erase>]
type SignalR =
    /// Starts a connection to a SignalR hub.
    #if FABLE_COMPILER
    static member inline connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    #else
    static member connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    #endif
        (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
            -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

        HubConnectionBuilder(Bindings.signalR.HubConnectionBuilder()) 
        |> config 
        |> fun hubBuilder -> hubBuilder.build()

    /// Creates the default http client.
    #if FABLE_COMPILER
    static member inline httpClient (logger: ILogger) = 
    #else
    static member httpClient (logger: ILogger) = 
    #endif
        Bindings.signalR.HttpClient(logger)

    /// Creates an ILogger from a logging function.
    static member inline logger (handler: LogLevel -> string -> unit) =
        {| log = handler |}
        |> unbox<ILogger>

    /// Gets an instance of the NullLogger.
    #if FABLE_COMPILER
    static member inline nullLogger () = 
    #else
    static member nullLogger () = 
    #endif
        Bindings.signalR.NullLogger()

    /// Creates a new stream implementation to stream items to the server.
    #if FABLE_COMPILER
    static member inline subject<'T> () = 
    #else
    static member subject<'T> () = 
    #endif
        Bindings.signalR.Subject<'T>()

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fable.SignalR.Elmish")>]
do ()
