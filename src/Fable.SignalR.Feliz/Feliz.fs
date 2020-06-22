namespace Fable.SignalR

open Feliz

module Feliz =
    [<RequireQualifiedAccess>]
    module SignalR =
        [<NoComparison;NoEquality>]
        type Hub<'ClientApi,'ServerApi> =
            { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
              invoke: 'ClientApi -> Async<'ServerApi> 
              send: 'ClientApi -> unit }

        [<NoComparison;NoEquality>]
        type StreamHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
            { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
              invoke: 'ClientApi -> Async<'ServerApi> 
              send: 'ClientApi -> unit
              stream: 'ClientStreamApi -> IStreamResult<'ServerStreamApi> }

        type Config<'ClientApi,'ServerApi> = HubConnectionBuilder<'ClientApi,unit,'ServerApi,unit> -> HubConnectionBuilder<'ClientApi,unit,'ServerApi,unit>

        type StreamConfig<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
            HubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
                -> HubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

    type HubRef<'ClientApi,'ServerApi> = Fable.React.IRefValue<SignalR.Hub<'ClientApi,'ServerApi>>
    type StreamHubRef<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
        Fable.React.IRefValue<SignalR.StreamHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>

    type React with
        static member inline useSignalR<'ClientApi,'ServerApi> (config: SignalR.Config<'ClientApi,'ServerApi>, ?dependencies: obj []) =
            let connection = React.useMemo((fun () -> SignalR.connect(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            let tokenSource = React.useRef(new System.Threading.CancellationTokenSource())
            let token = React.useRef(tokenSource.current.Token)

            React.useEffectOnce(fun () ->
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

            let hub : HubRef<'ClientApi,'ServerApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send }

            hub
        
        static member inline useSignalR<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
            (config: SignalR.StreamConfig<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>, ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> SignalR.connect<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            let tokenSource = React.useRef(new System.Threading.CancellationTokenSource())
            let token = React.useRef(tokenSource.current.Token)

            React.useEffectOnce(fun () ->
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

            let hub : StreamHubRef<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send
                      stream = stream }

            hub
