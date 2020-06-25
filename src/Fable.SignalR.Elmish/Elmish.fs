namespace Fable.SignalR

open Elmish
open Fable.Core
open System.ComponentModel

module Elmish =
    [<RequireQualifiedAccess>]
    module Elmish =
        type Hub<'ClientApi,'ServerApi> [<EditorBrowsable(EditorBrowsableState.Never)>] (hub: HubConnection<'ClientApi,unit,unit,'ServerApi,unit>) =
            interface System.IDisposable with
                member this.Dispose () = this.Dispose()
            
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.hub = hub
        
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.cts = new System.Threading.CancellationTokenSource()

            member this.Dispose () =
                this.hub.stopNow()
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
                    this.hub.stopNow()
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
                    this.hub.stopNow()
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
                    this.hub.stopNow()
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
            module Stream =
                module Bidrectional =
                    /// Starts a connection to a SignalR hub with server and client streaming enabled.
                    let inline connect
                        (registerHub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                            -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection = SignalR.connect(config)

                            connection.onMsg(registerMsgs >> dispatch) 

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]
                    
                    /// Starts a connection to a SignalR hub with server and client streaming enabled.
                    let inline connectWith
                        (registerHub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                            -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) 
                        (registerHandlers: HubRegistration -> ('Msg -> unit) -> unit) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                SignalR.connect(config)

                            registerHandlers (connection :> HubRegistration) dispatch

                            connection.onMsg(registerMsgs >> dispatch)

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]

                module ServerToClient =
                    /// Starts a connection to a SignalR hub with server streaming enabled.
                    let inline connect
                        (registerHub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi> 
                            -> HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection = SignalR.connect(config)

                            connection.onMsg(registerMsgs >> dispatch) 

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]
                    
                    /// Starts a connection to a SignalR hub with server streaming enabled.
                    let inline connectWith
                        (registerHub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi> 
                            -> HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>) 
                        (registerHandlers: HubRegistration -> ('Msg -> unit) -> unit) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                SignalR.connect(config)

                            registerHandlers (connection :> HubRegistration) dispatch

                            connection.onMsg(registerMsgs >> dispatch)

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>(connection))
                            |> dispatch ]

                module ClientToServer =
                    /// Starts a connection to a SignalR hub with client streaming enabled.
                    let inline connect
                        (registerHub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit> 
                            -> HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection = SignalR.connect(config)

                            connection.onMsg(registerMsgs >> dispatch) 

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>(connection))
                            |> dispatch ]
                    
                    /// Starts a connection to a SignalR hub with client streaming enabled.
                    let inline connectWith
                        (registerHub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> -> 'Msg) 
                        (registerMsgs: 'ServerApi -> 'Msg)
                        (config: HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit> 
                            -> HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>) 
                        (registerHandlers: HubRegistration -> ('Msg -> unit) -> unit) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let connection =
                                SignalR.connect(config)

                            registerHandlers (connection :> HubRegistration) dispatch

                            connection.onMsg(registerMsgs >> dispatch)

                            connection.startNow()

                            registerHub (new Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>(connection))
                            |> dispatch ]
                            
            /// Returns the base url of the hub connection.
            let baseUrl (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string -> 'Msg) : Cmd<_> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.baseUrl() |> msg |> dispatch) ]
                
            /// Returns the connectionId to the hub of this client.
            let connectionId (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string option -> 'Msg) : Cmd<_> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.connectionId() |> msg |> dispatch) ]
            
            /// Starts a connection to a SignalR hub.
            let inline connect 
                (registerHub: Elmish.Hub<'ClientApi,'ServerApi> -> 'Msg) 
                (registerMsgs: 'ServerApi -> 'Msg)
                (config: HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit> -> HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit>) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let connection = SignalR.connect(config)

                    connection.onMsg(registerMsgs >> dispatch) 

                    connection.startNow()

                    registerHub (new Elmish.Hub<'ClientApi,'ServerApi>(connection))
                    |> dispatch ]
            
            /// Starts a connection to a SignalR hub.
            let inline connectWith
                (registerHub: Elmish.Hub<'ClientApi,'ServerApi> -> 'Msg) 
                (registerMsgs: 'ServerApi -> 'Msg)
                (config: HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit> -> HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit>) 
                (registerHandlers: HubRegistration -> ('Msg -> unit) -> unit) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let connection =
                        SignalR.connect(config)

                    registerHandlers (connection :> HubRegistration) dispatch

                    connection.onMsg(registerMsgs >> dispatch)

                    connection.startNow()

                    registerHub (new Elmish.Hub<'ClientApi,'ServerApi>(connection))
                    |> dispatch ]

            /// Invokes a hub method on the server.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            let invoke (hub: #Elmish.Hub<'ClientApi,'Msg> option) (msg: 'ClientApi) (onError: exn -> 'Msg) =
                match hub with
                | Some hub -> 
                    Cmd.OfAsyncWith.either 
                        (fun msg -> Async.StartImmediate(msg, hub.cts.Token)) 
                        (fun msg -> hub.hub.invoke msg) 
                        msg 
                        id 
                        onError
                | None -> 
                    #if DEBUG
                    JS.console.error("Cannot send a message if hub is not initialized!")
                    #endif
                    [ fun _ -> () ]

            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            let send (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) : Cmd<_> =
                [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.sendNow msg) ]

            /// Returns the state of the Hub connection to the server.
            let state (hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: ConnectionState -> 'Msg) : Cmd<_> =
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.state() |> msg |> dispatch) ]

        [<Erase>]
        type SignalR =
            /// Streams from the hub.
            static member inline streamFrom (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamApi) (subscription: ISubscription -> 'Msg) (subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            hub.hub.streamFrom msg 
                            |> fun rsp -> rsp.subscribe(subscriber dispatch) 
                            |> subscription |> dispatch) ] : Cmd<'Msg>

            /// Streams from the hub.
            static member inline streamFrom (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,_,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamFromApi) (subscription: ISubscription -> 'Msg) (subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            hub.hub.streamFrom msg 
                            |> fun rsp -> rsp.subscribe(subscriber dispatch) 
                            |> subscription |> dispatch) ] : Cmd<'Msg>

            /// Streams to the hub.
            static member inline streamTo (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) =
                fun (subject: #ISubject<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.streamToNow(subject, hub.cts.Token)) ] : Cmd<_>
            /// Streams to the hub.
            static member inline streamTo (hub: Elmish.StreamHub.Bidrectional<'ClientApi,_,'ClientStreamToApi,'ServerApi,_> option) =
                fun (subject: #ISubject<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.streamToNow(subject, hub.cts.Token)) ] : Cmd<_>