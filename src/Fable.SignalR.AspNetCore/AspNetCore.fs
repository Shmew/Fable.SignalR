namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.Remoting.Json
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.DependencyInjection
    open System.Collections.Generic
    open System.Threading.Tasks
    
    type IServiceCollection with
        member this.AddSignalR(settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            let config = 
                let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)

                match settings.Config with
                | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                    {| Transient = FableHub.OnConnected.addTransient onConnect settings.Update
                       HubOptions = hubOptions |}

                | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.OnDisconnected.addTransient onDisconnect settings.Update
                       HubOptions = hubOptions |}

                | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Both.addTransient onConnect onDisconnect settings.Update
                       HubOptions = hubOptions |}
                | _ ->
                    {| Transient = FableHub.addTransient settings.Update
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<FableHub<'ClientApi,'ServerApi>>(System.Action<HubOptions<FableHub<'ClientApi,'ServerApi>>>(hubOptions))
                    .Services |> config.Transient
            | None ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR(settings: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =
            let config = 
                let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)

                match settings.Config with
                | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                    {| Transient = FableHub.Stream.OnConnected.addTransient onConnect settings.Update settings.Stream
                       HubOptions = hubOptions |}

                | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.OnDisconnected.addTransient onDisconnect settings.Update settings.Stream
                       HubOptions = hubOptions |}

                | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.Both.addTransient onConnect onDisconnect settings.Update settings.Stream
                       HubOptions = hubOptions |}
                | _ ->
                    {| Transient = FableHub.Stream.addTransient settings.Update settings.Stream
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>(
                        System.Action<HubOptions<StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>>(hubOptions))
                    .Services |> config.Transient
            | None ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR(endpoint: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task) =
            SignalR.ConfigBuilder(endpoint, update).Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string, 
             update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task, 
             stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> 
                IAsyncEnumerable<'StreamServerApi>) =
            
            SignalR.Stream.ConfigBuilder(endpoint, update, stream).Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string, 
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> Task, 
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, update) 
            |> config 
            |> fun res -> res.Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string, 
             update: 'ClientApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> Task, 
             stream: 'ClientStreamApi -> StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> 
                IAsyncEnumerable<'StreamServerApi>,
             config: SignalR.Stream.ConfigBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi> -> 
                SignalR.Stream.ConfigBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =

            SignalR.Stream.ConfigBuilder(endpoint, update, stream) 
            |> config 
            |> fun res -> res.Build()
            |> this.AddSignalR

    type IApplicationBuilder with
        member this.UseSignalR(settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            let config = 
                match settings.Config with
                | Some { OnConnected = Some _; OnDisconnected = None } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.OnConnected<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = None; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.OnDisconnected<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = Some _; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Both<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | _ ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)

        member this.UseSignalR(settings: SignalR.Stream.Settings<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>) =
            let config = 
                match settings.Config with
                | Some { OnConnected = Some _; OnDisconnected = None } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>(settings.EndpointPattern)
                        |> SignalR.Stream.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = None; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>(settings.EndpointPattern)
                        |> SignalR.Stream.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = Some _; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.Both<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>(settings.EndpointPattern)
                        |> SignalR.Stream.Config.bindEnpointConfig settings.Config
                | _ ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<StreamingFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'StreamServerApi>>(settings.EndpointPattern)
                        |> SignalR.Stream.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
