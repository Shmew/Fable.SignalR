namespace Fable.SignalR

open Feliz

module Feliz =
    type Hub<'ClientApi,'ServerApi> = Fable.React.IRefValue<Fable.SignalR.Hub<'ClientApi,'ServerApi>>

    [<RequireQualifiedAccess>]
    module Hub =
        type Config<'ClientApi,'ServerApi> = 
            HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit> 
                -> HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit>

    [<RequireQualifiedAccess>]
    module StreamHub =
        type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
            Fable.React.IRefValue<StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>

        module Bidrectional =
            type Config<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
                HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                    -> HubConnectionBuilder<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>

        type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
            Fable.React.IRefValue<StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>

        module ServerToClient =
            type Config<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> = 
                HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi> 
                    -> HubConnectionBuilder<'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi>

        type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> = 
            Fable.React.IRefValue<StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>>

        module ClientToServer =
            type Config<'ClientApi,'ClientStreamApi,'ServerApi> = 
                HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit> 
                    -> HubConnectionBuilder<'ClientApi,unit,'ClientStreamApi,'ServerApi,unit>

    type React with
        static member inline useSignalR<'ClientApi,'ServerApi> (config: Hub.Config<'ClientApi,'ServerApi>, ?dependencies: obj []) =
            let connection = React.useMemo((fun () -> SignalR.connect(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            React.useEffectOnce(fun () ->
                connection.current.startNow()

                React.createDisposable(connection.current.stopNow)
            )

            React.useRef(connection.current :> Fable.SignalR.Hub<'ClientApi,'ServerApi>)
        
        static member inline useSignalR<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> 
            (config: StreamHub.ServerToClient.Config<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>, ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,'ClientStreamApi,_,'ServerApi,'ServerStreamApi>(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            React.useEffectOnce(fun () ->
                connection.current.startNow()

                React.createDisposable(connection.current.stopNow)
            )

            React.useRef(connection.current :> Fable.SignalR.StreamHub.ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>)

        static member inline useSignalR<'ClientApi,'ClientStreamApi,'ServerApi> 
            (config: StreamHub.ClientToServer.Config<'ClientApi,'ClientStreamApi,'ServerApi>, ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,_,'ClientStreamApi,'ServerApi,_>(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            React.useEffectOnce(fun () ->
                connection.current.startNow()

                React.createDisposable(connection.current.stopNow)
            )

            React.useRef(connection.current :> Fable.SignalR.StreamHub.ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi>)

        static member inline useSignalR<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
            (config: StreamHub.Bidrectional.Config<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>, 
             ?dependencies: obj []) =
            
            let connection = React.useMemo((fun () -> 
                SignalR.connect<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>(config)), ?dependencies = dependencies)
            let connection = React.useRef(connection)

            React.useEffectOnce(fun () ->
                connection.current.startNow()

                React.createDisposable(connection.current.stopNow)
            )

            React.useRef(connection.current :> Fable.SignalR.StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>)
