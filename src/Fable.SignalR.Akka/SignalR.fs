namespace Fable.SignalR.Akka

open Akka.Cluster
open Akka.Configuration
open Akkling
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Runtime.InteropServices

 // TODO: Add logging actor and log errors for when things like group name and connection ids are null

type AkkaHubLifetimeManager<'Hub when 'Hub :> Hub> (logger: ILogger<DefaultHubLifetimeManager<'Hub>>, config: Config) =
    inherit HubLifetimeManager<'Hub>()

    let system = 
        config
        |> Config.setLogLevel logger.IsEnabled
        |> System.create (typeof<'Hub>.FullName.GetHashCode() |> sprintf "AkkaHub-%i")
    
    let manager = Actors.manager system

    //let akkaHub =
    //    spawn system (typeof<'Hub>.FullName.GetHashCode() |> sprintf "AkkaHub-%i") <| fun mailbox ->
    //        let cluster = Cluster.Get (mailbox.Context.System)

    //        cluster.Subscribe (mailbox.Self, [| typeof<ClusterEvent.IMemberEvent> |])
    //        mailbox.Defer <| fun () -> cluster.Unsubscribe mailbox.Self
    //        logger.LogInformation(sprintf "Spawned Akka cluster on node [%A] with roles [%s]" cluster.SelfAddress (cluster.SelfRoles |> String.concat ","))

    //        let rec seed () =
    //            actor {
    //                let! (msg: obj) = mailbox.Receive()

    //                match msg with
    //                | :? ClusterEvent.IMemberEvent -> logger.LogInformation(sprintf "Cluster event %A" msg)
    //                | _ -> logger.LogInformation(sprintf "Recieved: %A" msg)

    //                return! seed ()
    //            }
    //        seed ()

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
    open FSharp.Data.LiteralProviders

    module private AkkaConfig =
       let [<Literal>] Hub = TextFile.``akka.conf``.Text
       let [<Literal>] TestHub = TextFile.``testConfig.conf``.Text

    type ISignalRServerBuilder with
        member this.AddAkkaClustering (?akkaConfig: Config) =
            let config = 
                let baseConfig = Configuration.parse AkkaConfig.TestHub

                akkaConfig
                |> Option.map (fun config -> config.SafeWithFallback baseConfig)
                |> Option.defaultValue baseConfig

            this.Services
                .AddSingleton<Config>(config)
                .AddSingleton(typedefof<HubLifetimeManager<_>>,typedefof<AkkaHubLifetimeManager<_>>)
            |> ignore

            this
