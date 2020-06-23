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
              
        type Config<'ClientApi,'ServerApi> = HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit> -> HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit>

        module Stream =
            module Both =
                [<NoComparison;NoEquality>]
                type Hub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
                    { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
                      invoke: 'ClientApi -> Async<'ServerApi> 
                      send: 'ClientApi -> unit
                      streamFrom: 'ClientStreamFromApi -> StreamResult<'ServerStreamApi>
                      streamTo: Subject<'ClientStreamToApi> -> unit }

                type Config<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
                    HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                        -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>

            module From =
                [<NoComparison;NoEquality>]
                type Hub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
                    { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
                      invoke: 'ClientApi -> Async<'ServerApi> 
                      send: 'ClientApi -> unit
                      streamFrom: 'ClientStreamApi -> StreamResult<'ServerStreamApi> }

                type Config<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
                    HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi> 
                        -> HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>

            module To =
                [<NoComparison;NoEquality>]
                type Hub<'ClientApi,'ClientStreamApi,'ServerApi> =
                    { cancellationToken: Fable.React.IRefValue<System.Threading.CancellationToken>
                      invoke: 'ClientApi -> Async<'ServerApi> 
                      send: 'ClientApi -> unit
                      streamTo: Subject<'ClientStreamApi> -> unit }

                type Config<'ClientApi,'ClientStreamApi,'ServerApi> = 
                    HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit> 
                        -> HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>

    type Hub<'ClientApi,'ServerApi> = Fable.React.IRefValue<SignalR.Hub<'ClientApi,'ServerApi>>

    [<RequireQualifiedAccess>]
    module StreamHub =
        type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
            Fable.React.IRefValue<SignalR.Stream.Both.Hub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>

        type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
            Fable.React.IRefValue<SignalR.Stream.From.Hub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>

        type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> = 
            Fable.React.IRefValue<SignalR.Stream.To.Hub<'ClientApi,'ClientStreamApi,'ServerApi>>

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

            let hub : Hub<'ClientApi,'ServerApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send }

            hub
        
        static member inline useSignalR<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
            (config: SignalR.Stream.From.Config<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>, ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,'ClientStreamApi,_,'ServerApi,'ServerStreamApi>(config)), ?dependencies = dependencies)
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
                connection.current.streamFrom msg

            let hub : StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send
                      streamFrom = stream }

            hub

        static member inline useSignalR<'ClientApi,'ClientStreamApi,'ServerApi> 
            (config: SignalR.Stream.To.Config<'ClientApi,'ClientStreamApi,'ServerApi>, ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,_,'ClientStreamApi,'ServerApi,_>(config)), ?dependencies = dependencies)
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
                connection.current.streamToNow msg

            let hub : StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send
                      streamTo = stream }

            hub

        static member inline useSignalR<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
            (config: SignalR.Stream.Both.Config<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>, 
             ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(config)), ?dependencies = dependencies)
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
                
            let streamFrom = React.useCallbackRef <| fun msg ->
                connection.current.streamFrom msg

            let streamTo = React.useCallbackRef <| fun msg ->
                connection.current.streamToNow msg

            let hub : StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
                React.useRef <|
                    { cancellationToken = token
                      invoke = invoke
                      send = send
                      streamFrom = streamFrom
                      streamTo = streamTo }

            hub
