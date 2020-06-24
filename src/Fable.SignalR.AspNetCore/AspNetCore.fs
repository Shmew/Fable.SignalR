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
        member this.AddSignalR (settings: SignalR.Settings<'ClientApi,'ServerApi>) =

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
                    {| Transient = FableHub.addUpdateTransient settings.Update
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<NormalFableHub<'ClientApi,'ServerApi>>(
                        System.Action<HubOptions<NormalFableHub<'ClientApi,'ServerApi>>>(hubOptions))
                    .Services |> config.Transient
            | _ ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =

            let config = 
                let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)

                match settings.Config with
                | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                    {| Transient = FableHub.Stream.From.OnConnected.addTransient onConnect settings.Update streamFrom
                       HubOptions = hubOptions |}

                | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.From.OnDisconnected.addTransient onDisconnect settings.Update streamFrom
                       HubOptions = hubOptions |}

                | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.From.Both.addTransient onConnect onDisconnect settings.Update streamFrom
                       HubOptions = hubOptions |}
                | _ ->
                    {| Transient = 
                        { Updater = (fun msg hub -> settings.Update msg hub)
                          StreamFrom = streamFrom }
                        |> FableHub.Stream.From.addTransient
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(
                        System.Action<HubOptions<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>>(hubOptions))
                    .Services |> config.Transient
            | _ ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let config = 
                let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)

                match settings.Config with
                | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                    {| Transient = FableHub.Stream.To.OnConnected.addTransient onConnect settings.Update (Task.toGen streamTo)
                       HubOptions = hubOptions |}

                | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.To.OnDisconnected.addTransient onDisconnect settings.Update (Task.toGen streamTo)
                       HubOptions = hubOptions |}

                | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.To.Both.addTransient onConnect onDisconnect settings.Update (Task.toGen streamTo)
                       HubOptions = hubOptions |}
                | _ ->
                    {| Transient = 
                        { Updater = (fun msg hub -> settings.Update msg hub)
                          StreamTo = (Task.toGen streamTo) }
                        |> FableHub.Stream.To.addTransient
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>>(
                        System.Action<HubOptions<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>>>(hubOptions))
                    .Services |> config.Transient
            | _ ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let config = 
                let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)

                match settings.Config with
                | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                    {| Transient = FableHub.Stream.Both.OnConnected.addTransient onConnect settings.Update streamFrom (Task.toGen streamTo)
                       HubOptions = hubOptions |}

                | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.Both.OnDisconnected.addTransient onDisconnect settings.Update streamFrom (Task.toGen streamTo)
                       HubOptions = hubOptions |}

                | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                    {| Transient = FableHub.Stream.Both.Both.addTransient onConnect onDisconnect settings.Update streamFrom (Task.toGen streamTo)
                       HubOptions = hubOptions |}
                | _ ->
                    {| Transient = 
                        { Updater = (fun msg hub -> settings.Update msg hub)
                          StreamFrom = streamFrom
                          StreamTo = (Task.toGen streamTo) }
                        |> FableHub.Stream.Both.addTransient
                       HubOptions = hubOptions |}

            match config.HubOptions with
            | Some hubOptions ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .AddHubOptions<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(
                        System.Action<HubOptions<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>>(hubOptions))
                    .Services |> config.Transient
            | _ ->
                this
                    .AddSignalR()
                    .AddNewtonsoftJsonProtocol(fun o -> o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
                    .Services |> config.Transient

        member this.AddSignalR(endpoint: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            SignalR.ConfigBuilder(endpoint, Task.toGen update).Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update).Build(), streamFrom)

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update).Build(), Task.toGen streamTo)

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update).Build(), streamFrom, Task.toGen streamTo)

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update) 
            |> config 
            |> fun res -> res.Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom)

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), Task.toGen streamTo)

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom, Task.toGen streamTo)

    type IApplicationBuilder with
        member this.UseSignalR (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
        
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
                        endpoints.MapHub<NormalFableHub<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)

        member this.UseSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> 
                IAsyncEnumerable<'ServerStreamApi>) =
            
            let config = 
                match settings.Config with
                | Some { OnConnected = Some _; OnDisconnected = None } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.From.OnConnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = None; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.From.OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = Some _; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.From.Both<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | _ ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)

        member this.UseSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            let config = 
                match settings.Config with
                | Some { OnConnected = Some _; OnDisconnected = None } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.To.OnConnected<'ClientApi,'ClientStreamApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = None; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.To.OnDisconnected<'ClientApi,'ClientStreamApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = Some _; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.To.Both<'ClientApi,'ClientStreamApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | _ ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)

        member this.UseSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> 
                IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            let config = 
                match settings.Config with
                | Some { OnConnected = Some _; OnDisconnected = None } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.Both.OnConnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = None; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.Both.OnDisconnected<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | Some { OnConnected = Some _; OnDisconnected = Some _ } ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<FableHub.Stream.Both.Both<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config
                | _ ->
                    fun (endpoints: IEndpointRouteBuilder) ->
                        endpoints.MapHub<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
