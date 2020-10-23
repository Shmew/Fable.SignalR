namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open Microsoft.Extensions.Logging
    open System.Collections.Generic
    open System.ComponentModel
    open System.Threading.Tasks

    module SignalR =
        [<EditorBrowsable(EditorBrowsableState.Never);RequireQualifiedAccess>]
        module State =
            type Empty = | Init
    
            type Endpoint = | Value of string

            type Send<'ClientApi,'ServerApi when 'ClientApi : not struct and 'ServerApi : not struct> = 
                | Value of endpoint:string * send:('ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task)

            type Settings<'ClientApi,'ClientStreamToApi,'ClientStreamFromApi,'ServerApi,'ServerStreamApi
                when 'ClientApi : not struct and 'ServerApi : not struct> =
                | HasStreamBoth of SignalR.Settings<'ClientApi,'ServerApi> * 
                                    ('ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) * 
                                    (IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task)
                | HasStreamFrom of SignalR.Settings<'ClientApi,'ServerApi> * ('ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>)
                | HasStreamTo of SignalR.Settings<'ClientApi,'ServerApi> * (IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task)
                | NoStream of SignalR.Settings<'ClientApi,'ServerApi>

                member this.MapSettings (mapper: SignalR.Settings<'ClientApi,'ServerApi> -> SignalR.Settings<'ClientApi,'ServerApi>) =
                    match this with
                    | HasStreamBoth(settings,streamFrom,streamTo) -> mapper settings |> fun res -> Settings.HasStreamBoth(res, streamFrom, streamTo)
                    | HasStreamFrom(settings,stream) -> mapper settings |> fun res -> Settings.HasStreamFrom(res, stream)
                    | HasStreamTo(settings,stream) -> mapper settings |> fun res -> Settings.HasStreamTo(res, stream)
                    | NoStream settings -> mapper settings |> Settings.NoStream

        type SettingsBuilder () =
            member _.Yield(_) =
                State.Empty.Init
    
            /// The endpoint used to communicate with the hub.
            [<CustomOperation("endpoint")>]
            member _.Endpoint (State.Empty.Init, value: string) = 
                State.Endpoint.Value value
    
            /// Handler for client message sends.
            [<CustomOperation("send")>]
            member _.Send (State.Endpoint.Value state, f: 'a -> FableHub<'a,'d> -> #Task) = 

                let f = (fun msg hub -> (f msg hub) :> Task)

                let send : State.Send<_,_> = State.Send.Value(state, f)

                send
                
            /// Handler for client invocations.
            [<CustomOperation("invoke")>]
            member _.Invoke (State.Send.Value (endpoint,send), f: 'a -> FableHub -> Task<'b>) = 

                let settings : SignalR.Settings<'a,'b> =
                    { EndpointPattern = endpoint
                      Send = send
                      Invoke = f
                      Config = None }

                State.Settings.NoStream settings
                
            /// Handler for streaming to the client.
            [<CustomOperation("stream_from")>]
            member _.StreamFrom (state: State.Settings<_,_,_,_,_>, f) =

                match state with
                | State.HasStreamBoth(settings,_,streamTo) -> State.Settings.HasStreamBoth(settings, f, streamTo)
                | State.HasStreamFrom(settings,_) -> State.Settings.HasStreamFrom(settings, f)
                | State.HasStreamTo(settings,streamTo) -> State.Settings.HasStreamBoth(settings, f, streamTo)
                | State.NoStream(settings) -> State.Settings.HasStreamFrom(settings, f)
                
            ///  Handler for streaming from the client.
            [<CustomOperation("stream_to")>]
            member _.StreamTo(state: State.Settings<_,_,_,_,_>, f: IAsyncEnumerable<_> -> FableHub<_,_> -> #Task) =

                let f = (fun ae hub -> (f ae hub) :> Task)

                match state with
                | State.HasStreamBoth(settings,streamFrom,_) -> State.Settings.HasStreamBoth(settings, streamFrom, f)
                | State.HasStreamFrom(settings,streamFrom) -> State.Settings.HasStreamBoth(settings, streamFrom, f)
                | State.HasStreamTo(settings,_) -> State.Settings.HasStreamTo(settings, f)
                | State.NoStream(settings) -> State.Settings.HasStreamTo(settings, f)
                
            /// Disable app.UseRouting() configuration.
            ///
            /// *You must configure this yourself if you do this!*
            [<CustomOperation("no_routing")>]
            member _.NoRouting (state: State.Settings<_,_,_,_,_>) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                NoRouting = true }
                            |> Some }

            /// Inject a Websocket middleware to support bearer tokens.
            [<CustomOperation("use_bearer_auth")>]
            member _.BearerAuth (state: State.Settings<_,_,_,_,_>) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                EnableBearerAuth = true }
                            |> Some }

            /// Enable MessagePack binary (de)serialization instead of JSON.
            [<CustomOperation("use_messagepack")>]
            member _.MessagePack (state: State.Settings<_,_,_,_,_>) =
                state.MapSettings <| fun state ->  
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                UseMessagePack = true }
                            |> Some }
            
            /// Configure the SignalR server.
            [<CustomOperation("use_server_builder")>]
            member _.UseServerBuilder (state: State.Settings<_,_,_,_,_>, f: ISignalRServerBuilder -> ISignalRServerBuilder) =
                state.MapSettings <| fun state ->  
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                UseServerBuilder = Some f }
                            |> Some }

            /// App configuration after app.UseRouting() is called.
            [<CustomOperation("with_after_routing")>]
            member _.AfterRouting (state: State.Settings<_,_,_,_,_>, f: IApplicationBuilder -> IApplicationBuilder) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                AfterUseRouting = Some f }
                            |> Some }

            /// App configuration before app.UseRouting() is called.
            [<CustomOperation("with_before_routing")>]
            member _.BeforeRouting (state: State.Settings<_,_,_,_,_>, f: IApplicationBuilder -> IApplicationBuilder) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                BeforeUseRouting = Some f }
                            |> Some }

            /// Customize hub endpoint conventions.
            [<CustomOperation("with_endpoint_config")>]
            member _.EndpointConfig (state: State.Settings<_,_,_,_,_>, f: HubEndpointConventionBuilder -> HubEndpointConventionBuilder) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                EndpointConfig = Some f }
                            |> Some }

            /// Options used to configure hub instances.
            [<CustomOperation("with_hub_options")>]
            member _.HubOptions (state: State.Settings<_,_,_,_,_>, f: HubOptions -> unit) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                HubOptions = Some f }
                            |> Some }
    
            /// Adds a logging filter with the given LogLevel.
            [<CustomOperation("with_log_level")>]
            member _.LogLevel (state: State.Settings<_,_,_,_,_>, logLevel: Microsoft.Extensions.Logging.LogLevel) =
                state.MapSettings <| fun state ->
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                LogLevel = Some logLevel }
                            |> Some }

            /// Called when a new connection is established with the hub.
            [<CustomOperation("with_on_connected")>]
            member _.OnConnected (state: State.Settings<_,_,_,_,_>, f: FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                state.MapSettings <| fun state -> 
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                OnConnected = Some f }
                            |> Some }
    
            /// Called when a connection with the hub is terminated.
            [<CustomOperation("with_on_disconnected")>]
            member _.OnDisconnected (state: State.Settings<_,_,_,_,_>, f: exn -> FableHub<'ClientApi,'ServerApi> -> Task<unit>) =
                state.MapSettings <| fun state ->  
                    { state with
                        Config =
                            { SignalR.Settings.GetConfigOrDefault state with
                                OnDisconnected = Some f }
                            |> Some }
                
            member _.Run (state: State.Settings<_,_,_,_,_>) = 
                match state with
                | State.HasStreamBoth(settings,streamFrom,streamTo) -> settings, Some streamFrom, Some streamTo
                | State.HasStreamFrom(settings,stream) -> settings, Some stream, None
                | State.HasStreamTo(settings,stream) -> settings, None, Some stream
                | State.NoStream settings -> settings, None, None
    
    [<AutoOpen>]
    module Builder =
        /// Creates a SignalR hub configuration.
        // fsharplint:disable-next-line
        let configure_signalr = SignalR.SettingsBuilder()

    type Saturn.Application.ApplicationBuilder with
        /// Adds a SignalR hub into the application.
        [<CustomOperation("use_signalr")>]
        member this.UseSignalR
            (state, settings: SignalR.Settings<'ClientApi,'ServerApi> * 
                ('ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) option *
                (IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> Task) option) =
            
            let settings,streamFrom,streamTo = settings

            match streamFrom,streamTo with
            | Some streamFrom, Some streamTo ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings, streamFrom, streamTo))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings, streamFrom, streamTo))
            | Some streamFrom, None ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings, streamFrom))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings, streamFrom))
            | None, Some streamTo ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings, streamTo))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings, streamTo))
            | _ ->
                this.ServiceConfig(state, fun services -> services.AddSignalR(settings))
                |> fun state -> this.AppConfig(state, fun app -> app.UseSignalR(settings))
            |> fun state -> 
                settings.Config
                |> Option.bind(fun o -> o.LogLevel)
                |> function
                | Some logLevel -> this.Logging(state, fun l -> l.AddFilter("Microsoft.AspNetCore.SignalR", logLevel) |> ignore)
                | None -> state
