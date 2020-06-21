namespace Fable.SignalR

open Fable.Core
open Feliz

module Feliz =
    [<NoComparison;NoEquality>]
    type FelizHub<'ClientApi,'ServerApi> =
        { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
          invoke: 'ClientApi -> Async<'ServerApi> 
          send: 'ClientApi -> unit
          stream: 'ClientApi -> IStreamResult<'ServerApi> }

    type HubRef<'ClientApi,'ServerApi> = Fable.React.IRefValue<FelizHub<'ClientApi,'ServerApi>>
    
    type FelizHubConfig<'ClientApi,'ServerApi> =
        { config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>
          onMsg: 'ServerApi -> unit }

    type FelizHubConfigWithHandlers<'ClientApi,'ServerApi> =
        { config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>
          onMsg: 'ServerApi -> unit
          handlers: HubRegistration -> unit }

    //[<NoComparison;NoEquality>]
    //type StreamingFelizHub<'ClientApi,'StreamingClientApi,'ServerApi,'StreamingServerApi> =
    //    { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
    //      invoke: 'ClientApi -> Async<'ServerApi> 
    //      send: 'ClientApi -> unit }

    //type StreaminHubRef<'ClientApi,'StreamingClientApi,'ServerApi,'StreamingServerApi> = 
    //    Fable.React.IRefValue<StreamingFelizHub<'ClientApi,'StreamingClientApi,'ServerApi,'StreamingServerApi>>
    
    //type StreaminFelizHubConfig<'ClientApi,'StreamingClientApi,'ServerApi,'StreamingServerApi> =
    //    { config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>
    //      onMsg: 'ServerApi -> unit }

    //type StreaminFelizHubConfigWithHandlers<'ClientApi,'ServerApi> =
    //    { config: HubConnectionBuilder<'ClientApi,'ServerApi> -> HubConnectionBuilder<'ClientApi,'ServerApi>
    //      onMsg: 'ServerApi -> unit
    //      handlers: HubRegistration -> unit }

    type React with
        static member inline useSignalR<'ClientApi,'ServerApi> (config: FelizHubConfig<'ClientApi,'ServerApi>, ?dependencies: obj []) =
            let connection = React.useMemo((fun () -> SignalR.connect(config.config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            let tokenSource = React.useRef(new System.Threading.CancellationTokenSource())
            let token = React.useRef(tokenSource.current.Token)

            React.useEffectOnce(fun () ->
                connection.current.onMsg config.onMsg
                
                connection.current.startNow()

                React.createDisposable <| fun () -> 
                    connection.current.stopNow()
                    tokenSource.current.Cancel()
                    tokenSource.current.Dispose()
            )

            let send = React.useCallbackRef <| fun msg -> 
                Async.StartImmediate(connection.current.send msg, token.current)

            let invoke = React.useCallbackRef <| fun msg ->
                connection.current.invoke msg
                
            let stream = React.useCallbackRef <| fun msg ->
                connection.current.stream msg

            let hub : HubRef<'ClientApi,'ServerApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send
                      stream = stream }

            hub

        //static member inline useSignalR<'ClientApi,'ServerApi> (config: FelizHubConfigWithHandlers<'ClientApi,'ServerApi>, ?dependencies: obj []) =
        //    let connection = React.useMemo((fun () -> SignalR.connect(config.config)), ?dependencies = dependencies)
        //    let connection = React.useRef(connection)

        //    let tokenSource = React.useRef(new System.Threading.CancellationTokenSource())
        //    let token = React.useRef(tokenSource.current.Token)

        //    React.useEffectOnce(fun () ->
        //        connection.current.onMsg config.onMsg

        //        connection.current :> HubRegistration
        //        |> config.handlers

        //        connection.current.startNow()

        //        React.createDisposable <| fun () -> 
        //            connection.current.stopNow()
        //            tokenSource.current.Cancel()
        //            tokenSource.current.Dispose()
        //    )

        //    let send = React.useCallbackRef <| fun msg -> 
        //        Async.StartImmediate(connection.current.send msg, token.current)

        //    let invoke = React.useCallbackRef <| fun msg ->
        //        connection.current.invoke msg
                
        //    let hub : HubRef<'ClientApi,'ServerApi> = 
        //        React.useRef <|
        //            { cancellationToken = token
        //              invoke = invoke
        //              send = send }

        //    hub
