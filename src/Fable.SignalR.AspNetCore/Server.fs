namespace Fable.SignalR

open Fable.SignalR.Shared
#if !NET6_0_OR_GREATER
open FSharp.Control.Tasks.V2
#endif
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open System
open System.Collections.Generic
open System.ComponentModel
open System.Threading.Tasks

type IFableHubCallerClients<'ServerApi when 'ServerApi : not struct> =
    abstract Send: 'ServerApi -> Task
    abstract Invoke: InvokeArg<'ServerApi> -> Task

// fsharplint:disable-next-line
type FableHub =
    abstract Context : HubCallerContext
    abstract Groups : IGroupManager
    abstract Dispose : unit -> unit
    abstract Services : System.IServiceProvider

// fsharplint:disable-next-line
type FableHubCaller<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    abstract Clients : IHubClients<IFableHubCallerClients<'ServerApi>>
    abstract Groups : IGroupManager

// fsharplint:disable-next-line
type FableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    inherit FableHub

    abstract Clients : IHubCallerClients<IFableHubCallerClients<'ServerApi>>

module internal DIHelpers =
    let injectFableHub<'Hub,'ClientApi,'ServerApi when 'Hub :> FableHub<'ClientApi,'ServerApi> and 'Hub :> Hub<IFableHubCallerClients<'ServerApi>>> (services: IServiceCollection) =
        services.AddSingleton<FableHubCaller<'ClientApi,'ServerApi>>(fun (s: IServiceProvider) ->
            s.GetRequiredService<IHubContext<'Hub,IFableHubCallerClients<'ServerApi>>>() 
            |> fun hub ->
                { new FableHubCaller<'ClientApi,'ServerApi> with 
                    member _.Clients = hub.Clients
                    member _.Groups = hub.Groups }
        )

open DIHelpers

[<EditorBrowsable(EditorBrowsableState.Never)>]
type BaseFableHubOptions<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
      Services: System.IServiceProvider }

and [<EditorBrowsable(EditorBrowsableState.Never)>] BaseFableHub<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> 
    (settings: BaseFableHubOptions<'ClientApi,'ServerApi>) =
    
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Dispose () = this.Dispose()
        member _.Services = settings.Services
        
    member this.Invoke (msg: 'ClientApi, invocationId: System.Guid) =
        task {
            let! message = settings.Invoke msg (this :> FableHub)
            do! this.Clients.Caller.Invoke({ connectionId = this.Context.ConnectionId; invocationId = invocationId; message = message })
        } :> Task

    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)

    static member AddServices (send, invoke, services: IServiceCollection) =
        services
            .AddTransient<BaseFableHubOptions<'ClientApi,'ServerApi>>(
                System.Func<System.IServiceProvider,BaseFableHubOptions<'ClientApi,'ServerApi>>
                    (fun sp -> { Send = send; Invoke = invoke; Services = sp }))
        |> injectFableHub<BaseFableHub<'ClientApi,'ServerApi>,'ClientApi,'ServerApi>

[<EditorBrowsable(EditorBrowsableState.Never)>]
type StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
      StreamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
      Services: System.IServiceProvider }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Dispose () = this.Dispose()
        member _.Services = settings.Services
    
    member this.Invoke (msg: 'ClientApi, invocationId: System.Guid) =
        task {
            let! message = settings.Invoke msg (this :> FableHub)
            do! this.Clients.Caller.Invoke({ connectionId = this.Context.ConnectionId; invocationId = invocationId; message = message })
        } :> Task
        
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)

    static member AddServices (send, invoke, stream, s: IServiceCollection) =
        s.AddTransient<StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(
            System.Func<System.IServiceProvider,StreamFromFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                (fun sp -> { Send = send; Invoke = invoke; StreamFrom = stream; Services = sp }))
        |> injectFableHub<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

type [<EditorBrowsable(EditorBrowsableState.Never)>] StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
      StreamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
      Services: System.IServiceProvider }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Dispose () = this.Dispose()
        member _.Services = settings.Services
        
    member this.Invoke (msg: 'ClientApi, invocationId: System.Guid) =
        task {
            let! message = settings.Invoke msg (this :> FableHub)
            do! this.Clients.Caller.Invoke({ connectionId = this.Context.ConnectionId; invocationId = invocationId; message = message })
        } :> Task

    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

    static member AddServices (send, invoke, stream, s: IServiceCollection) =
        s.AddTransient<StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>>(
            System.Func<System.IServiceProvider,StreamToFableHubOptions<'ClientApi,'ClientStreamApi,'ServerApi>>
                (fun sp -> { Send = send; Invoke = invoke; StreamTo = stream; Services = sp }))
        |> injectFableHub<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>,'ClientApi,'ServerApi>

type [<EditorBrowsable(EditorBrowsableState.Never)>] StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct> =

    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
      Services: System.IServiceProvider }

and [<EditorBrowsable(EditorBrowsableState.Never)>] StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
    when 'ClientApi : not struct and 'ServerApi : not struct>
    (settings: StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =
        
    inherit Hub<IFableHubCallerClients<'ServerApi>>()

    interface FableHub<'ClientApi,'ServerApi> with
        member this.Clients = this.Clients
        member this.Context = this.Context
        member this.Groups = this.Groups
        member this.Dispose () = this.Dispose()
        member _.Services = settings.Services
        
    member this.Invoke (msg: 'ClientApi, invocationId: System.Guid) =
        task {
            let! message = settings.Invoke msg (this :> FableHub)
            do! this.Clients.Caller.Invoke({ connectionId = this.Context.ConnectionId; invocationId = invocationId; message = message })
        } :> Task
    member this.Send msg = settings.Send msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamFrom msg = settings.StreamFrom msg (this :> FableHub<'ClientApi,'ServerApi>)
    member this.StreamTo msg = settings.StreamTo msg (this :> FableHub<'ClientApi,'ServerApi>)

    static member AddServices (send, invoke, streamFrom, streamTo, s: IServiceCollection) =
        s.AddTransient<StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(
            System.Func<System.IServiceProvider,StreamBothFableHubOptions<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                (fun sp -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; Services = sp }))
        |> injectFableHub<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

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
              Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
              Services: System.IServiceProvider }

            member this.AsNormalOptions : BaseFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke
                  Services = this.Services }
    
    type OnConnected<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        (settings: OnConnected.IOverride<'ClientApi,'ServerApi>) =

        inherit BaseFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnConnectedAsync () = 
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

        static member AddServices (onConnected, send, invoke, s: IServiceCollection) =
            s.AddTransient<OnConnected.IOverride<'ClientApi,'ServerApi>>(
                System.Func<System.IServiceProvider,OnConnected.IOverride<'ClientApi,'ServerApi>>
                    (fun sp -> { Send = send; Invoke = invoke; OnConnected = onConnected; Services = sp }))
            |> injectFableHub<OnConnected<'ClientApi,'ServerApi>,'ClientApi,'ServerApi>

    module OnDisconnected =
        type IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
              Services: System.IServiceProvider }

            member this.AsNormalOptions : BaseFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke
                  Services = this.Services }
    
    type OnDisconnected<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct>
        (settings: OnDisconnected.IOverride<'ClientApi,'ServerApi>) =

        inherit BaseFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

        static member AddServices (onDisconnected, send, invoke, s: IServiceCollection) =
            s.AddTransient<OnDisconnected.IOverride<'ClientApi,'ServerApi>>(
                System.Func<System.IServiceProvider,OnDisconnected.IOverride<'ClientApi,'ServerApi>>
                    (fun sp -> { Send = send; Invoke = invoke; OnDisconnected = onDisconnected; Services = sp }))
            |> injectFableHub<OnDisconnected<'ClientApi,'ServerApi>,'ClientApi,'ServerApi>

    module Both =
        type IOverride<'ClientApi,'ServerApi
            when 'ClientApi : not struct and 'ServerApi : not struct> =

            { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
              Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
              OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
              OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
              Services: System.IServiceProvider }

            member this.AsNormalOptions : BaseFableHubOptions<'ClientApi,'ServerApi> =
                { Send = this.Send
                  Invoke = this.Invoke
                  Services = this.Services }

    type Both<'ClientApi,'ServerApi
        when 'ClientApi : not struct and 'ServerApi : not struct> 
        (settings: Both.IOverride<'ClientApi,'ServerApi>) =

        inherit BaseFableHub<'ClientApi,'ServerApi>(settings.AsNormalOptions)

        override this.OnConnectedAsync () =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnConnected :> Task

        override this.OnDisconnectedAsync (err: exn) =
            this :> FableHub<'ClientApi,'ServerApi>
            |> settings.OnDisconnected err :> Task

        static member AddServices (onConnected, onDisconnected, send, invoke, s: IServiceCollection) =
            s.AddTransient<Both.IOverride<'ClientApi,'ServerApi>>(
                System.Func<System.IServiceProvider,Both.IOverride<'ClientApi,'ServerApi>>
                    (fun sp -> { Send = send; Invoke = invoke; OnConnected = onConnected; OnDisconnected = onDisconnected; Services = sp }))
            |> injectFableHub<Both<'ClientApi,'ServerApi>,'ClientApi,'ServerApi>

    module Stream =
        module Both =
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }
        
            type OnConnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo; Services = settings.Services })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                static member AddServices (onConnected, send, invoke, streamFrom, streamTo, s: IServiceCollection) =
                    s.AddTransient<OnConnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,OnConnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected; Services = sp }))
                    |> injectFableHub<OnConnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }
        
            type OnDisconnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo; Services = settings.Services })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onDisconnected, send, invoke, streamFrom, streamTo, s: IServiceCollection) =
                    s.AddTransient<OnDisconnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,OnDisconnected.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<OnDisconnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

            module Both =
                type IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      StreamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      StreamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }

            type Both<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                internal (settings: Both.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
                    ({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.StreamFrom; StreamTo = settings.StreamTo; Services = settings.Services })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onConnected, onDisconnected, send, invoke, streamFrom, streamTo, s: IServiceCollection) =
                    s.AddTransient<Both.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,Both.IOverride<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Invoke = invoke; StreamFrom = streamFrom; StreamTo = streamTo; OnConnected = onConnected; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<Both<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

        module From =
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream; Services = settings.Services })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                static member AddServices (onConnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected; Services = sp }))
                    |> injectFableHub<OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream; Services = settings.Services })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onDisconnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Invoke = invoke; Stream = stream; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

            module Both =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }

            type Both<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

                inherit StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamFrom = settings.Stream; Services = settings.Services })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onConnected, onDisconnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(
                        System.Func<System.IServiceProvider,Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>
                            (fun sp -> { Send = send; Stream = stream; Invoke = invoke; OnConnected = onConnected; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<Both<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ServerApi>

        module To =
            module OnConnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }

                    member this.AsNormalOptions : BaseFableHubOptions<'ClientApi,'ServerApi> =
                        { Send = this.Send
                          Invoke = this.Invoke
                          Services = this.Services }
        
            type OnConnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream; Services = settings.Services })

                override this.OnConnectedAsync () = 
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                static member AddServices (onConnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>(
                        System.Func<System.IServiceProvider,OnConnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun sp -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected; Services = sp }))
                    |> injectFableHub<OnConnected<'ClientApi,'ClientStreamApi,'ServerApi>,'ClientApi,'ServerApi>

            module OnDisconnected =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }
        
            type OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct>
                (settings: OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream; Services = settings.Services })

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onDisconnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>(
                        System.Func<System.IServiceProvider,OnDisconnected.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun sp -> { Send = send; Invoke = invoke; Stream = stream; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi>,'ClientApi,'ServerApi>

            module Both =
                type IOverride<'ClientApi,'ClientStreamApi,'ServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> =

                    { Send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task
                      Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
                      Stream: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> Task
                      OnConnected: FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      OnDisconnected: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>
                      Services: System.IServiceProvider }

            type Both<'ClientApi,'ClientStreamApi,'ServerApi
                when 'ClientApi : not struct and 'ServerApi : not struct> 
                (settings: Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>) =

                inherit StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>({ Send = settings.Send; Invoke = settings.Invoke; StreamTo = settings.Stream; Services = settings.Services })

                override this.OnConnectedAsync () =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnConnected :> Task

                override this.OnDisconnectedAsync (err: exn) =
                    this :> FableHub<'ClientApi,'ServerApi>
                    |> settings.OnDisconnected err :> Task

                static member AddServices (onConnected, onDisconnected, send, invoke, stream, s: IServiceCollection) =
                    s.AddTransient<Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>(
                        System.Func<System.IServiceProvider,Both.IOverride<'ClientApi,'ClientStreamApi,'ServerApi>>
                            (fun sp -> { Send = send; Invoke = invoke; Stream = stream; OnConnected = onConnected; OnDisconnected = onDisconnected; Services = sp }))
                    |> injectFableHub<Both<'ClientApi,'ClientStreamApi,'ServerApi>,'ClientApi,'ServerApi>
        
[<RequireQualifiedAccess>]
module SignalR =
    /// Configuration options for customizing behavior of a SignalR hub.
    [<RequireQualifiedAccess>]
    type Config<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> =
        { /// App configuration after app.UseRouting() is called.
          AfterUseRouting: (IApplicationBuilder -> IApplicationBuilder) option
          /// App configuration before app.UseRouting() is called.
          BeforeUseRouting: (IApplicationBuilder -> IApplicationBuilder) option
          /// Inject a Websocket middleware to support bearer tokens.
          ///
          /// Default: false
          EnableBearerAuth: bool 
          /// Customize hub endpoint conventions.
          EndpointConfig: (HubEndpointConventionBuilder -> HubEndpointConventionBuilder) option
          /// Options used to configure hub instances.
          HubOptions: (HubOptions -> unit) option
          /// Adds a logging filter with the given LogLevel.
          LogLevel: Microsoft.Extensions.Logging.LogLevel option
          /// Disable app.UseRouting() configuration from this library.
          ///
          /// *You must configure this yourself if you do this!*
          ///
          /// Default: false
          NoRouting: bool
          /// Called when a new connection is established with the hub.
          OnConnected: (FableHub<'ClientApi,'ServerApi> -> Task<unit>) option
          /// Called when a connection with the hub is terminated.
          OnDisconnected: (exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) option
          /// Enable MessagePack binary (de)serialization instead of JSON.
          UseMessagePack: bool
          /// Configure the SignalR server.
          UseServerBuilder: (ISignalRServerBuilder -> ISignalRServerBuilder) option }

        /// Creates an empty record.
        static member Default () =
            { AfterUseRouting = None
              BeforeUseRouting = None
              EnableBearerAuth = false
              EndpointConfig = None 
              HubOptions = None
              LogLevel = None
              NoRouting = false
              OnConnected = None
              OnDisconnected = None
              UseMessagePack = false
              UseServerBuilder = None }

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
          Invoke: 'ClientApi -> FableHub -> Task<'ServerApi>
          /// Optional hub configuration.
          Config: Config<'ClientApi,'ServerApi> option }

        static member internal GetConfigOrDefault (settings: Settings<'ClientApi,'ServerApi>) =
            match settings.Config with
            | None -> Config<'ClientApi,'ServerApi>.Default()
            | Some config -> config

        static member internal Create (endpointPattern: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task, invoke: 'ClientApi -> FableHub -> Task<'ServerApi>) =    
            ConfigBuilder<'ClientApi,'ServerApi>(endpointPattern, update, invoke)

    and ConfigBuilder<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> 
        (endpoint: string, 
         send: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task,
         invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
         ?config: Config<'ClientApi,'ServerApi>) =

        let mutable state =
            { EndpointPattern = endpoint
              Send = send
              Invoke = invoke
              Config = config }

        new (settings: Settings<'ClientApi,'ServerApi>) = ConfigBuilder(settings.EndpointPattern, settings.Send, settings.Invoke, ?config = settings.Config)

        /// App configuration after app.UseRouting() is called.
        member this.AfterUseRouting (appConfig: IApplicationBuilder -> IApplicationBuilder) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            AfterUseRouting = Some appConfig }
                        |> Some }
            this

        /// App configuration before app.UseRouting() is called.
        member this.BeforeUseRouting (appConfig: IApplicationBuilder -> IApplicationBuilder) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            BeforeUseRouting = Some appConfig }
                        |> Some }
            this

        /// Inject a Websocket middleware to support bearer tokens.
        member this.EnableBearerAuth () =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            EnableBearerAuth = true }
                        |> Some }
            this

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
            
        /// Disable app.UseRouting() configuration.
        ///
        /// *You must configure this yourself if you do this!*
        member this.NoRouting () =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            NoRouting = true }
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

        /// Enable MessagePack binary (de)serialization instead of JSON.
        member this.UseMessagePack () =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            UseMessagePack = true }
                        |> Some }
            this
            
        /// Configure the SignalR server.
        member this.UseServerBuilder (handler: ISignalRServerBuilder -> ISignalRServerBuilder) =
            state <-
                { state with
                    Config =
                        { Settings<'ClientApi,'ServerApi>.GetConfigOrDefault state with
                            UseServerBuilder = Some handler }
                        |> Some }
            this

        /// Returns the SignalR settings.
        member _.Build () = state

[<assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Fable.SignalR.Saturn")>]
do ()
