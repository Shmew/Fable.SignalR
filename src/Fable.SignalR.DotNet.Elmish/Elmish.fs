namespace Fable.SignalR

open Elmish
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
open System.Collections.Generic

[<RequireQualifiedAccess>]
module private Async =
    let map f a =
        async {
            let! res = a
            return f res
        }

module Elmish =
    [<RequireQualifiedAccess>]
    module Elmish =
        type HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg> 
            internal (hub: IHubConnectionBuilder, dispatch: 'Msg -> unit) =
            
            let mutable hub = hub
            let mutable handlers = Handlers.empty
            let mutable onMessage : ('ServerApi -> unit) option = None
            let mutable useMsgPack = false

            /// Configures logging for the HubConnection.
            member this.ConfigureLogging (f: ILoggingBuilder -> ILoggingBuilder) =
                hub <- hub.ConfigureLogging(Action<ILoggingBuilder> (f >> ignore))
                this

            /// Callback when the connection is closed.
            member this.OnClosed (callback: exn option -> Async<'Msg>) =
                handlers <- { handlers with OnClosed = Some (callback >> Async.map dispatch) }
                this

            /// Configures the HubConnection to callback when a new message is recieved.
            member this.OnMessage (callback: 'ServerApi -> 'Msg) = 
                onMessage <- Some (callback >> dispatch)
                this

            /// Callback when the connection successfully reconnects.
            member this.OnReconnected (callback: string option -> Async<'Msg>) =
                handlers <- { handlers with OnReconnected = Some (callback >> Async.map dispatch) }
                this

            /// Callback when the connection starts reconnecting.
            member this.OnReconnecting (callback: exn option -> Async<'Msg>) =
                handlers <- { handlers with OnReconnecting = Some (callback >> Async.map dispatch) }
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
                |> fun hub -> hub, onMessage

        type Hub<'ClientApi,'ServerApi> internal (hub: HubConnection<'ClientApi,unit,unit,'ServerApi,unit>, ?onMsg: 'ServerApi -> unit) =
            let onMsgDisposable = 
                match onMsg with
                | Some onMsg -> 
                    hub.OnMessage(fun msg -> async { return onMsg msg })
                    |> Some
                | None -> None

            interface IDisposable with
                member this.Dispose () = this.Dispose()
            
            member internal _.Hub = hub
        
            member internal _.Cts = new System.Threading.CancellationTokenSource()

            member this.Dispose () =
                (hub :> IDisposable).Dispose()
                this.Cts.Cancel()
                this.Cts.Dispose()
                onMsgDisposable |> Option.iter (fun d -> d.Dispose())

            /// Default interval at which to ping the server.
            /// 
            /// The default value is 15,000 milliseconds (15 seconds).
            /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
            member this.KeepAliveInterval = this.Hub.KeepAliveInterval

            /// The server timeout in milliseconds.
            /// 
            /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
            /// The default timeout value is 30,000 milliseconds (30 seconds).
            member this.ServerTimeout = this.Hub.ServerTimeout
        
        module StreamHub =
            type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                internal (hub: HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>, ?onMsg: 'ServerApi -> unit) =
                
                let onMsgDisposable = 
                    match onMsg with
                    | Some onMsg -> 
                        hub.OnMessage(fun msg -> async { return onMsg msg })
                        |> Some
                    | None -> None

                interface IDisposable with
                    member this.Dispose () = this.Dispose()
                    
                member internal _.Hub = hub
        
                member internal _.Cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> IDisposable).Dispose()
                    this.Cts.Cancel()
                    this.Cts.Dispose()
                    onMsgDisposable |> Option.iter (fun d -> d.Dispose())

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.KeepAliveInterval = this.Hub.KeepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.ServerTimeout = this.Hub.ServerTimeout

            type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
                internal (hub: HubConnection<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>, ?onMsg: 'ServerApi -> unit) =
                
                let onMsgDisposable = 
                    match onMsg with
                    | Some onMsg -> 
                        hub.OnMessage(fun msg -> async { return onMsg msg })
                        |> Some
                    | None -> None
        
                interface IDisposable with
                    member this.Dispose () = this.Dispose()

                member internal _.Hub = hub
        
                member internal _.Cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> IDisposable).Dispose()
                    this.Cts.Cancel()
                    this.Cts.Dispose()
                    onMsgDisposable |> Option.iter (fun d -> d.Dispose())

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.KeepAliveInterval = this.Hub.KeepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.ServerTimeout = this.Hub.ServerTimeout

            type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> 
                internal (hub: HubConnection<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>, ?onMsg: 'ServerApi -> unit) =
                
                let onMsgDisposable = 
                    match onMsg with
                    | Some onMsg -> 
                        hub.OnMessage(fun msg -> async { return onMsg msg })
                        |> Some
                    | None -> None

                interface IDisposable with
                    member this.Dispose () = this.Dispose()
        
                member internal _.Hub = hub
        
                member internal _.Cts = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    (hub :> IDisposable).Dispose()
                    this.Cts.Cancel()
                    this.Cts.Dispose()
                    onMsgDisposable |> Option.iter (fun d -> d.Dispose())

                /// Default interval at which to ping the server.
                /// 
                /// The default value is 15,000 milliseconds (15 seconds).
                /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
                member this.KeepAliveInterval = this.Hub.KeepAliveInterval

                /// The server timeout in milliseconds.
                /// 
                /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
                /// The default timeout value is 30,000 milliseconds (30 seconds).
                member this.ServerTimeout = this.Hub.ServerTimeout

    [<RequireQualifiedAccess>]
    module Cmd =
        [<RequireQualifiedAccess>]
        module SignalR =
            module Stream =
                module Bidrectional =
                    /// Starts a connection to a SignalR hub with server and client streaming enabled.
                    let connect
                        (registerHub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> 'Msg) 
                        (config: Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let (connection, onMsg) =
                                Elmish.HubConnectionBuilder(HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.Build()

                            connection.StartNow()

                            registerHub (new Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(connection, ?onMsg = onMsg))
                            |> dispatch ]
                
                module ServerToClient =
                    /// Starts a connection to a SignalR hub with server streaming enabled.
                    let connect
                        (registerHub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> -> 'Msg)
                        (config: Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let (connection, onMsg) =
                                Elmish.HubConnectionBuilder(HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.Build()

                            connection.StartNow()

                            registerHub (new Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>(connection, ?onMsg = onMsg))
                            |> dispatch ]
                    
                module ClientToServer =
                    /// Starts a connection to a SignalR hub with client streaming enabled.
                    let connect
                        (registerHub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> -> 'Msg)
                        (config: Elmish.HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit,'Msg> 
                            -> Elmish.HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit,'Msg>) : Cmd<'Msg> =
            
                        [ fun dispatch -> 
                            let (connection, onMsg) =
                                Elmish.HubConnectionBuilder(HubConnectionBuilder(), dispatch) 
                                |> config 
                                |> fun hubBuilder -> hubBuilder.Build()

                            connection.StartNow()

                            registerHub (new Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>(connection, ?onMsg = onMsg))
                            |> dispatch ]
            
            /// Starts a connection to a SignalR hub.
            let connect
                (registerHub: Elmish.Hub<'ClientApi,'ServerApi> -> 'Msg)
                (config: Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg> 
                    -> Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg>) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let (connection, onMsg) =
                        Elmish.HubConnectionBuilder(HubConnectionBuilder(), dispatch) 
                        |> config 
                        |> fun hubBuilder -> hubBuilder.Build()

                    connection.StartNow()

                    registerHub (new Elmish.Hub<'ClientApi,'ServerApi>(connection, ?onMsg = onMsg))
                    |> dispatch ]

        type SignalR =
            /// Invokes a hub method on the server and maps the error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member attempt (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : 'ClientApi -> (exn -> 'Msg) -> Cmd<'Msg> =
                fun (msg: 'ClientApi) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.attempt 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onError
                    | None -> [ ignore ]
            
            /// Invokes a hub method on the server and maps the error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member attempt (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> (exn -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.attempt 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onError
                    | None -> [ ignore ]
            
            /// Invokes a hub method on the server and maps the error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member attempt (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) : 'ClientApi -> (exn -> 'Msg) -> Cmd<'Msg> =
                fun (msg: 'ClientApi) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.attempt 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onError
                    | None -> [ ignore ]
            
            /// Invokes a hub method on the server and maps the error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member attempt (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> (exn -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.attempt 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onError
                    | None -> [ ignore ]

            /// Returns the connectionId to the hub of this client.
            // fsharplint:disable-next-line
            static member connectionId (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : (string option -> 'Msg) -> Cmd<'Msg> =
                fun (msg: string option -> 'Msg) ->
                    [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.ConnectionId |> msg |> dispatch) ]
            
            /// Returns the connectionId to the hub of this client.
            // fsharplint:disable-next-line
            static member connectionId (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : (string option -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: string option -> 'Msg) ->
                    [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.ConnectionId |> msg |> dispatch) ]

            /// Returns the connectionId to the hub of this client.
            // fsharplint:disable-next-line
            static member connectionId (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) : (string option -> 'Msg) -> Cmd<'Msg> =
                fun (msg: string option -> 'Msg) ->
                    [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.ConnectionId |> msg |> dispatch) ]
            
            /// Returns the connectionId to the hub of this client.
            // fsharplint:disable-next-line
            static member connectionId (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                : (string option -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: string option -> 'Msg) ->
                    [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.ConnectionId |> msg |> dispatch) ]

            /// Invokes a hub method on the server and maps the success or error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member either (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : 'ClientApi -> ('ServerApi -> 'Msg) -> (exn -> 'Msg) -> Cmd<'Msg> =
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.either 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg 
                            onSuccess
                            onError
                    | None -> [ ignore ]
                    
            /// Invokes a hub method on the server and maps the success or error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member either (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> (exn -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.either 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg 
                            onSuccess
                            onError
                    | None -> [ ignore ]

            /// Invokes a hub method on the server and maps the success or error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member either (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> (exn -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.either 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg 
                            onSuccess
                            onError
                    | None -> [ ignore ]

            /// Invokes a hub method on the server and maps the success or error.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member either (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> (exn -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) (onError: exn -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.either 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg 
                            onSuccess
                            onError
                    | None -> [ ignore ]
                    
            /// Invokes a hub method on the server and maps the success.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member perform (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : 'ClientApi -> ('ServerApi -> 'Msg) -> Cmd<'Msg> =
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.perform 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onSuccess
                    | None -> [ ignore ]
                    
            /// Invokes a hub method on the server and maps the success.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member perform (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.perform 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onSuccess
                    | None -> [ ignore ]

            /// Invokes a hub method on the server and maps the success.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member perform (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.perform 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onSuccess
                    | None -> [ ignore ]

            /// Invokes a hub method on the server and maps the success.
            /// 
            /// This method resolves when the server indicates it has finished invoking the method. When it finishes, 
            /// the server has finished invoking the method. If the server method returns a result, it is produced as the result of
            /// resolving the async call.
            // fsharplint:disable-next-line
            static member perform (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> ('ServerApi -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) (onSuccess: 'ServerApi -> 'Msg) ->
                    match hub with
                    | Some hub ->
                        Cmd.OfAsyncWith.perform 
                            (fun msg -> Async.StartImmediate(msg, hub.Cts.Token)) 
                            hub.Hub.Invoke
                            msg
                            onSuccess
                    | None -> [ ignore ]

            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            // fsharplint:disable-next-line
            static member send (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : 'ClientApi -> Cmd<'Msg> =
                fun (msg: 'ClientApi) -> [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.SendNow msg) ]
                
            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            // fsharplint:disable-next-line
            static member send (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : 'ClientApi -> Cmd<'Msg> =
                
                fun (msg: 'ClientApi) -> [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.SendNow msg) ]

            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            // fsharplint:disable-next-line
            static member send (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) : 'ClientApi -> Cmd<'Msg> =
                fun (msg: 'ClientApi) -> [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.SendNow msg) ]

            /// Invokes a hub method on the server. Does not wait for a response from the receiver.
            /// 
            /// This method resolves when the client has sent the invocation to the server. The server may still
            /// be processing the invocation.
            // fsharplint:disable-next-line
            static member send (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) : 'ClientApi -> Cmd<'Msg> =
                fun (msg: 'ClientApi) -> [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.SendNow msg) ]

            /// Returns the state of the Hub connection to the server.
            // fsharplint:disable-next-line
            static member state (hub: Elmish.Hub<'ClientApi,'ServerApi> option) : (HubConnectionState -> 'Msg) -> Cmd<'Msg> =
                fun (msg: HubConnectionState -> 'Msg) -> [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.State |> msg |> dispatch) ]
                    
            /// Returns the state of the Hub connection to the server.
            // fsharplint:disable-next-line
            static member state (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) 
                : (HubConnectionState -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: HubConnectionState -> 'Msg) -> [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.State |> msg |> dispatch) ]

            /// Returns the state of the Hub connection to the server.
            // fsharplint:disable-next-line
            static member state (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) 
                : (HubConnectionState -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: HubConnectionState -> 'Msg) -> [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.State |> msg |> dispatch) ]

            /// Returns the state of the Hub connection to the server.
            // fsharplint:disable-next-line
            static member state (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                : (HubConnectionState -> 'Msg) -> Cmd<'Msg> =
                
                fun (msg: HubConnectionState -> 'Msg) -> [ fun dispatch -> hub |> Option.iter (fun hub -> hub.Hub.State |> msg |> dispatch) ]

            /// Streams from the hub.
            // fsharplint:disable-next-line
            static member streamFrom (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamApi) (subscriber: ('Msg -> unit) -> IAsyncEnumerable<'ServerStreamApi> -> unit) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            async {
                                let! streamResult = hub.Hub.StreamFrom msg 
                                
                                return subscriber dispatch streamResult
                            }
                            |> fun a -> Async.StartImmediate(a, hub.Cts.Token)) ] : Cmd<'Msg>

            /// Streams from the hub.
            // fsharplint:disable-next-line
            static member streamFrom (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) =
                fun (msg: 'ClientStreamFromApi) (subscriber: ('Msg -> unit) -> IAsyncEnumerable<'ServerStreamApi> -> unit) ->
                    [ fun dispatch -> 
                        hub |> Option.iter (fun hub -> 
                            async {
                                let! streamResult = hub.Hub.StreamFrom msg 
                                
                                return subscriber dispatch streamResult
                            }
                            |> fun a -> Async.StartImmediate(a, hub.Cts.Token)) ] : Cmd<'Msg>

            /// Streams to the hub.
            // fsharplint:disable-next-line
            static member streamTo (hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option) =
                fun (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.StreamToNow(asyncEnum)) ] : Cmd<_>

            /// Streams to the hub.
            // fsharplint:disable-next-line
            static member streamTo (hub: Elmish.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> option) =
                fun (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>) ->
                    [ fun _ -> hub |> Option.iter (fun hub -> hub.Hub.StreamToNow(asyncEnum)) ] : Cmd<_>
