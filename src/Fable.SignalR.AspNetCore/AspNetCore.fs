namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.Remoting.Json
    open Fable.SignalR.Shared
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open Microsoft.AspNetCore.SignalR.Protocol
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.Logging
    open Newtonsoft.Json
    open System.Collections.Generic
    open System.Threading.Tasks
    
    [<RequireQualifiedAccess>]
    module internal Option =
        let mapThrough (config: SignalR.Config<_,_> option) (configToFun: SignalR.Config<_,_> -> ('T -> 'T) option) (item: 'T) =
            match config |> Option.bind configToFun with
            | Some f -> f item
            | None -> item

    [<RequireQualifiedAccess>]
    module internal Impl =
        let [<Literal>] Ns = "Microsoft.AspNetCore.SignalR"

        let config<'T, 'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi when 'T :> Hub> (builder: IServiceCollection) (hubOptions: (HubOptions -> unit) option) 
            (msgPack: bool) (builderFun: (ISignalRServerBuilder -> ISignalRServerBuilder) option) (transients: IServiceCollection -> IServiceCollection) =
            
            builder.AddSignalR()
            |> fun builder ->
                if msgPack then
                    builder.Services.AddSingleton<IHubProtocol,MsgPackProtocol.ServerFableHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>>()
                    |> ignore

                    builder
                else
                    builder
                        .AddNewtonsoftJsonProtocol(fun o -> 
                            o.PayloadSerializerSettings.DateParseHandling <- DateParseHandling.None
                            o.PayloadSerializerSettings.ContractResolver <- new Serialization.DefaultContractResolver()
                            o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
            |> fun builder ->
                match builderFun with
                | Some f -> f builder
                | None -> builder
            |> fun builder ->
                match hubOptions with
                | Some hubOptions ->
                    builder.AddHubOptions<'T>(System.Action<HubOptions<'T>>(hubOptions)).Services
                | None -> builder.Services
            |> transients

    type IHostBuilder with
        /// Adds a logging filter for SignalR with the given log level threshold.
        member this.SignalRLogLevel (logLevel: Microsoft.Extensions.Logging.LogLevel) =
            this.ConfigureLogging(fun l -> l.AddFilter(Impl.Ns, logLevel) |> ignore)
        
        /// Adds a logging filter for SignalR with the given log level threshold.
        member this.SignalRLogLevel (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            settings.Config
            |> Option.bind(fun o -> o.LogLevel)
            |> function
            | Some logLevel ->
                this.ConfigureLogging(fun l -> l.AddFilter(Impl.Ns, logLevel) |> ignore)
            | None -> this

    type IServiceCollection with
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let msgPk = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            let builderConfig = settings.Config |> Option.bind (fun s -> s.UseServerBuilder)

            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                fun s -> FableHub.OnConnected.AddServices(onConnect, settings.Send, settings.Invoke, s)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.OnDisconnected.AddServices(onDisconnect, settings.Send, settings.Invoke, s)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Both.AddServices(onConnect, onDisconnect, settings.Send, settings.Invoke, s)
            | _ -> fun s -> BaseFableHub.AddServices(settings.Send, settings.Invoke, s)
            |> Impl.config<BaseFableHub<'ClientApi,'ServerApi>,'ClientApi,unit,unit,'ServerApi,unit> this hubOptions msgPk builderConfig
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let msgPk = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            let builderConfig = settings.Config |> Option.bind (fun s -> s.UseServerBuilder)

            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                fun s -> FableHub.Stream.From.OnConnected.AddServices(onConnect, settings.Send, settings.Invoke, streamFrom, s)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.From.OnDisconnected.AddServices(onDisconnect, settings.Send, settings.Invoke, streamFrom, s)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.From.Both.AddServices(onConnect, onDisconnect, settings.Send, settings.Invoke, streamFrom, s)
            | _ -> fun s -> StreamFromFableHub.AddServices(settings.Send, settings.Invoke, streamFrom, s)
            |> Impl.config<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ClientStreamApi,unit,'ServerApi,'ServerStreamApi> 
                this hubOptions msgPk builderConfig
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let msgPk = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            let builderConfig = settings.Config |> Option.bind (fun s -> s.UseServerBuilder)

            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                fun s -> FableHub.Stream.To.OnConnected.AddServices(onConnect, settings.Send, settings.Invoke, Task.toGen streamTo, s)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.To.OnDisconnected.AddServices(onDisconnect, settings.Send, settings.Invoke, Task.toGen streamTo, s)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.To.Both.AddServices(onConnect, onDisconnect, settings.Send, settings.Invoke, Task.toGen streamTo, s)
            | _ -> fun s -> StreamToFableHub.AddServices(settings.Send, settings.Invoke, Task.toGen streamTo, s)
            |> Impl.config<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>,'ClientApi,unit,'ClientStreamApi,'ServerApi,unit> 
                this hubOptions msgPk builderConfig
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let msgPk = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            let builderConfig = settings.Config |> Option.bind (fun s -> s.UseServerBuilder)

            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                fun s -> FableHub.Stream.Both.OnConnected.AddServices(onConnect, settings.Send, settings.Invoke, streamFrom, Task.toGen streamTo, s)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.Both.OnDisconnected.AddServices(onDisconnect, settings.Send, settings.Invoke, streamFrom, Task.toGen streamTo, s)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                fun s -> FableHub.Stream.Both.Both.AddServices(onConnect, onDisconnect, settings.Send, settings.Invoke, streamFrom, Task.toGen streamTo, s)
            | _ -> fun s -> StreamBothFableHub.AddServices(settings.Send, settings.Invoke, streamFrom, Task.toGen streamTo, s)
            |> Impl.config<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>,'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> 
                this hubOptions msgPk builderConfig
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR(endpoint: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task, invoke: 'ClientApi -> FableHub -> Task<'ServerApi>) =
            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), streamFrom)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), streamFrom, Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> res.Build()
            |> this.AddSignalR
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> FableHub -> Task<'ServerApi>,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom, Task.toGen streamTo)

    type IApplicationBuilder with
        member private this.ApplyConfig (config: SignalR.Config<_,_> option, configToFun: SignalR.Config<_,_> -> (IApplicationBuilder -> IApplicationBuilder) option) =
            Option.mapThrough config configToFun this

        member private this.ApplyConfigs (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            if settings.Config.IsSome then
                this
                    .ApplyConfig(settings.Config, fun c -> c.BeforeUseRouting)
                    .ApplyConfig(settings.Config, fun c -> 
                        if c.NoRouting then None 
                        else Some (fun app -> app.UseRouting())
                    )
                    .ApplyConfig(settings.Config, fun c -> 
                        if c.EnableBearerAuth then 
                            Some (fun app -> app.UseMiddleware<WebSocketsMiddleware>(settings.EndpointPattern)) 
                        else None
                    )
                    .ApplyConfig(settings.Config, fun c -> c.AfterUseRouting)
            else this.UseRouting()

        /// Configures routing and endpoints for the SignalR hub.
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
                        endpoints.MapHub<BaseFableHub<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .ApplyConfigs(settings)
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
        
        /// Configures routing and endpoints for the SignalR hub.
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
                .ApplyConfigs(settings)
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
        
        /// Configures routing and endpoints for the SignalR hub.
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
                .ApplyConfigs(settings)
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
        
        /// Configures routing and endpoints for the SignalR hub.
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
                .ApplyConfigs(settings)
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
