namespace Fable.SignalR.Akka

open Akka.Actor
open Akka.FSharp
open Akka.FSharp.Actors
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.AspNetCore.SignalR
open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module internal Helpers =
    let private tryCast (t:Task<obj>) : 'Message =
        match t.Result with
        | :? 'Message as m -> m
        | o ->
            let context = Akka.Actor.Internal.InternalCurrentActorCellKeeper.Current
            if isNull context
            then failwith "Cannot cast object outside the actor system context "
            else
                match o with
                | :? (byte[]) as bytes -> 
                    let serializer = context.System.Serialization.FindSerializerForType typeof<'Message>
                    serializer.FromBinary(bytes, typeof<'Message>) :?> 'Message
                | _ -> raise (InvalidCastException("Tried to cast object to " + typeof<'Message>.ToString()))

    let inline (<!!) (actorRef : #ICanTell) (msg : obj) : Task = 
        task { return actorRef.Tell(msg, ActorCell.GetCurrentSelfOrNoSender()) } :> Task

    let (<??) (tell : #ICanTell) (msg : obj) = 
        tell.Ask(msg).ContinueWith(tryCast, TaskContinuationOptions.ExecuteSynchronously)

    let inline (|!!>) (computation : Task) (recipient : ICanTell) = pipeTo (computation |> Async.AwaitTask) recipient ActorRefs.NoSender

    let inline (<!!|) (recipient : ICanTell) (computation : Task) = pipeTo (computation |> Async.AwaitTask) recipient ActorRefs.NoSender

    let (@@) (path1: string) (path2: string) = path1 + "/" + path2

    module Child =
        let tryGet (mailbox: Actor<_>) (name: string) =
            let child = mailbox.Context.Child(name)

            if child.IsNobody() then None
            else Some child

        let getOrCreate (mailbox: Actor<_>) (name: string) (f: IActorRefFactory -> IActorRef) =
            tryGet mailbox name
            |> function
            | Some child -> child
            | None -> f mailbox.Context

        let create (mailbox: Actor<_>) (name: string) (f: IActorRefFactory -> IActorRef) =
            getOrCreate mailbox name f
            |> ignore

        let iter (mailbox: Actor<_>) (name: string) (f: IActorRef -> _) =
            tryGet mailbox name
            |> Option.iter (f >> ignore)

        let iterMsg (mailbox: Actor<_>) (name: string) (msg: 'Msg) =
            iter mailbox name (fun child -> child <! msg)

        let tryAsk<'Response,'Msg,'ParentMsg> (mailbox: Actor<'ParentMsg>) (name: string) (msg: 'Msg) =
            tryGet mailbox name
            |> Option.map (fun child -> child.Ask<'Response> msg)
            |> function
            | Some rsp -> 
                task { 
                    let! res = rsp 
                    return Some res
                }
            | None -> task { return None }

    module Set =
        let asIReadOnlyList (set: Set<'T>) = (ResizeArray set) :> IReadOnlyList<'T>

    module Task =
        let liftGen x = task { return x } :> Task

    module ActorSelection =
        let tell msg (actorSel: ActorSelection) = actorSel.Tell msg

[<RequireQualifiedAccess>]
module Msg =
    type Group =
        | AddConnection of string
        | RemoveConnection of string
        | SendToMembers of methodName:string * args: obj []
        | SendToMembersExcept of excludedMembers:Set<string> * methodName:string * args: obj []
        | GetMembers

    type Groups =
        | AddToGroup of groupName:string * connectionId:string
        | RemoveFromGroup of groupName:string * connectionId:string
        | SendToGroup of groupName:string * msg:Group
        | SendToGroups of groupNames:Set<string> * msg:Group
        | GetMembers of groupName:string

    type Connection =
        | Connected of HubConnectionContext
        | Disconnected
        | JoinedGroup of group:string
        | LeftGroup of group:string
        | SendToConnection of methodName:string * args: obj []

    type Connections =
        | AddConnection of HubConnectionContext
        | RemoveConnection of string
        | SendConnections of connectionIds:Set<string> * msg:Connection
        | SendConnectionsExcept of connectionIds:Set<string> * msg:Connection
        | SendAll of msg:Connection
        | ConnectionMsg of connection:string * msg:Connection

    type User =
        | AddConnection of string
        | RemoveConnection of string
        | SendToConnections of msg:Connection
        | GetConnections

    type Users =
        | AddToUser of userName:string * connectionId:string
        | RemoveFromUser of userName:string * connectionId:string
        | SendToUser of userName:string * msg:User
        | SendToUsers of userNames:Set<string> * msg:User
        | GetConnections of userName:string

    type Manager =
        | OnConnected of ctx: HubConnectionContext
        | OnDisconnected of ctx: HubConnectionContext
        | SendAll of methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendAllExcept of methodName:string * args: obj [] * connectionIds: IReadOnlyList<string> * cancellationToken:CancellationToken
        | SendConnection of connectionId:string * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendConnections of connectionIds: IReadOnlyList<string> * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendGroup of groupName:string * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendGroups of groupNames: IReadOnlyList<string> * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendGroupExcept of groupName:string * methodName:string * args: obj [] * excludedUserIds: IReadOnlyList<string> * cancellationToken:CancellationToken
        | SendUser of userId:string * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | SendUsers of userIds: IReadOnlyList<string> * methodName:string * args: obj [] * cancellationToken:CancellationToken
        | AddToGroup of connectionId:string * groupName:string * cancellationToken:CancellationToken
        | RemoveFromGroup of connectionId:string * groupName:string * cancellationToken:CancellationToken

[<RequireQualifiedAccess>]
module private Name =
    let [<Literal>] Connections = "Connections"
    let [<Literal>] Groups = "Groups"
    let [<Literal>] Manager = "Manager"
    let [<Literal>] Users = "Users"

[<RequireQualifiedAccess>]
module private Addr =
    type Connections (path: string) =
        member _.connection (connectionId: string) = path @@ connectionId
        member _.up = path

    type Groups (path: string) =
        member _.group (groupName: string) = path @@ groupName
        member _.up = path

    type Users (path: string) =
        member _.user (userId: string) = path @@ userId
        member _.up = path

    type Manager (path: string) =
        member _.connections = path @@ Name.Connections
        member _.groups = path @@ Name.Groups
        member _.users = path @@ Name.Users
        member _.up = path

        member this.Connections = Connections(this.connections)
        member this.Groups = Groups(this.groups)
        member this.Users = Users(this.users)

    type Root (root: string) =
        member _.manager = root @@ Name.Manager

        member this.Manager = Manager(this.manager)

[<AutoOpen>]
module private ActorExtensions =
    type Actor<'Message> with
        member this.Addr = Addr.Root(this.Self.Path.Address.ToString() @@ "user")

//[<RequireQualifiedAccess>]
//module private Providers =
//    type Groups =
//        static member GetMembers (mailbox: Actor<_>) (groupName: string) : Task<Set<string> option> =
//            Msg.Groups.GetMembers groupName
//            |> mailbox.Context.ActorSelection(Addr.root @@ Addr.Root.Manager.groups).Ask<Set<string> option>

//        static member GetMembersAsync (mailbox: Actor<_>) (groupName: string) : Async<Set<string> option> =
//            Msg.Groups.GetMembers groupName
//            |> mailbox.Context.ActorSelection(Addr.root @@ Addr.Root.Manager.groups).Ask<Set<string> option>
//            |> Async.AwaitTask

module Actors =
    open Microsoft.AspNetCore.SignalR.Protocol

    [<RequireQualifiedAccess>]
    type ConnectionState =
        | Connected of ctx:HubConnectionContext * groups:Set<string>
        | Disconnected

    let private group name system =
        spawn system name <| fun (mailbox: Actor<Msg.Group>) ->
            let rec loop (members: Set<string>) = actor {
                let! msg = mailbox.Receive()

                let members =
                    match msg with
                    | Msg.Group.AddConnection user ->
                        members.Add user |> Some
                    | Msg.Group.RemoveConnection user ->
                        members.Remove user |> Some
                    | Msg.Group.GetMembers ->
                        mailbox.Context.Sender.Tell members
                        None
                    | Msg.Group.SendToMembers(methodName, args) ->
                        let msg = Msg.Connection.SendToConnection(methodName, args)

                        members
                        |> Seq.iter (
                            mailbox.Addr.Manager.Connections.connection
                            >> mailbox.Context.ActorSelection 
                            >> ActorSelection.tell msg
                        )

                        None
                    | Msg.Group.SendToMembersExcept(excludedMembers, methodName, args) ->
                        let msg = Msg.Connection.SendToConnection(methodName, args)
                        
                        Set.difference members excludedMembers
                        |> Seq.iter (
                            mailbox.Addr.Manager.Connections.connection
                            >> mailbox.Context.ActorSelection
                            >> ActorSelection.tell msg
                        )

                        None
                    |> Option.defaultValue members

                return! loop members
            }

            loop Set.empty

    let private groups system =
        spawn system Name.Groups <| fun (mailbox: Actor<Msg.Groups>) ->
            let rec loop () = actor {
                let! msg = mailbox.Receive()

                match msg with
                | Msg.Groups.AddToGroup(groupName,user) ->
                    group groupName
                    |> Child.getOrCreate mailbox groupName
                    <! Msg.Group.AddConnection user
                | Msg.Groups.RemoveFromGroup(groupName,user) ->
                    Msg.Group.RemoveConnection user
                    |> Child.iterMsg mailbox groupName
                | Msg.Groups.SendToGroup(groupName, msg) ->
                    Child.iterMsg mailbox groupName msg
                | Msg.Groups.SendToGroups(groups, msg) ->
                    groups
                    |> Seq.iter (fun groupName -> Child.iterMsg mailbox groupName msg)
                | Msg.Groups.GetMembers groupName ->
                    Msg.Group.GetMembers
                    |> Child.tryAsk mailbox groupName
                    |> mailbox.Sender().Tell
                return! loop ()
            }

            loop ()

    let private connection name system =
        let findGroupActor (mailbox: Actor<Msg.Connection>) (group: string) =
            mailbox.Addr.Manager.Groups.group group
            |> mailbox.Context.ActorSelection

        let leaveGroup (mailbox: Actor<Msg.Connection>) (group: string) =
            findGroupActor mailbox group
            <! Msg.Group.RemoveConnection mailbox.Context.Self.Path.Name
        
        let joinGroup (mailbox: Actor<Msg.Connection>) (group: string) =
            findGroupActor mailbox group
            <! Msg.Group.AddConnection mailbox.Context.Self.Path.Name

        spawn system name <| fun (mailbox: Actor<Msg.Connection>) ->
            let rec loop (state: ConnectionState) = actor {
                let! msg = mailbox.Receive()
                
                let state =
                    match state,msg with
                    | ConnectionState.Connected (_,groups), Msg.Connection.Disconnected ->
                        Set.iter (leaveGroup mailbox) groups

                        Some ConnectionState.Disconnected
                    | ConnectionState.Connected (ctx,groups), Msg.Connection.JoinedGroup group ->
                        joinGroup mailbox group
                        
                        ConnectionState.Connected(ctx,groups.Add group) |> Some
                    | ConnectionState.Connected (ctx,groups), Msg.Connection.LeftGroup group ->
                        leaveGroup mailbox group

                        ConnectionState.Connected(ctx,groups.Remove group) |> Some
                    | ConnectionState.Connected (ctx,_), Msg.Connection.SendToConnection(methodName, args) ->
                        InvocationMessage(methodName, args)
                        |> SerializedHubMessage
                        |> ctx.WriteAsync
                        |> ignore

                        None
                    | _, Msg.Connection.Connected ctx ->
                        ConnectionState.Connected(ctx,Set.empty) |> Some
                    | _ -> None
                    |> Option.defaultValue state

                return! loop state
            }

            loop <| ConnectionState.Disconnected

    let private connections system =
        spawn system Name.Connections <| fun (mailbox: Actor<Msg.Connections>) ->
            let rec loop () = actor {
                let! msg = mailbox.Receive()

                match msg with
                | Msg.Connections.AddConnection ctx ->
                    connection ctx.ConnectionId
                    |> Child.create mailbox ctx.ConnectionId

                    Msg.Connection.Connected ctx
                    |> Child.iterMsg mailbox ctx.ConnectionId
                | Msg.Connections.RemoveConnection connectionId ->
                    Child.iterMsg mailbox connectionId Msg.Connection.Disconnected
                | Msg.Connections.SendConnections(connectionIds, msg) ->
                    connectionIds |> Seq.iter (fun connectionId -> Child.iterMsg mailbox connectionId msg)
                | Msg.Connections.SendAll msg ->
                    mailbox.Context.GetChildren()
                    |> Seq.iter (fun child -> child.Tell msg)
                | Msg.Connections.SendConnectionsExcept(excludedConnectionIds, msg) ->
                    mailbox.Context.GetChildren() 
                    |> Seq.iter (fun child -> 
                        if excludedConnectionIds.Contains(child.Path.Name) |> not then
                            child.Tell msg)
                | Msg.Connections.ConnectionMsg(connectionId, msg) -> 
                    Child.iterMsg mailbox connectionId msg

                return! loop ()
            }

            loop ()

    let private user name system = 
        spawn system name <| fun (mailbox: Actor<Msg.User>) ->
            let rec loop (connections: Set<string>) = actor {
                let! msg = mailbox.Receive()

                let connections =
                    match msg with
                    | Msg.User.AddConnection connectionId ->
                        connections.Add connectionId |> Some
                    | Msg.User.RemoveConnection connectionId ->
                        connections.Remove connectionId |> Some
                    | Msg.User.SendToConnections msg ->
                        connections
                        |> Seq.iter (
                            mailbox.Addr.Manager.Connections.connection
                            >> mailbox.Context.ActorSelection
                            >> ActorSelection.tell msg
                        )

                        None
                    | Msg.User.GetConnections ->
                        mailbox.Context.Sender.Tell connections
                        None
                    |> Option.defaultValue connections

                return! loop connections
            }

            loop Set.empty

    let private users system =
        spawn system Name.Users <| fun (mailbox: Actor<Msg.Users>) ->
            let rec loop () = actor {
                let! msg = mailbox.Receive()

                match msg with
                | Msg.Users.AddToUser(userName,connectionId) ->
                    user userName
                    |> Child.getOrCreate mailbox userName
                    <! Msg.User.AddConnection connectionId
                | Msg.Users.RemoveFromUser(userName,connectionId) ->
                    Msg.User.RemoveConnection connectionId
                    |> Child.iterMsg mailbox userName
                | Msg.Users.SendToUser(user, msg) ->
                    mailbox.Context.Child(user).Tell msg
                | Msg.Users.SendToUsers(users, msg) ->
                    mailbox.Context.GetChildren()
                    |> Seq.iter (fun child -> 
                        if users.Contains(child.Path.Name) then
                            child.Tell msg)
                | Msg.Users.GetConnections userName ->
                    Msg.User.GetConnections
                    |> Child.tryAsk mailbox userName
                    |> mailbox.Sender().Tell
                return! loop ()
            }

            loop ()

    // figure out what to do with cancellation tokens
    let manager system =
        spawn system Name.Manager <| fun (mailbox: Actor<Msg.Manager>) ->
            Child.create mailbox Name.Groups groups
            Child.create mailbox Name.Connections connections
            Child.create mailbox Name.Users users

            let rec loop () = actor {
                let! msg = mailbox.Receive()

                match msg with
                | Msg.Manager.OnConnected ctx ->
                    Msg.Connections.AddConnection ctx
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.OnDisconnected ctx ->
                    Msg.Connections.RemoveConnection ctx.ConnectionId
                    |> Child.iterMsg mailbox Name.Connections

                    match ctx.UserIdentifier with // double check this is how they do this in signalr for users
                    | null -> ()
                    | userName -> 
                        Msg.Users.RemoveFromUser(userName, ctx.ConnectionId)
                        |> Child.iterMsg mailbox Name.Users

                | Msg.Manager.SendAll(methodName, args, ct) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> Msg.Connections.SendAll
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.SendAllExcept(methodName, args, excludedConnectionIds, ct) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> fun msg -> Msg.Connections.SendConnectionsExcept(Set.ofSeq excludedConnectionIds, msg)
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.SendConnection(connectionId, methodName, args, ct) ->
                    mailbox.Addr.Manager.Connections.connection connectionId
                    |> mailbox.Context.ActorSelection
                    |> ActorSelection.tell (Msg.Connection.SendToConnection(methodName, args))

                | Msg.Manager.SendConnections(connectionIds, methodName, args, ct) ->
                    connectionIds
                    |> Seq.iter (
                        mailbox.Addr.Manager.Connections.connection
                        >> mailbox.Context.ActorSelection
                        >> ActorSelection.tell (Msg.Connection.SendToConnection(methodName, args))
                    )

                | Msg.Manager.SendGroup(groupName, methodName, args, ct) ->
                    Msg.Group.SendToMembers(methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroup(groupName, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendGroups(groupNames, methodName, args, ct) ->
                    Msg.Group.SendToMembers(methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroups(Set.ofSeq groupNames, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendGroupExcept(groupName, methodName, args, excludedConnectionIds, ct) ->
                    Msg.Group.SendToMembersExcept(Set.ofSeq excludedConnectionIds, methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroup(groupName, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendUser(userName, methodName, args, ct) ->
                    let msg =
                        Msg.Connection.SendToConnection(methodName, args)
                        |> Msg.User.SendToConnections

                    mailbox.Addr.Manager.Users.user userName
                    |> mailbox.Context.ActorSelection
                    |> ActorSelection.tell msg

                | Msg.Manager.SendUsers(userNames, methodName, args, ct) ->
                    let msg =
                        Msg.Connection.SendToConnection(methodName, args)
                        |> Msg.User.SendToConnections

                    userNames
                    |> Seq.iter (
                        mailbox.Addr.Manager.Users.user
                        >> mailbox.Context.ActorSelection
                        >> ActorSelection.tell msg
                    )

                | Msg.Manager.AddToGroup(connectionId, groupName, _) ->
                    Msg.Groups.AddToGroup(groupName, connectionId)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.RemoveFromGroup(connectionId, groupName, _) ->
                    Msg.Groups.RemoveFromGroup(groupName, connectionId)
                    |> Child.iterMsg mailbox Name.Groups

                return! loop ()
            }

            loop ()
