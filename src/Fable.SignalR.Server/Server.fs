namespace Fable.SignalR

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic
open System.Threading.Tasks

type IFableHubServerApi<'ServerApi when 'ServerApi : not struct> =
    abstract Send: 'ServerApi -> Task

type IStreamingFableHubServerApi<'ServerApi, 'StreamServerApi when 'ServerApi : not struct> =
    abstract Send: 'ServerApi -> Task

    abstract Stream: IAsyncEnumerable<'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>

type FableHub<'ClientApi,'ServerApi 
    when 'ClientApi : not struct and 'ServerApi : not struct> 
    (updater: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =
            
    inherit Hub<IFableHubServerApi<'ServerApi>>()

    member this.Send (msg: 'ClientApi) =
        updater msg this

type StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (sendUpdater: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task,
     sendStreamer: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>) =

    inherit Hub<IStreamingFableHubServerApi<'ServerApi,'StreamServerApi>>()

    member this.Send (msg: 'ClientApi) =
        sendUpdater msg this

    member this.Stream (msg: 'ClientStreamApi) =
        sendStreamer msg this
            
[<RequireQualifiedAccess>]
module FableHub =
    [<RequireQualifiedAccess>]
    module OnConnected =
        type internal IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

        let addTransient onConnected update (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Update = update; OnConnected = onConnected })
    
    type OnConnected<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        internal (settings: OnConnected.IOverride<'ClientApi,'ServerApi>) =

        inherit FableHub<'ClientApi,'ServerApi>(settings.Update)

        override this.OnConnectedAsync () = 
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

    [<RequireQualifiedAccess>]
    module OnDisconnected =
        type internal IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

        let addTransient onDisconnected update (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Update = update; OnDisconnected = onDisconnected })
    
    type OnDisconnected<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        internal (settings: OnDisconnected.IOverride<'ClientApi,'ServerApi>) =

        inherit FableHub<'ClientApi,'ServerApi>(settings.Update)

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

    [<RequireQualifiedAccess>]
    module Both =
        type internal IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

        let addTransient onConnected onDisconnected update (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Update = update; OnConnected = onConnected; OnDisconnected = onDisconnected })

    type Both<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        internal (settings: Both.IOverride<'ClientApi,'ServerApi>) =

        inherit FableHub<'ClientApi,'ServerApi>(settings.Update)

        override this.OnConnectedAsync () =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

    let addTransient update (s: IServiceCollection) =
        s.AddTransient<'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task> <|
            System.Func<System.IServiceProvider,'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task>
                (fun _ -> update)

    [<RequireQualifiedAccess>]
    module Stream =
        [<RequireQualifiedAccess>]
        module OnConnected =
            type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> =

                { Update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task
                  Stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>
                  OnConnected: StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit> }

            let addTransient onConnected update stream (s: IServiceCollection) =
                s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>> <|
                    System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>
                        (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected })
        
        type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> 
            internal (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =

            inherit StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>(settings.Update, settings.Stream)

            override this.OnConnectedAsync () = 
                this :> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>
                |> settings.OnConnected :> Task

        [<RequireQualifiedAccess>]
        module OnDisconnected =
            type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> =

                { Update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task
                  Stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>
                  OnDisconnected: exn -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit> }

            let addTransient onDisconnected update stream (s: IServiceCollection) =
                s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>> <|
                    System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>
                        (fun _ -> { Update = update; Stream = stream; OnDisconnected = onDisconnected })
        
        type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct>
            internal (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =

            inherit StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>(settings.Update, settings.Stream)

            override this.OnDisconnectedAsync (err: exn) =
                this :> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>
                |> settings.OnDisconnected err :> Task

        [<RequireQualifiedAccess>]
        module Both =
            type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> =

                { Update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task
                  Stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>
                  OnConnected: StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>
                  OnDisconnected: exn -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit> }

            let addTransient onConnected onDisconnected update stream (s: IServiceCollection) =
                s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>> <|
                    System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>
                        (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected; OnDisconnected = onDisconnected })

        type Both<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> 
            internal (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =

            inherit StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>(settings.Update, settings.Stream)

            override this.OnConnectedAsync () =
                this :> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>
                |> settings.OnConnected :> Task

            override this.OnDisconnectedAsync (err: exn) =
                this :> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>
                |> settings.OnDisconnected err :> Task

        let addTransient update stream (s: IServiceCollection) =
            s.AddTransient<'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task> <|
                System.Func<System.IServiceProvider,'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task>
                    (fun _ -> update)
            |> fun s ->
            s.AddTransient<'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>> <|
            System.Func<System.IServiceProvider,'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>>
                (fun _ -> stream)

[<RequireQualifiedAccess>]
module SignalR =
    type Config<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct> =

        { EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option
          HubOptions: (HubOptions -> unit) option
          OnConnected: (FableHub<'ClientApi,'ServerApi> -> Task<unit>) option
          OnDisconnected: (exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) option }

        static member Default () =
            { EndpointConfig = None 
              HubOptions = None
              OnConnected = None
              OnDisconnected = None }

    [<RequireQualifiedAccess>]
    module Config =
        let bindEnpointConfig (settings: Config<'ClientApi,'ServerApi> option) (endpointBuilder: HubEndpointConventionBuilder) =
            settings
            |> Option.bind (fun c -> c.EndpointConfig |> Option.map (fun c -> c endpointBuilder))
            |> Option.defaultValue endpointBuilder

    type Settings<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct> =

        { EndpointPattern: string
          Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
          Config: Config<'ClientApi,'ServerApi> option }

        static member GetConfigOrDefault (settings: Settings<'ClientApi,'ServerApi>) =
            match settings.Config with
            | None -> Config<'ClientApi,'ServerApi>.Default()
            | Some config -> config

        static member Create (endpointPattern: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =
                ConfigBuilder(endpointPattern, update)

    and ConfigBuilder<'ClientApi,'ServerApi 
        when 'ClientApi : not struct and 'ServerApi : not struct>
        (endpoint: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =

        let mutable state =
            { EndpointPattern = endpoint
              Update = update
              Config = None }

        member this.EndpointConfig (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
            state <-
                { state with
                    Config =
                        { Settings.GetConfigOrDefault state with
                            EndpointConfig = Some f }
                        |> Some }
            this

        member this.HubOptions (f: HubOptions -> unit) =
            state <-
                { state with
                    Config =
                        { Settings.GetConfigOrDefault state with
                            HubOptions = Some f }
                        |> Some }
            this

        member this.OnConnected (f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings.GetConfigOrDefault state with
                            OnConnected = Some f }
                        |> Some }
            this

        member this.OnDisconnected (f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings.GetConfigOrDefault state with
                            OnDisconnected = Some f }
                        |> Some }
            this

        member _.Build () = state

    [<RequireQualifiedAccess>]
    module Stream =
        type Config<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option
              HubOptions: (HubOptions -> unit) option
              OnConnected: (StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) option
              OnDisconnected: (exn -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) option }

            static member Default () =
                { EndpointConfig = None 
                  HubOptions = None
                  OnConnected = None
                  OnDisconnected = None }

        [<RequireQualifiedAccess>]
        module Config =
            let bindEnpointConfig (settings: Config<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> option) (endpointBuilder: HubEndpointConventionBuilder) =
                settings
                |> Option.bind (fun c -> c.EndpointConfig |> Option.map (fun c -> c endpointBuilder))
                |> Option.defaultValue endpointBuilder

        type Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { EndpointPattern: string
              Update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task
              Stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>
              Config: Config<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> option }

            static member GetConfigOrDefault (settings: Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =
                match settings.Config with
                | None -> Config<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>.Default()
                | Some config -> config

            static member Create 
                (endpointPattern: string, 
                 update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task, 
                 stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>) =
                    
                ConfigBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>(endpointPattern, update, stream)

        and ConfigBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct>
            (endpoint: string, 
             update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task, 
             stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> IAsyncEnumerable<'StreamServerApi>) =

            let mutable state =
                { EndpointPattern = endpoint
                  Update = update
                  Stream = stream
                  Config = None }

            member this.EndpointConfig (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
                state <-
                    { state with
                        Config =
                            { Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>.GetConfigOrDefault state with
                                EndpointConfig = Some f }
                            |> Some }
                this

            member this.HubOptions (f: HubOptions -> unit) =
                state <-
                    { state with
                        Config =
                            { Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>.GetConfigOrDefault state with
                                HubOptions = Some f }
                            |> Some }
                this

            member this.OnConnected (f: StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) =
                state <-
                    { state with
                        Config =
                            { Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>.GetConfigOrDefault state with
                                OnConnected = Some f }
                            |> Some }
                this

            member this.OnDisconnected (f: exn -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) =
                state <-
                    { state with
                        Config =
                            { Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>.GetConfigOrDefault state with
                                OnDisconnected = Some f }
                            |> Some }
                this

            member _.Build () = state

