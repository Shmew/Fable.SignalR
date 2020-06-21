namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open System.ComponentModel
    open System.Threading.Tasks

    module SignalR =
        [<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
        module State =
            type Empty = | Init
    
            type Endpoint = | Value of string
    
        type SettingsBuilder () =
            member _.Yield(_) =
                State.Empty.Init
    
            [<CustomOperation("endpoint")>]
            member _.Endpoint (State.Empty.Init, value: string) = 
                State.Endpoint.Value value
    
            [<CustomOperation("update")>]
            member _.Update (State.Endpoint.Value state, f) : SignalR.Settings<'ClientApi,'ServerApi> = 
                { EndpointPattern = state
                  Update = f
                  Config = None }
    
            [<CustomOperation("with_endpoint_config")>]
            member _.EndpointConfig (state: SignalR.Settings<'ClientApi,'ServerApi>, f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
                { state with
                    Config =
                        { SignalR.Settings.GetConfigOrDefault state with
                            EndpointConfig = Some f }
                        |> Some }
    
            [<CustomOperation("with_hub_options")>]
            member _.HubOptions (state: SignalR.Settings<'ClientApi,'ServerApi>, f: HubOptions -> unit) =
                { state with
                    Config =
                        { SignalR.Settings.GetConfigOrDefault state with
                            HubOptions = Some f }
                        |> Some }
    
            [<CustomOperation("with_on_connected")>]
            member _.OnConnected (state: SignalR.Settings<'ClientApi,'ServerApi>, f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                { state with
                    Config =
                        { SignalR.Settings.GetConfigOrDefault state with
                            OnConnected = Some f }
                        |> Some }
    
            [<CustomOperation("with_on_disconnected")>]
            member _.OnDisconnected (state: SignalR.Settings<'ClientApi,'ServerApi>, f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                { state with
                    Config =
                        { SignalR.Settings.GetConfigOrDefault state with
                            OnDisconnected = Some f }
                        |> Some }
            
            member _.Run (state: SignalR.Settings<'ClientApi,'ServerApi>) = state

        module Stream =
            [<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
            module State =
                type Empty = | Init
    
                type Endpoint = | Value of string

                type Update<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi
                    when 'ClientApi : not struct and 'ServerApi : not struct> = 

                    | Value of endpoint: string * update: ('ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task)
    
            type SettingsBuilder () =
                member _.Yield(_) =
                    State.Empty.Init
    
                [<CustomOperation("endpoint")>]
                member _.Endpoint (State.Empty.Init, value: string) = 
                    State.Endpoint.Value value
    
                [<CustomOperation("update")>]
                member _.Update (State.Endpoint.Value state, f) = 
                    State.Update.Value(state, f)

                [<CustomOperation("stream")>]
                member _.Stream (State.Update.Value (endpoint, update), f) : SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> = 
                    { EndpointPattern = endpoint
                      Update = update
                      Stream = f
                      Config = None }
    
                [<CustomOperation("with_endpoint_config")>]
                member _.EndpointConfig 
                    (state: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>, 
                     f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
                    
                    { state with
                        Config =
                            { SignalR.Stream.Settings.GetConfigOrDefault state with
                                EndpointConfig = Some f }
                            |> Some }
    
                [<CustomOperation("with_hub_options")>]
                member _.HubOptions (state: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>, f: HubOptions -> unit) =
                    { state with
                        Config =
                            { SignalR.Stream.Settings.GetConfigOrDefault state with
                                HubOptions = Some f }
                            |> Some }
    
                [<CustomOperation("with_on_connected")>]
                member _.OnConnected 
                    (state: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>, 
                     f: StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) =
                    
                    { state with
                        Config =
                            { SignalR.Stream.Settings.GetConfigOrDefault state with
                                OnConnected = Some f }
                            |> Some }
    
                [<CustomOperation("with_on_disconnected")>]
                member _.OnDisconnected 
                    (state: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>, 
                     f: exn -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task<unit>) =
                    
                    { state with
                        Config =
                            { SignalR.Stream.Settings.GetConfigOrDefault state with
                                OnDisconnected = Some f }
                            |> Some }
                
                member _.Run (state: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) = state
    
    [<AutoOpen>]
    module Builder =
        // fsharplint:disable-next-line
        let configure_signalr = SignalR.SettingsBuilder()
        
        // fsharplint:disable-next-line
        let configure_streaming_signalr = SignalR.Stream.SettingsBuilder()

    type Saturn.Application.ApplicationBuilder with
        [<CustomOperation("use_signalr")>]
        member this.UseSignalR(state, settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            this.ServiceConfig(state, fun services -> services.AddSignalR(settings))
            |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings))

        [<CustomOperation("use_streaming_signalr")>]
        member this.UseStreamingSignalR(state, settings: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =
            this.ServiceConfig(state, fun services -> services.AddSignalR(settings))
            |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings))
