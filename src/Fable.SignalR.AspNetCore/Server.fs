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
    abstract Invoke: {| connectionId: string; message: 'ServerApi |} -> Task

type FableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    abstract Clients : IHubCallerClients<IFableHubCallerClients<'ServerApi>>
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Invoke: 'ClientApi -> Task
    abstract Send : 'ClientApi -> Task
    abstract Dispose : unit -> unit

[<EditorBrowsable(EditorBrowsableState.Never)>]
type NormalFableHubOptions<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> 'ServerApi }

and [<EditorBrowsable(EditorBrowsableState.Never)>] NormalFableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> 
    (settings: NormalFableHubOptions<'ClientApi,'ServerApi>) =
    
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Invoke msg = this.Invoke msg
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()
        
    member this.Invoke msg = 
        this.Clients.Caller.Invoke({| connectionId = this.Context.ConnectionId; message = settings.Invoke msg |})
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)

[<EditorBrowsable(EditorBrowsableState.Never)>]
type StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> 'ServerApi
      StreamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi> }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Invoke msg = this.Invoke msg
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()
    
    member this.Invoke msg = this.Clients.Caller.Invoke({| connectionId = this.Context.ConnectionId; message = settings.Invoke msg |})
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)

type [<EditorBrowsable(EditorBrowsableState.Never)>] StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> 'ServerApi
      StreamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Invoke msg = this.Invoke msg
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()
        
    member this.Invoke msg = this.Clients.Caller.Invoke({| connectionId = this.Context.ConnectionId; message = settings.Invoke msg |})
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

type [<EditorBrowsable(EditorBrowsableState.Never)>] StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> 'ServerApi
      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Invoke msg = this.Invoke msg
        member this.Send msg = this.Send msg
        member this.Dispose () = this.Dispose()
        
    member this.Invoke msg = this.Clients.Caller.Invoke({| connectionId = this.Context.ConnectionId; message = settings.Invoke msg |})
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

[<RequireQualifiedAccess>]
module internal Task =
    let toGen (f: 'a -> 'b -> #Task) =
        fun a b -> f a b :> Task

[<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
module FableHub =
    module OnConnected =
        type IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              Invoke: 'ClientApi -> 'ServerApi
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

            member this.AsNormalOptions : NormalFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke }

        let addTransient onConnected send invoke (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Send = send; Invoke = invoke; OnConnected = onConnected })
    
    type OnConnected<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        (settings: OnConnected.IOverride<'ClientApi,'ServerApi>) =

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnConnectedAsync () = 
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

    module OnDisconnected =
        type IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              Invoke: 'ClientApi -> 'ServerApi
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

            member this.AsNormalOptions : NormalFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke }

        let addTransient onDisconnected send invoke (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Send = send; Invoke = invoke; OnDisconnected = onDisconnected })
    
    type OnDisconnected<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct>
        (settings: OnDisconnected.IOverride<'ClientApi,'ServerApi>) =

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

    module Both =
        type IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              Invoke: 'ClientApi -> 'ServerApi
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

            member this.AsNormalOptions : NormalFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke }

        let addTransient onConnected onDisconnected send invoke (s: IServiceCollection) =
            s.AddTransient<IOverride<'ClientApi,'ServerApi>> <|
                System.Func<System.IServiceProvider,IOverride<'ClientApi,'ServerApi>>
                    (fun _ -> { Send = send; Invoke = invoke; OnConnected = onConnected; OnDisconnected = onDisconnected })

    type Both<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        (settings: Both.IOverride<'ClientApi,'ServerApi>) =

        inherit NormalFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnConnectedAsync () =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

    module Stream =
        module Both =
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected send invoke streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected send invoke streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            module Both =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected send invoke streamFrom streamTo (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: Both.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo })

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
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Invoke = invoke; Stream = stream; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            module Both =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun _ -> { Send = send; Stream = stream; Invoke = invoke; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream })

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
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                    member this.AsNormalOptions : NormalFableHubOptions<'ClientApi,'ServerApi> =
                        { Send = this.Send
                          Invoke = this.Invoke }

                let addTransient onConnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected })
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onDisconnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Send = send; Invoke = invoke; Stream = stream; OnDisconnected = onDisconnected })
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

            module Both =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> 'ServerApi
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit> }

                let addTransient onConnected onDisconnected send invoke stream (s: IServiceCollection) =
                    s.AddTransient<IOverride<'ClientApi,'ClientStreamApi,'ServerApi>> <|
                        System.Func<System.IServiceProvider,IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun _ -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected; OnDisconnected = onDisconnected })

            type Both<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream })

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

    let addUpdateTransient send invoke (s: IServiceCollection) =
        s.AddTransient<NormalFableHubOptions<'ClientApi,'ServerApi>> <|
            System.Func<System.IServiceProvider,NormalFableHubOptions<'ClientApi,'ServerApi>>
                (fun _ -> { Send = send; Invoke = invoke })

[<RequireQualifiedAccess>]
module SignalR =
    /// Configuration options for customizing behavior of a SignalR hub.
    [<RequireQualifiedAccess>]
    type Config<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
        { /// Customize hub endpoint conventions.
          EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option
          /// Options used to configure hub instances.
          HubOptions: (HubOptions -> unit) option
          /// Adds a logging filter with the given LogLevel.
          LogLevel: Microsoft.Extensions.Logging.LogLevel option
          /// Called when a new connection is established with the hub.
          OnConnected: (FableHub<'ClientApi,'ServerApi> -> Task<unit>) option
          /// Called when a connection with the hub is terminated.
          OnDisconnected: (exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) option }

        /// Creates an empty record.
        static member Default () =
            { EndpointConfig = None 
              HubOptions = None
              LogLevel = None
              OnConnected = None
              OnDisconnected = None }

    [<RequireQualifiedAccess>]
    module internal Config =
        let bindEnpointConfig (settings: Config<'ClientApi,'ServerApi> option) (endpointBuilder: HubEndpointConventionBuilder) =
            settings
            |> Option.bind (fun c -> c.EndpointConfig |> Option.map (fun c -> c endpointBuilder))
            |> Option.defaultValue endpointBuilder

    /// SignalR hub settings.
    type Settings<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
        { /// The endpoint used to communicate with the hub.
          EndpointPattern: string
          /// Handler for client message sends.
          Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
          /// Handler for client invocations.
          Invoke: 'ClientApi -> 'ServerApi
          /// Optional hub configuration.
          Config: Config<'ClientApi,'ServerApi> option }

        static member internal GetConfigOrDefault (settings: Settings<'ClientApi,'ServerApi>) =
            match settings.Config with
            | None -> Config<'ClientApi,'ServerApi>.Default()
            | Some config -> config

        static member internal Create (endpointPattern: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task, invoke: 'ClientApi -> 'ServerApi) =    
            ConfigBuilder<'ClientApi,'ServerApi>(endpointPattern, update, invoke)

    and ConfigBuilder<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct>
        internal 
        (endpoint: string, 
         send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task,
         invoke: 'ClientApi -> 'ServerApi) =

        let mutable state =
            { EndpointPattern = endpoint
              Send = send
              Invoke = invoke
              Config = None }

        /// Customize hub endpoint conventions.
        member this.EndpointConfig (f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            EndpointConfig = Some f }
                        |> Some }
            this
        
        /// Options used to configure hub instances.
        member this.HubOptions (f: HubOptions -> unit) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            HubOptions = Some f }
                        |> Some }
            this
            
        /// Adds a logging filter with the given LogLevel.
        member this.LogLevel (logLevel: Microsoft.Extensions.Logging.LogLevel) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            LogLevel = Some logLevel }
                        |> Some }
            this
            
        /// Called when a new connection is established with the hub.
        member this.OnConnected (f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            OnConnected = Some f }
                        |> Some }
            this
            
        /// Called when a connection with the hub is terminated.
        member this.OnDisconnected (f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            OnDisconnected = Some f }
                        |> Some }
            this

        member internal _.Build () = state

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fable.SignalR.Saturn")>]
do ()
