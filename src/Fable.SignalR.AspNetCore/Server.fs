namespace Fable.SignalR

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open System.Collections.Generic
open System.ComponentModel
open System.Threading.Tasks

[<EditorBrowsable(EditorBrowsableState.Never)>]
type IFableHubCallerClients<'ServerApi when 'ServerApi : not struct> =
    abstract Send: 'ServerApi -> Task

type FableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    abstract Clients : IHubCallerClients<IFableHubCallerClients<'ServerApi>>
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Send : 'ClientApi -> Task
    abstract Dispose : unit -> unit

[<EditorBrowsable(EditorBrowsableState.Never)>]
type NormalFableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> 
    (updater: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =
    
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()

    member this.Send msg = updater msg this

[<EditorBrowsable(EditorBrowsableState.Never)>]
type StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Updater: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      StreamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi> }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()

    member this.Send msg = settings.Updater msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)

type StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Updater: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      StreamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task }

and StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()

    member this.Send msg = settings.Updater msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

type StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Updater: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task }

and StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()

    member this.Send msg = settings.Updater msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

[<RequireQualifiedAccess>]
module internal Task =
    let toGen (f: 'a -> 'b -> #Task) =
        fun a b -> f a b :> Task

[<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
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

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.Update)

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

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.Update)

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

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.Update)

        override this.OnConnectedAsync () =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

    module Stream =
        module Both =
            [<RequireQualifiedAccess>]
            module OnConnected =
                type internal IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected update streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: OnConnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Updater = settings.Update; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            [<RequireQualifiedAccess>]
            module OnDisconnected =
                type internal IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected update streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; StreamFrom = streamFrom; StreamTo = streamTo; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                internal (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Updater = settings.Update; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            [<RequireQualifiedAccess>]
            module Both =
                type internal IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected update streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: Both.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Updater = settings.Update; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            let addTransient settings (s: IServiceCollection) =
                s.AddTransient<StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                    System.Func<System.IServiceProvider,StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                        (fun _ -> settings)

        module From =
            [<RequireQualifiedAccess>]
            module OnConnected =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Updater = settings.Update; StreamFrom = settings.Stream })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            [<RequireQualifiedAccess>]
            module OnDisconnected =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; Stream = stream; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                internal (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Updater = settings.Update; StreamFrom = settings.Stream })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            [<RequireQualifiedAccess>]
            module Both =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Updater = settings.Update; StreamFrom = settings.Stream })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            let addTransient settings (s: IServiceCollection) =
                s.AddTransient<StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                    System.Func<System.IServiceProvider,StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                        (fun _ -> settings)

        module To =
            [<RequireQualifiedAccess>]
            module OnConnected =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Updater = settings.Update; StreamTo = settings.Stream })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            [<RequireQualifiedAccess>]
            module OnDisconnected =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Update = update; Stream = stream; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                internal (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Updater = settings.Update; StreamTo = settings.Stream })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            [<RequireQualifiedAccess>]
            module Both =
                type internal IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected update stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Update = update; Stream = stream; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Updater = settings.Update; StreamTo = settings.Stream })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            let addTransient settings (s: IServiceCollection) =
                s.AddTransient<StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                    System.Func<System.IServiceProvider,StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>>
                        (fun _ -> settings)

    let addUpdateTransient update (s: IServiceCollection) =
        s.AddTransient<'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task> <|
            System.Func<System.IServiceProvider,'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task>
                (fun _ -> update)

[<RequireQualifiedAccess>]
module SignalR =
    [<RequireQualifiedAccess>]
    type Config<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =

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

    type Settings<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
        { EndpointPattern: string
          Update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
          Config: Config<'ClientApi,'ServerApi> option }

        static member GetConfigOrDefault (settings: Settings<'ClientApi,'ServerApi>) =
            match settings.Config with
            | None -> Config<'ClientApi,'ServerApi>.Default()
            | Some config -> config

        static member Create (endpointPattern: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =    
            ConfigBuilder<'ClientApi,'ServerApi>(endpointPattern, update)

    and ConfigBuilder<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct>
        (endpoint: string, 
         update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =

        let mutable state =
            { EndpointPattern = endpoint
              Update = update
              Config = None }

        member this.EndpointConfig (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            EndpointConfig = Some f }
                        |> Some }
            this

        member this.HubOptions (f: HubOptions -> unit) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            HubOptions = Some f }
                        |> Some }
            this

        member this.OnConnected (f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            OnConnected = Some f }
                        |> Some }
            this

        member this.OnDisconnected (f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            OnDisconnected = Some f }
                        |> Some }
            this

        member _.Build () = state

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fable.SignalR.Saturn")>]
do ()
