namespace Fable.SignalR.Akka

open Akka
open Akka.Actor
open Akka.Cluster
open Akka.Configuration
open Akka.FSharp
open Fable.SignalR
open FSharp.Control.Tasks.ContextInsensitive
open FSharp.Data.LiteralProviders
open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.SignalR.Protocol
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
 
 module private AkkaConfig =
    let [<Literal>] Hub = TextFile.``akka.conf``.Text

 // TODO: Add logging actor and log errors for when things like group name and connection ids are null

type AkkaHubLifetimeManager<'Hub when 'Hub :> Hub> (logger: ILogger<DefaultHubLifetimeManager<'Hub>>) =
    inherit HubLifetimeManager<'Hub>()

    //let config = ConfigurationFactory.ParseString("")
    let system = System.create (typeof<'Hub>.FullName.GetHashCode() |> sprintf "AkkaHub-%i") <| Configuration.parse(AkkaConfig.Hub)
    
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
        manager <! Msg.Manager.SendAll(methodName, args, cancellationToken)
        |> Task.liftGen

    override _.SendAllExceptAsync (methodName, args, excludedConnectionIds, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendAllExcept(methodName, args, excludedConnectionIds, cancellationToken)
        |> Task.liftGen

    override _.SendConnectionAsync (connectionId, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendConnection(connectionId, methodName, args, cancellationToken)
        |> Task.liftGen

    override _.SendConnectionsAsync (connectionIds, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendConnections(connectionIds, methodName, args, cancellationToken)
        |> Task.liftGen

    override _.SendGroupAsync (groupName, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendGroup(groupName, methodName, args, cancellationToken)
        |> Task.liftGen

    override _.SendGroupsAsync (groupNames, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendGroups(groupNames, methodName, args, cancellationToken)
        |> Task.liftGen

    override _.SendGroupExceptAsync (groupName, methodName, args, excludedConnectionIds, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendGroupExcept(groupName, methodName, args, excludedConnectionIds, cancellationToken)
        |> Task.liftGen

    override _.SendUserAsync (userId, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendUser(userId, methodName, args, cancellationToken) 
        |> Task.liftGen
        
    override _.SendUsersAsync (userIds, methodName, args, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.SendUsers(userIds, methodName, args, cancellationToken)
        |> Task.liftGen

    override _.AddToGroupAsync (connectionId, groupName, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.AddToGroup(connectionId, groupName, cancellationToken)
        |> Task.liftGen

    override _.RemoveFromGroupAsync (connectionId, groupName, [<Optional>] cancellationToken) =
        manager <! Msg.Manager.RemoveFromGroup(connectionId, groupName, cancellationToken)
        |> Task.liftGen

    interface IDisposable with
        member _.Dispose () =
            system.Terminate().Wait()
            system.WhenTerminated.Wait()

[<AutoOpen>]
module SignalRServerBuilderExtensions =
    type ISignalRServerBuilder with
        member this.AddAkkaClustering () =
            this.Services.AddSingleton(typedefof<HubLifetimeManager<_>>,typedefof<AkkaHubLifetimeManager<_>>)
            |> ignore

            this
