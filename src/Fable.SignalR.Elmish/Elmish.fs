namespace Fable.SignalR

open Elmish
open Fable.Core
open System.ComponentModel

module Elmish =
    [<RequireQualifiedAccess>]
    module Elmish =
        type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg> 
            internal (hub: Fable.SignalR.IHubConnectionBuilder<'ClientApi,'ServerApi>, dispatch: 'Msg -> unit) =
            
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
            member this.onClose (callback: exn option -> 'Msg) =
                handlers <- { handlers with onClose = Some (callback >> dispatch) }
                this

            /// Configures the HubConnection to callback when a new message is recieved.
            member this.onMessage (callback: 'ServerApi -> 'Msg) = 
                handlers <- { handlers with onMessage = Some (unbox (callback >> dispatch)) }
                this
            
            /// Registers a handler that will be invoked when the connection successfully reconnects.
            member this.onReconnected (callback: (string option -> 'Msg)) =
                handlers <- { handlers with onReconnected = Some (callback >> dispatch) }
                this

            /// Registers a handler that will be invoked when the connection starts reconnecting.
            member this.onReconnecting (callback: (exn option -> 'Msg)) =
                handlers <- { handlers with onReconnecting = Some (callback >> dispatch) }
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

        type Hub<'ClientApi,'ServerApi> [<EditorBrowsable(EditorBrowsableState.Never)>] (hub: HubConnection<'ClientApi,unit,unit,'ServerApi,unit>) =
            interface System.IDisposable with
                member this.Dispose () = this.Dispose()
            
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.hub = hub
        
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.cts = new System.Threading.CancellationTokenSource()

            member this.Dispose () =
                (hub :> System.IDisposable).Dispose()
                this.cts.Cancel()
                this.cts.Dispose()

            /// Default interval at which to ping the server.
            /// 
            /// The default value is 15,000 milliseconds (15 seconds).
            /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
            member this.keepAliveInterval = this.hub.keepAliveInterval

            /// The server timeout in milliseconds.
            /// 
            /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
            /// The default timeout value is 30,000 milliseconds (30 seconds).
            member this.serverTimeout = this.hub.serverTimeout
        
        module StreamHub =
            type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit Hub<'ClientApi,'ServerApi>(unbox hub)
                
                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()

                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> System.IDisposable).Dispose()
                    this.cts.Cancel()
                    this.cts.Dispose()

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.keepAliveInterval = this.hub.keepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.serverTimeout = this.hub.serverTimeout

            type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>) =

                inherit Hub<'ClientApi,'ServerApi>(unbox hub)
        
                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()

                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> System.IDisposable).Dispose()
                    this.cts.Cancel()
                    this.cts.Dispose()

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.keepAliveInterval = this.hub.keepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.serverTimeout = this.hub.serverTimeout

            type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>) =
                
                inherit Hub<'ClientApi,'ServerApi>(unbox hub)

                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> System.IDisposable).Dispose()
                    this.cts.Cancel()
                    this.cts.Dispose()

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.keepAliveInterval = this.hub.keepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.serverTimeout = this.hub.serverTimeout

    [<RequireQualifiedAccess>]
    module Cmd =
        [<RequireQualifiedAccess>]
        module SignalR =
            [<Erase>]
            module Stream =
                [<Erase>]
                module Bidrectional =
                    /// Starts a connection to a SignalR hub with server and client streaming enabled.
                    #if FABLE_COMPILER
                    let inline connect
                    #else
                    let connect
                    #endif
                        (registerHub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (config: Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                Elmish.HubConnectionBuilder(Bindings.signalR.HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.build()

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]
                
                [<Erase>]
                module ServerToClient =
                    /// Starts a connection to a SignalR hub with server streaming enabled.
                    #if FABLE_COMPILER
                    let inline connect
                    #else
                    let connect
                    #endif
                        (registerHub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> -> 'Msg)
                        (config: Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                Elmish.HubConnectionBuilder(Bindings.signalR.HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.build()

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]
                    
                [<Erase>]
                module ClientToServer =
                    /// Starts a connection to a SignalR hub with client streaming enabled.
                    #if FABLE_COMPILER
                    let inline connect
                    #else
                    let connect
                    #endif
                        (registerHub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> -> 'Msg)
                        (config: Elmish.HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                Elmish.HubConnectionBuilder(Bindings.signalR.HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.build()

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>(connection))
                            |> dispatch ]
                                                
            /// Returns the base url of the hub connection.
            let baseUrl (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string -> 'Msg) : Cmd<'Msg> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.baseUrl |> msg |> dispatch) ]
                
            /// Returns the connectionId to the hub of this client.
            let connectionId (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string option -> 'Msg) : Cmd<'Msg> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.connectionId |> msg |> dispatch) ]
            
            /// Starts a connection to a SignalR hub.
            #if FABLE_COMPILER
            let inline connect
            #else
            let connect
            #endif
                (registerHub: Elmish.Hub<'ClientApi,'ServerApi> -> 'Msg)
                (config: Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg> 
                    -> Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg>) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let connection =
                        Elmish.HubConnectionBuilder(Bindings.signalR.HubConnectionBuilder(), dispatch) 
                        |> config 
                        |> fun hubBuilder -> hubBuilder.build()

                    connection.startNow()

                    registerHub (new Elmish.Hub<'ClientApi,'ServerApi>(connection))
                    |> dispatch ]
            
            /// Invokes a hub method on the server and maps the error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            let attempt (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) (onError: exn -> 'Msg) =
                match hub with
                | Some hub ->
                    Cmd.OfAsyncWith.attempt 
                        (fun msg -> Async.StartImmediate(msg, hub.cts.Token)) 
                        hub.hub.invoke 
                        msg
                        onError
                | None -> 
                    #if DEBUG
                    JS.console.error("Cannot send a message if hub is not initialized!")
                    #endif
                    [ ignore ]

            /// Invokes a hub method on the server and maps the success or error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            let either (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) (onError: exn -> 'Msg) =
                match hub with
                | Some hub ->
                    Cmd.OfAsyncWith.either 
                        (fun msg -> Async.StartImmediate(msg, hub.cts.Token)) 
                        hub.hub.invoke 
                        msg 
                        onSuccess
                        onError
                | None -> 
                    #if DEBUG
                    JS.console.error("Cannot send a message if hub is not initialized!")
                    #endif
                    [ ignore ]

            /// Invokes a hub method on the server and maps the success.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            let perform (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) =
                match hub with
                | Some hub ->
                    Cmd.OfAsyncWith.perform 
                        (fun msg -> Async.StartImmediate(msg, hub.cts.Token)) 
                        hub.hub.invoke 
                        msg
                        onSuccess
                | None -> 
                    #if DEBUG
                    JS.console.error("Cannot send a message if hub is not initialized!")
                    #endif
                    [ ignore ]

            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            let send (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) : Cmd<'Msg> =
                [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.sendNow msg) ]

            /// Returns the state of the Hub connection to the server.
            let state (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: ConnectionState -> 'Msg) : Cmd<'Msg> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.state |> msg |> dispatch) ]

        [<Erase>]
        type SignalR =
            /// Streams from the hub.
            static member inline streamFrom (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamApi) (subscription: System.IDisposable -> 'Msg) (subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            async {
                                let! streamResult = hub.hub.streamFrom msg 
                                
                                streamResult.subscribe(subscriber dispatch)
                                |> subscription |> dispatch
                            }
                            |> fun a -> Async.StartImmediate(a, hub.cts.Token)) ] : Cmd<'Msg>

            /// Streams from the hub.
            static member inline streamFrom (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,_,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamFromApi) (subscription: System.IDisposable -> 'Msg) (subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            async {
                                let! streamResult = hub.hub.streamFrom msg 
                                
                                streamResult.subscribe(subscriber dispatch) 
                                |> subscription |> dispatch
                            }
                            |> fun a -> Async.StartImmediate(a, hub.cts.Token)) ] : Cmd<'Msg>

            /// Streams to the hub.
            static member inline streamTo (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) =
                fun (subject: #ISubject<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.streamToNow(subject)) ] : Cmd<_>

            /// Streams to the hub.
            static member inline streamTo (hub: Elmish.StreamHub.Bidrectional<'ClientApi,_,'ClientStreamToApi,'ServerApi,_> option) =
                fun (subject: #ISubject<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.streamToNow(subject)) ] : Cmd<_>
