namespace Fable.SignalR

[<AutoOpen>]
module SignalRExtension =
    open Fable.Remoting.Json
    open Fable.SignalR
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.SignalR
    open Microsoft.AspNetCore.SignalR.Protocol
    open Microsoft.AspNetCore.Routing
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.DependencyInjection.Extensions
    open Microsoft.Extensions.Hosting
    open Microsoft.Extensions.Logging
    open Newtonsoft.Json
    open System
    open System.Collections.Generic
    open System.Threading.Tasks
    
    [<RequireQualifiedAccess>]
    module internal Impl =
        let config<'T when 'T :> Hub> (builder: IServiceCollection) (hubOptions: (HubOptions -> unit) option) (msgPack: bool) (transients: IServiceCollection -> IServiceCollection) =
            builder
                .AddSignalR()
            |> fun builder ->
                if msgPack then
                    builder.Services
                        .TryAddEnumerable(ServiceDescriptor.Singleton<IHubProtocol>(MsgPackProtocol.FableHubProtocol()))
                    builder
                else
                    builder
                        .AddNewtonsoftJsonProtocol(fun o -> 
                            o.PayloadSerializerSettings.DateParseHandling <- DateParseHandling.None
                            o.PayloadSerializerSettings.ContractResolver <- new Serialization.DefaultContractResolver()
                            o.PayloadSerializerSettings.Converters.Add(FableJsonConverter()))
            |> fun builder ->
                match hubOptions with
                | Some hubOptions ->
                    builder.AddHubOptions<'T>(System.Action<HubOptions<'T>>(hubOptions)).Services
                | None -> builder.Services
            |> transients

    type IHostBuilder with
        /// Adds a logging filter for SignalR with the given log level threshold.
        member this.SignalRLogLevel (logLevel: Microsoft.Extensions.Logging.LogLevel) =
            this.ConfigureLogging(fun l -> l.AddFilter("Microsoft.AspNetCore.SignalR", logLevel) |> ignore)
        
        /// Adds a logging filter for SignalR with the given log level threshold.
        member this.SignalRLogLevel (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            settings.Config
            |> Option.bind(fun o -> o.LogLevel)
            |> function
            | Some logLevel ->
                this.ConfigureLogging(fun l -> l.AddFilter("Microsoft.AspNetCore.SignalR", logLevel) |> ignore)
            | None -> this

    type IServiceCollection with
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR (settings: SignalR.Settings<'ClientApi,'ServerApi>) =
            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let useMessagePack = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))

            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                FableHub.OnConnected.addTransient onConnect settings.Send settings.Invoke
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                FableHub.OnDisconnected.addTransient onDisconnect settings.Send settings.Invoke
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                FableHub.Both.addTransient onConnect onDisconnect settings.Send settings.Invoke
            | _ -> FableHub.addUpdateTransient settings.Send settings.Invoke
            |> Impl.config<NormalFableHub<'ClientApi,'ServerApi>> this hubOptions useMessagePack
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let useMessagePack = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            
            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                FableHub.Stream.From.OnConnected.addTransient onConnect settings.Send settings.Invoke streamFrom
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.From.OnDisconnected.addTransient onDisconnect settings.Send settings.Invoke streamFrom
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.From.Both.addTransient onConnect onDisconnect settings.Send settings.Invoke streamFrom
            | _ -> FableHub.Stream.From.addTransient settings.Send settings.Invoke streamFrom
            |> Impl.config<StreamFromFableHub<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>> this hubOptions useMessagePack
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let useMessagePack = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            
            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                FableHub.Stream.To.OnConnected.addTransient onConnect settings.Send settings.Invoke (Task.toGen streamTo)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.To.OnDisconnected.addTransient onDisconnect settings.Send settings.Invoke (Task.toGen streamTo)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.To.Both.addTransient onConnect onDisconnect settings.Send settings.Invoke (Task.toGen streamTo)
            | _ -> FableHub.Stream.To.addTransient settings.Send settings.Invoke (Task.toGen streamTo)
            |> Impl.config<StreamToFableHub<'ClientApi,'ClientStreamApi,'ServerApi>> this hubOptions useMessagePack
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (settings: SignalR.Settings<'ClientApi,'ServerApi>, 
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =

            let hubOptions = settings.Config |> Option.bind (fun s -> s.HubOptions)
            let useMessagePack = Option.defaultValue false (settings.Config |> Option.map (fun c -> c.UseMessagePack))
            
            match settings.Config with
            | Some { OnConnected = Some onConnect; OnDisconnected = None } ->
                FableHub.Stream.Both.OnConnected.addTransient onConnect settings.Send settings.Invoke streamFrom (Task.toGen streamTo)
            | Some { OnConnected = None; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.Both.OnDisconnected.addTransient onDisconnect settings.Send settings.Invoke streamFrom (Task.toGen streamTo)
            | Some { OnConnected = Some onConnect; OnDisconnected = Some onDisconnect } ->
                FableHub.Stream.Both.Both.addTransient onConnect onDisconnect settings.Send settings.Invoke streamFrom (Task.toGen streamTo)
            | _ -> FableHub.Stream.Both.addTransient settings.Send settings.Invoke streamFrom (Task.toGen streamTo)
            |> Impl.config<StreamBothFableHub<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>> this hubOptions useMessagePack
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR(endpoint: string, update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task, invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi) =
            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build()
            |> this.AddSignalR

        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), streamFrom)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task) =
            
            this.AddSignalR(SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke).Build(), streamFrom, Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> res.Build()
            |> this.AddSignalR
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamFrom: 'ClientStreamApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamTo: IAsyncEnumerable<'ClientStreamApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), Task.toGen streamTo)
        
        /// Adds SignalR services to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        member this.AddSignalR
            (endpoint: string,
             update: 'ClientApi -> FableHub<'ClientApi,'ServerApi> -> #Task,
             invoke: 'ClientApi -> System.IServiceProvider -> 'ServerApi,
             streamFrom: 'ClientStreamFromApi -> FableHub<'ClientApi,'ServerApi> -> IAsyncEnumerable<'ServerStreamApi>,
             streamTo: IAsyncEnumerable<'ClientStreamToApi> -> FableHub<'ClientApi,'ServerApi> -> #Task,
             config: SignalR.ConfigBuilder<'ClientApi,'ServerApi> -> SignalR.ConfigBuilder<'ClientApi,'ServerApi>) =

            SignalR.ConfigBuilder(endpoint, Task.toGen update, invoke) 
            |> config 
            |> fun res -> this.AddSignalR(res.Build(), streamFrom, Task.toGen streamTo)

    type IApplicationBuilder with
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
                        endpoints.MapHub<NormalFableHub<'ClientApi,'ServerApi>>(settings.EndpointPattern)
                        |> SignalR.Config.bindEnpointConfig settings.Config

            this
                .UseRouting()
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
                .UseRouting()
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
                .UseRouting()
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
                .UseRouting()
                // fsharplint:disable-next-line
                .UseEndpoints(fun endpoints -> endpoints |> config |> ignore)
