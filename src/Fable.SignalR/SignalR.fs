namespace Fable.SignalR

open Fable.Core
open HubInterfaces
open System.ComponentModel

/// A builder for configuring HubConnection instances.
type HubConnectionBuilder<'ClientApi,'ServerApi> [<EditorBrowsable(EditorBrowsableState.Never)>] (hub: IHubConnectionBuilder<'ClientApi,'ServerApi>) =
    /// Configures console logging for the HubConnection.
    member _.configureLogging (logLevel: LogLevel) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.configureLogging(logLevel) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures custom logging for the HubConnection.
    member _.configureLogging (logger: ILogger) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.configureLogging(logger) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures custom logging for the HubConnection.
    member _.configureLogging (logLevel: string) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.configureLogging(logLevel) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>

    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server and client support.
    member _.withUrl (url: string) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withUrl(url) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures the HubConnection to use the specified HTTP-based transport to connect to the specified URL.
    member _.withUrl (url: string, transportType: TransportType) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withUrl(url, transportType) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures the HubConnection to use HTTP-based transports to connect to the specified URL.
    member _.withUrl (url: string, options: Http.ConnectionOptions -> Http.ConnectionOptions) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withUrl(url, Http.ConnectionOptions.Create() |> options |> unbox<Http.IConnectionOptions>) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>

    /// Configures the HubConnection to use the specified Hub Protocol.
    member _.withHubProtocol (protocol: IHubProtocol<'ClientApi,'ServerApi>) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withHubProtocol(protocol) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>

    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively before trying up to 4 reconnect attempts.
    member _.withAutomaticReconnect () : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withAutomaticReconnect() 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect attempt.
    /// The length of the array represents how many failed reconnect attempts it takes before the client will stop attempting to reconnect.
    member _.withAutomaticReconnect (retryDelays: int list) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withAutomaticReconnect(ResizeArray retryDelays) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>
    /// Configures the HubConnection to automatically attempt to reconnect if the connection is lost.
    member _.withAutomaticReconnect (reconnectPolicy: IRetryPolicy) : HubConnectionBuilder<'ClientApi,'ServerApi> = 
        hub.withAutomaticReconnect(reconnectPolicy) 
        |> HubConnectionBuilder<'ClientApi,'ServerApi>

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
           parseMessages = jsonParser.parseMessages<'ClientApi,'ServerApi> |}
        |> unbox<IHubProtocol<'ClientApi,'ServerApi>>
        |> fun protocol ->
            hub.withHubProtocol(protocol).build()
        |> HubConnection<'ClientApi,'ServerApi>

[<Erase>]
type SignalR =
    #if FABLE_COMPILER
    static member inline connect<'ClientApi,'ServerApi> (config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>) =
    #else
    static member connect<'ClientApi,'ServerApi> (config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>) =
    #endif
        HubConnectionBuilder<'ClientApi,'ServerApi>(Bindings.signalR.HubConnectionBuilder()) 
        |> config 
        |> fun hubBuilder -> hubBuilder.build()

    static member inline NullLogger () = Bindings.signalR.NullLogger()
