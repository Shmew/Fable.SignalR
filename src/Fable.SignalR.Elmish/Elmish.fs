namespace Fable.SignalR

open Elmish
open Fable.Core
open System.ComponentModel

module Elmish =
    [<RequireQualifiedAccess>]
    module Elmish =
        type Hub<'ClientApi,'ServerApi> [<EditorBrowsable(EditorBrowsableState.Never)>] (hub: HubConnection<'ClientApi,unit,unit,'ServerApi,unit>) =
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.hub = hub
        
            [<EditorBrowsable(EditorBrowsableState.Never)>]
            member _.CancellationToken = new System.Threading.CancellationTokenSource()

            member this.Dispose () =
                this.hub.stopNow()
                this.CancellationToken.Cancel()
                this.CancellationToken.Dispose()

            interface System.IDisposable with
                member this.Dispose () = this.Dispose()
        
        module StreamHub =
            type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit Hub<'ClientApi,'ServerApi>(unbox hub)
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.CancellationToken = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    this.hub.stopNow()
                    this.CancellationToken.Cancel()
                    this.CancellationToken.Dispose()

                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()

            type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>) =

                inherit Hub<'ClientApi,'ServerApi>(unbox hub)
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.CancellationToken = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    this.hub.stopNow()
                    this.CancellationToken.Cancel()
                    this.CancellationToken.Dispose()

                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()

            type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> 
                [<EditorBrowsable(EditorBrowsableState.Never)>] 
                (hub: HubConnection<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>) =

                inherit Hub<'ClientApi,'ServerApi>(unbox hub)
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.hub = hub
        
                [<EditorBrowsable(EditorBrowsableState.Never)>]
                member _.CancellationToken = new System.Threading.CancellationTokenSource()

                member this.Dispose () =
                    this.hub.stopNow()
                    this.CancellationToken.Cancel()
                    this.CancellationToken.Dispose()

                interface System.IDisposable with
                    member this.Dispose () = this.Dispose()

    [<RequireQualifiedAccess>]
    module Cmd =
        [<RequireQualifiedAccess>]
        module SignalR =
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

            module Stream =
                module Bidrectional =
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

            let invoke (hub: Elmish.Hub<'ClientApi,'Msg> option) (msg: 'ClientApi) (onError: exn -> 'Msg) =
                match hub with
                | Some hub -> 
                    Cmd.OfAsyncWith.either 
                        (fun msg -> Async.StartImmediate(msg, hub.CancellationToken.Token)) 
                        (fun msg -> hub.hub.invoke msg) 
                        msg 
                        id 
                        onError
                | None -> 
                    #if DEBUG
                    JS.console.error("Cannot send a message if hub is not initialized!")
                    #endif
                    [ fun _ -> () ]
            
            let send (hub: Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) : Cmd<_> =
                [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.sendNow msg) ]

            let streamFrom (hub: Elmish.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option) 
                (msg: 'ClientStreamApi) (sub: StreamResult<'ServerStreamApi> -> ('Msg -> unit) -> unit) : Cmd<_> =
                
                [ fun dispatch -> hub |> Option.iter (fun hub -> hub.hub.streamFrom msg |> fun rsp -> sub rsp dispatch) ]

            /// Finish streaming and do srtp for allowing other types to work with these commands

