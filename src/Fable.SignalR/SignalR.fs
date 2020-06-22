namespace Fable.SignalR

open Fable.Core
open HubInterfaces
open System.ComponentModel

/// A builder for configuring HubConnection instances.
type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> [<EditorBrowsable(EditorBrowsableState.Never)>] 
    (hub: IHubConnectionBuilder<'ClientApi,_,_,'ServerApi,_>, onMsg: ('ServerApi -> unit) option, handlers: (HubRegistration -> unit) option) =

    /// Configures console logging for the HubConnection.
    member _.configureLogging (logLevel: LogLevel) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.configureLogging(logLevel), onMsg, handlers)
    /// Configures custom logging for the HubConnection.
    member _.configureLogging (logger: ILogger) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.configureLogging(logger), onMsg, handlers)
    /// Configures custom logging for the HubConnection.
    member _.configureLogging (logLevel: string) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.configureLogging(logLevel), onMsg, handlers)

    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server and client support.
    member _.withUrl (url: string) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withUrl(url), onMsg, handlers)
    /// Configures the HubConnection to use the specified HTTP-based transport to connect to the specified URL.
    member _.withUrl (url: string, transportType: TransportType) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withUrl(url, transportType), onMsg, handlers)
    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    member _.withUrl (url: string, options: Http.ConnectionOptions -> Http.ConnectionOptions) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        hub.withUrl(url, Http.ConnectionOptions.Create() |> options |> unbox<Http.IConnectionOptions>) 
        |> fun res -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(res, onMsg, handlers)

    /// Configures the HubConnection to use the specified Hub Protocol.
    member _.withHubProtocol (protocol: IHubProtocol<'ClientStreamFrom,'ServerApi,'ServerStreamApi>) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withHubProtocol(protocol), onMsg, handlers)

    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts.
    member _.withAutomaticReconnect () : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withAutomaticReconnect(), onMsg, handlers)
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect attempt.
    /// The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
    member _.withAutomaticReconnect (retryDelays: int list) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withAutomaticReconnect(ResizeArray retryDelays), onMsg, handlers)
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    member _.withAutomaticReconnect (reconnectPolicy: IRetryPolicy) : HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub.withAutomaticReconnect(reconnectPolicy), onMsg, handlers)

    member _.onMessage (handler: 'ServerApi -> unit) = 
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub, (Some handler), handlers)

    member _.addHandlers (handler: HubRegistration -> unit) =
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(hub, onMsg, (Some handler))

    /// Creates a HubConnection from the configuration options specified in this builder.
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    #if FABLE_COMPILER
    member inline _.build () =
    #else
    member _.build () =
    #endif
        let jsonParser = Parser.SimpleJsonProtocol()

        {| name = jsonParser.name
           version = jsonParser.version
           transferFormat = jsonParser.transferFormat
           writeMessage = jsonParser.writeMessage
           parseMessages = jsonParser.parseMessages<'ClientStreamFromApi,'ServerApi,'ServerStreamApi> |}
        |> unbox<IHubProtocol<'ClientStreamFromApi,'ServerApi,'ServerStreamApi>>
        |> fun protocol ->
            hub.withHubProtocol(protocol).build()
        |> HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
        |> fun res -> 
            if onMsg.IsSome then
                res.onMsg onMsg.Value
            if handlers.IsSome then
                res :> HubRegistration
                |> handlers.Value
            res

[<Erase>]
type SignalR =
    #if FABLE_COMPILER
    static member inline connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =
    #else
    static member connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =
    #endif
        HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(Bindings.signalR.HubConnectionBuilder(), None, None) 
        |> config 
        |> fun hubBuilder -> hubBuilder.build()

    static member inline NullLogger () = Bindings.signalR.NullLogger()
