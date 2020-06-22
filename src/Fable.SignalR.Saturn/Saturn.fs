namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open System.Collections.Generic
    open System.ComponentModel
    open System.Threading.Tasks

    module SignalR =
        [<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
        module State =
            type Empty = | Init
    
            type Endpoint = | Value of string

            type Settings<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> =
                | HasStream of SignalR.Settings<'ClientApi,'ServerApi> * ('ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>)
                | NoStream of SignalR.Settings<'ClientApi,'ServerApi>

                member this.mapSettings (mapper: SignalR.Settings<'ClientApi,'ServerApi> -> SignalR.Settings<'ClientApi,'ServerApi>) =
                    match this with
                    | HasStream(settings,stream) -> mapper settings |> fun res -> Settings.HasStream(res, stream)
                    | NoStream settings -> mapper settings |> Settings.NoStream

        type SettingsBuilder () =
            member _.Yield(_) =
                State.Empty.Init
    
            [<CustomOperation("endpoint")>]
            member _.Endpoint (State.Empty.Init, value: string) = 
                State.Endpoint.Value value
    
            [<CustomOperation("update")>]
            member _.Update (State.Endpoint.Value state, f) = 
                let settings : SignalR.Settings<_,_> =
                    { EndpointPattern = state
                      Update = f
                      Config = None }

                State.Settings.NoStream settings

            [<CustomOperation("stream")>]
            member _.Stream (state: State.Settings<_,_,_,_>, f) = 
                match state with
                | State.HasStream(settings,_) -> settings
                | State.NoStream(settings) -> settings
                |> fun settings -> State.Settings.HasStream(settings, f)

            [<CustomOperation("with_endpoint_config")>]
            member _.EndpointConfig (state: State.Settings<_,_,_,_>, f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
                state.mapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                EndpointConfig = Some f }
                            |> Some }
    
            [<CustomOperation("with_hub_options")>]
            member _.HubOptions (state: State.Settings<_,_,_,_>, f: HubOptions -> unit) =
                state.mapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                HubOptions = Some f }
                            |> Some }
    
            [<CustomOperation("with_on_connected")>]
            member _.OnConnected (state: State.Settings<_,_,_,_>, f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                state.mapSettings <| fun state -> 
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                OnConnected = Some f }
                            |> Some }
    
            [<CustomOperation("with_on_disconnected")>]
            member _.OnDisconnected (state: State.Settings<_,_,_,_>, f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                state.mapSettings <| fun state ->  
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                OnDisconnected = Some f }
                            |> Some }
                
            member _.Run (state: State.Settings<_,_,_,_>) = 
                match state with
                | State.HasStream(settings,stream) -> settings, Some stream
                | State.NoStream settings -> settings, None
    
    [<AutoOpen>]
    module Builder =
        // fsharplint:disable-next-line
        let configure_signalr = SignalR.SettingsBuilder()

    type Saturn.Application.ApplicationBuilder with
        [<CustomOperation("use_signalr")>]
        member this.UseSignalR
            (state, settings: SignalR.Settings<'ClientApi,'ServerApi> * 
                ('ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) option) =
            
            let settings,stream = settings

            match stream with
            | Some stream ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings, stream))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings, stream))
            | None ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings))

