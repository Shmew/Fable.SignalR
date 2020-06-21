namespace Fable.SignalR

open Elmish
open Fable.Core
open System.ComponentModel

module Elmish =
    type ElmishHub<'ClientApi,'ServerApi> [<EditorBrowsable(EditorBrowsableState.Never)>] (hub: HubConnection<'ClientApi,'ServerApi>) =
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
                (registerHub: ElmishHub<'ClientApi,'ServerApi> -> 'Msg) 
                (registerMsgs: 'ServerApi -> 'Msg)
                (config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let connection = SignalR.connect(config)

                    connection.onMsg(registerMsgs >> dispatch) 

                    connection.startNow()

                    registerHub (new ElmishHub<'ClientApi,'ServerApi>(connection))
                    |> dispatch ]

            let inline connectWith
                (registerHub: ElmishHub<'ClientApi,'ServerApi> -> 'Msg) 
                (registerMsgs: 'ServerApi -> 'Msg)
                (config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>) 
                (registerHandlers: HubRegistration -> ('Msg -> unit) -> unit) : Cmd<'Msg> =
            
                [ fun dispatch -> 
                    let connection =
                        SignalR.connect(config)

                    registerHandlers (connection :> HubRegistration) dispatch

                    connection.onMsg(registerMsgs >> dispatch)

                    connection.startNow()

                    registerHub (new ElmishHub<'ClientApi,'ServerApi>(connection))
                    |> dispatch ]

            let invoke (hub: ElmishHub<'ClientApi,'Msg> option) (msg: 'ClientApi) (onError: exn -> 'Msg) =
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
            
            let send (hub: ElmishHub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) : Cmd<_> =
                [ fun _ -> hub |> Option.iter (fun hub -> hub.hub.sendNow msg) ]
