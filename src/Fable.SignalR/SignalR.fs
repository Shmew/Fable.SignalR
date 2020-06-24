namespace Fable.SignalR

open Fable.Core
open System.ComponentModel

/// A builder for configuring HubConnection instances.
type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
    internal (hub: IHubConnectionBuilder<'ClientApi,'ServerApi>) =

    let mutable hub = hub
    let mutable onMsg : ('ServerApi -> unit) option = None
    let mutable handlers : (HubRegistration -> unit) option = None

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

    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server and client support.
    member this.withUrl (url: string) = 
        hub <- hub.withUrl(url)
        this

    /// Configures the HubConnection to use the specified HTTP-based transport to connect to the specified URL.
    member this.withUrl (url: string, transportType: TransportType) = 
        hub <- hub.withUrl(url, transportType)
        this
    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    member this.withUrl (url: string, options: Http.ConnectionBuilder -> Http.ConnectionBuilder) = 
        hub <- hub.withUrl(url, (Http.ConnectionBuilder() |> options).build())
        this

    /// Configures the HubConnection to use the specified Hub Protocol.
    member this.withHubProtocol (protocol: IHubProtocol<'ClientStreamFromApi,'ServerApi,'ServerStreamApi>) = 
        hub <- hub.withHubProtocol(protocol)
        this

    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts.
    member this.withAutomaticReconnect () = 
        hub <- hub.withAutomaticReconnect()
        this
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect attempt.
    /// The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
    member this.withAutomaticReconnect (retryDelays: int list) = 
        hub <- hub.withAutomaticReconnect(ResizeArray retryDelays)
        this
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    member this.withAutomaticReconnect (reconnectPolicy: RetryPolicy) = 
        hub <- hub.withAutomaticReconnect(reconnectPolicy)
        this

    /// Configures the HubConnection to callback when a new message is recieved.
    member this.onMessage (handler: 'ServerApi -> unit) = 
        onMsg <- Some handler
        this

    /// Configures the HubConnection to callback for certain conditions are met.
    member this.addHandlers (handler: HubRegistration -> unit) =
        handlers <- Some handler
        this

    /// Creates a HubConnection from the configuration options specified in this builder.
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    #if FABLE_COMPILER
    member inline _.build () : HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    #else
    member _.build () : HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    #endif
        let jsonParser = Parser.JsonProtocol()

        {| name = jsonParser.name
           version = jsonParser.version
           transferFormat = jsonParser.transferFormat
           writeMessage = jsonParser.writeMessage
           parseMessages = jsonParser.parseMessages<'ClientStreamFromApi,'ServerApi,'ServerStreamApi> |}
        |> unbox<IHubProtocol<'ClientStreamFromApi,'ServerApi,'ServerStreamApi>>
        |> fun protocol -> hub.withHubProtocol(protocol).build()
        |> fun res -> 
            if onMsg.IsSome then
                res.onMsg onMsg.Value
            if handlers.IsSome then
                res :> HubRegistration
                |> handlers.Value
            res

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

    /// Gets an instance of the NullLogger.
    static member inline NullLogger () = Bindings.signalR.NullLogger()

    /// Creates a new stream implementation to stream items to the server.
    static member inline Subject<'T> () = Bindings.signalR.Subject<'T>()
