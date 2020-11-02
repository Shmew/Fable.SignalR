namespace Fable.SignalR.Akka

open Akka.Cluster.Tools.PublishSubscribe
open Akka.Configuration
open Akkling
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Runtime.InteropServices

type AkkaHubConfig =
    { Hostname: string
      PublicHostname: string
      Port: uint16
      SeedNodes: (string * uint16) list }

[<RequireQualifiedAccess>]
module AkkaHubConfig =
    let seedNodes (akkaHubConfig: AkkaHubConfig) = akkaHubConfig.SeedNodes

    let setSeedNodes (seedNodes: (string * uint16) list) (akkaHubConfig: AkkaHubConfig) = 
        { akkaHubConfig with SeedNodes = seedNodes }

 // TODO: Add logging actor and log errors for when things like group name and connection ids are null
 open ConfigBuilder

type AkkaHubLifetimeManager<'Hub when 'Hub :> Hub> (logger: ILogger<DefaultHubLifetimeManager<'Hub>>, config: Config, akkaHubConfig: AkkaHubConfig) =
    inherit HubLifetimeManager<'Hub>()

    let systemName = typeof<'Hub>.FullName.GetHashCode() |> sprintf "AkkaHub-%i"

    let system =
        { Extensions = [ "Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider, Akka.Cluster.Tools" ]
          Loggers = [ "Akka.Event.DefaultLogger, Akka" ]
          Provider = "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
          DownUnreachable = 30<s>
          Roles = [ "signalr" ]
          SeedNodes =
            if List.distinct akkaHubConfig.SeedNodes <> akkaHubConfig.SeedNodes then
                sprintf "Duplicate seed-node names detected on system: %s!%s%A" 
                    systemName System.Environment.NewLine akkaHubConfig.SeedNodes
                |> InvalidOperationException
                |> raise
            
            akkaHubConfig.SeedNodes
            |> List.map (fun (node, port) -> sprintf "akka.tcp://%s@%s:%i" systemName node port)
          PublicHostname = akkaHubConfig.PublicHostname
          Hostname = akkaHubConfig.Hostname
          Port = akkaHubConfig.Port
          LogLevelPredicate = logger.IsEnabled }
        |> create
        |> fun res ->
            printfn "%s" res
            res
        |> Configuration.parse
        |> fun initConfig -> initConfig.SafeWithFallback(config)
        |> System.create systemName

    //let system = 
    //    Config.empty
    //    |> Config.logLevel logger.IsEnabled 
    //    |> Config.seedNodes systemName akkaHubConfig.SeedNodes
    //    |> Config.instanceInfo akkaHubConfig.Hostname akkaHubConfig.PublicHostname akkaHubConfig.Port
    //    |> Config.extensions [ "Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider, Akka.Cluster.Tools" ]
    //    |> Config.loggers [ "Akka.Event.DefaultLogger, Akka" ]
    //    |> Config.provider "Akka.Cluster.ClusterActorRefProvider, Akka.Cluster"
    //    |> Config.autoDownUnreachable 30
    //    |> Config.roles [ "signalr" ]
    //    |> Config.pubSubRoutingLogicBroadcast
    //    |> fun initConfig -> initConfig.SafeWithFallback(config)
    //    |> System.create systemName

    //let shard = Actors.Clustering.shard system
    let manager = Actors.distributor system

    //do printfn "%s" (system.Settings.ToString())

    override _.OnConnectedAsync ctx =
        manager <! Msg.Manager.OnConnected ctx
        |> Task.liftGen

    override _.OnDisconnectedAsync ctx =
           manager <! Msg.Manager.OnConnected ctx
           |> Task.liftGen

    override _.SendAllAsync (methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendAll(methodName, args)

    override _.SendAllExceptAsync (methodName, args, excludedConnectionIds, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendAllExcept(methodName, args, excludedConnectionIds)

    override _.SendConnectionAsync (connectionId, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendConnection(connectionId, methodName, args)

    override _.SendConnectionsAsync (connectionIds, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendConnections(connectionIds, methodName, args)

    override _.SendGroupAsync (groupName, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendGroup(groupName, methodName, args)

    override _.SendGroupsAsync (groupNames, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendGroups(groupNames, methodName, args)

    override _.SendGroupExceptAsync (groupName, methodName, args, excludedConnectionIds, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendGroupExcept(groupName, methodName, args, excludedConnectionIds)

    override _.SendUserAsync (userId, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendUser(userId, methodName, args) 
        
    override _.SendUsersAsync (userIds, methodName, args, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.SendUsers(userIds, methodName, args)

    override _.AddToGroupAsync (connectionId, groupName, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.AddToGroup(connectionId, groupName)

    override _.RemoveFromGroupAsync (connectionId, groupName, [<Optional>] cancellationToken) =
        CancellationToken.iterTask cancellationToken <| fun () ->
            manager <! Msg.Manager.RemoveFromGroup(connectionId, groupName)

    interface IDisposable with
        member _.Dispose () =
            system.Terminate().Wait()
            system.WhenTerminated.Wait()

[<AutoOpen>]
module SignalRServerBuilderExtensions =
    let internal baseConfig = DistributedPubSub.DefaultConfig()
    
    type ISignalRServerBuilder with
        member this.AddAkkaClustering (hostname: string, publicHostname: string, port: uint16, seedNodes: (string * uint16) list, ?config: Config) =
            let config = 
                config
                |> Option.map (fun config -> config.SafeWithFallback(baseConfig))
                |> Option.defaultValue baseConfig

            this.Services
                .AddSingleton<Config>(config)
                .AddSingleton<AkkaHubConfig>({ Hostname = hostname; PublicHostname = publicHostname; Port = port; SeedNodes = seedNodes })
                .AddSingleton(typedefof<HubLifetimeManager<_>>,typedefof<AkkaHubLifetimeManager<_>>)
            |> ignore

            this
