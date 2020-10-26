namespace Fable.SignalR.Akka

open Akka.Actor
open Akkling
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.SignalR
open System
open System.Collections.Generic

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
        | SendAll of methodName:string * args: obj []
        | SendAllExcept of methodName:string * args: obj [] * connectionIds: IReadOnlyList<string>
        | SendConnection of connectionId:string * methodName:string * args: obj []
        | SendConnections of connectionIds: IReadOnlyList<string> * methodName:string * args: obj []
        | SendGroup of groupName:string * methodName:string * args: obj []
        | SendGroups of groupNames: IReadOnlyList<string> * methodName:string * args: obj []
        | SendGroupExcept of groupName:string * methodName:string * args: obj [] * excludedUserIds: IReadOnlyList<string>
        | SendUser of userId:string * methodName:string * args: obj []
        | SendUsers of userIds: IReadOnlyList<string> * methodName:string * args: obj []
        | AddToGroup of connectionId:string * groupName:string
        | RemoveFromGroup of connectionId:string * groupName:string

[<RequireQualifiedAccess>]
module private Name =
    let [<Literal>] Connections = "Connections"
    let [<Literal>] Groups = "Groups"
    let [<Literal>] Manager = "Manager"
    let [<Literal>] Users = "Users"
    let [<Literal>] User = "user"

module private AddressBook =
    open Fable.SignalR.Shared.MemoryCache

    module private Internal =
        let lookupF (system: IActorRefFactory) (identity: string) (addr: string) =
            async {
                let selection = select system addr

                match! selection <? Identify(identity) with
                | ActorIdentity (returnedIdentity, Some ref) when returnedIdentity = identity -> return Some ref
                | _ -> return None
            }

    open Internal

    [<NoComparison;NoEquality>]
    type AddressBook (system: IActorRefFactory, pathResolver: string -> string) =
        let addressBook = MemoryCache<string,IActorRef>(CacheExpirationPolicy.SlidingExpiration(TimeSpan(2, 0, 0)))

        let getAddr = memoize pathResolver

        let lookup identity =
            async {
                let! lookup = lookupF system identity (getAddr identity)

                return
                    match lookup with
                    | Some ref -> Ok (untyped ref) 
                    | None -> Error "Address not found."
            }
            
        member _.tryLookup<'Message> (identity: string) : Async<IActorRef<'Message> option> =            
            addressBook.TryGetOrAddAsync identity (lookup identity)
            |> Async.map (function | Ok ref -> Some (typed ref) | Error _ -> None)

        member this.tell (msg: 'Message) (identity: string) =
            async {
                let! res = this.tryLookup<'Message> identity
                
                return
                    match res with
                    | Some ref -> ref <! msg
                    | None -> ()
            }
            |> Async.Start

        member this.tryAsk (msg: 'Message) (identity: string): Async<'Response option> =
            async {
                let! res = this.tryLookup<'Message> identity
                
                match res with
                | Some ref -> return! ref <? msg |> Async.map Some
                | None -> return None
            }

open AddressBook

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
        member this.Addr = Addr.Root(this.Self.Path.Address.ToString() @@ Name.User)

module Actors =
    open Microsoft.AspNetCore.SignalR.Protocol

    [<RequireQualifiedAccess>]
    type ConnectionState =
        | Connected of ctx:HubConnectionContext * groups:Set<string>
        | Disconnected

    let private group name system =
        props <| fun (mailbox: Actor<Msg.Group>) ->
            let addressbook = AddressBook(system, mailbox.Addr.Manager.Connections.connection)
            
            let sendToConnection methodName args connectionId =
                addressbook.tell (Msg.Connection.SendToConnection(methodName, args)) connectionId

            let rec loop (members: Set<string>) = actor {
                let! msg = mailbox.Receive()

                let members =
                    match msg with
                    | Msg.Group.AddConnection user ->
                        members.Add user |> Some
                    | Msg.Group.RemoveConnection user ->
                        members.Remove user |> Some
                    | Msg.Group.GetMembers ->
                        mailbox.UntypedContext.Sender.Tell members
                        None
                    | Msg.Group.SendToMembers(methodName, args) ->
                        Seq.iter (sendToConnection methodName args) members

                        None
                    | Msg.Group.SendToMembersExcept(excludedMembers, methodName, args) ->
                        Set.difference members excludedMembers
                        |> Seq.iter (sendToConnection methodName args)

                        None
                    |> Option.defaultValue members

                return! loop members
            }

            loop Set.empty
        |> spawn system name

    let private groups system =
        props <| fun (mailbox: Actor<Msg.Groups>) ->
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
                    |> Child.tryAskThenTell mailbox groupName
                return! loop ()
            }

            loop ()
        |> spawn system Name.Groups

    let private connection name system =
        props <| fun (mailbox: Actor<Msg.Connection>) ->
            let addressbook = AddressBook(system, mailbox.Addr.Manager.Groups.group)
            let pathName = mailbox.UntypedContext.Self.Path.Name

            let leaveGroup =
                Msg.Group.RemoveConnection pathName
                |> addressbook.tell

            let joinGroup =
                Msg.Group.AddConnection pathName
                |> addressbook.tell

            let rec loop (state: ConnectionState) = actor {
                let! msg = mailbox.Receive()
                
                let state =
                    match state, msg with
                    | ConnectionState.Connected (_,groups), Msg.Connection.Disconnected ->
                        Set.iter leaveGroup groups

                        Some ConnectionState.Disconnected
                    | ConnectionState.Connected (ctx,groups), Msg.Connection.JoinedGroup group ->
                        joinGroup group
                        
                        ConnectionState.Connected(ctx,groups.Add group) |> Some
                    | ConnectionState.Connected (ctx,groups), Msg.Connection.LeftGroup group ->
                        leaveGroup group

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
        |> spawn system name

    let private connections system =
        props <| fun (mailbox: Actor<Msg.Connections>) ->
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
                    mailbox.UntypedContext.GetChildren()
                    |> Seq.iter (fun child -> child.Tell msg)
                | Msg.Connections.SendConnectionsExcept(excludedConnectionIds, msg) ->
                    mailbox.UntypedContext.GetChildren() 
                    |> Seq.iter (fun child -> 
                        if excludedConnectionIds.Contains(child.Path.Name) |> not then
                            child.Tell msg)
                | Msg.Connections.ConnectionMsg(connectionId, msg) -> 
                    Child.iterMsg mailbox connectionId msg

                return! loop ()
            }

            loop ()
        |> spawn system Name.Connections

    let private user name system = 
        props <| fun (mailbox: Actor<Msg.User>) ->
            let addressbook = AddressBook(system, mailbox.Addr.Manager.Connections.connection)

            let rec loop (connections: Set<string>) = actor {
                let! msg = mailbox.Receive()

                let connections =
                    match msg with
                    | Msg.User.AddConnection connectionId ->
                        connections.Add connectionId |> Some
                    | Msg.User.RemoveConnection connectionId ->
                        connections.Remove connectionId |> Some
                    | Msg.User.SendToConnections msg ->
                        Seq.iter (addressbook.tell msg) connections

                        None
                    | Msg.User.GetConnections ->
                        mailbox.UntypedContext.Sender.Tell connections

                        None
                    |> Option.defaultValue connections

                return! loop connections
            }

            loop Set.empty
        |> spawn system name

    let private users system =
        props <| fun (mailbox: Actor<Msg.Users>) ->
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
                    mailbox.UntypedContext.Child(user).Tell msg
                | Msg.Users.SendToUsers(users, msg) ->
                    mailbox.UntypedContext.GetChildren()
                    |> Seq.iter (fun child -> 
                        if users.Contains(child.Path.Name) then
                            child.Tell msg)
                | Msg.Users.GetConnections userName ->
                    Msg.User.GetConnections
                    |> Child.tryAskThenTell mailbox userName
                return! loop ()
            }

            loop ()
        |> spawn system Name.Users

    let manager system =
        props <| fun (mailbox: Actor<Msg.Manager>) ->
            Child.create mailbox Name.Groups groups
            Child.create mailbox Name.Connections connections
            Child.create mailbox Name.Users users

            let connectionAddressbook = AddressBook(system, mailbox.Addr.Manager.Connections.connection)
            let userAddressbook = AddressBook(system, mailbox.Addr.Manager.Users.user)

            let rec loop () = actor {
                let! msg = mailbox.Receive()

                mailbox.Log.Force().Debug(sprintf "Manager got msg: %A" msg)
                Logging.logDebugf mailbox "Manager got msg: %A" msg

                match msg with
                | Msg.Manager.OnConnected ctx ->
                    Msg.Connections.AddConnection ctx
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.OnDisconnected ctx ->
                    Msg.Connections.RemoveConnection ctx.ConnectionId
                    |> Child.iterMsg mailbox Name.Connections

                    match ctx.UserIdentifier with
                    | null -> ()
                    | userName -> 
                        Msg.Users.RemoveFromUser(userName, ctx.ConnectionId)
                        |> Child.iterMsg mailbox Name.Users

                | Msg.Manager.SendAll(methodName, args) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> Msg.Connections.SendAll
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.SendAllExcept(methodName, args, excludedConnectionIds) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> fun msg -> Msg.Connections.SendConnectionsExcept(Set.ofSeq excludedConnectionIds, msg)
                    |> Child.iterMsg mailbox Name.Connections

                | Msg.Manager.SendConnection(connectionId, methodName, args) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> flip connectionAddressbook.tell connectionId

                | Msg.Manager.SendConnections(connectionIds, methodName, args) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> connectionAddressbook.tell
                    |> flip Seq.iter connectionIds

                | Msg.Manager.SendGroup(groupName, methodName, args) ->
                    Msg.Group.SendToMembers(methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroup(groupName, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendGroups(groupNames, methodName, args) ->
                    Msg.Group.SendToMembers(methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroups(Set.ofSeq groupNames, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendGroupExcept(groupName, methodName, args, excludedConnectionIds) ->
                    Msg.Group.SendToMembersExcept(Set.ofSeq excludedConnectionIds, methodName, args)
                    |> fun msg -> Msg.Groups.SendToGroup(groupName, msg)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.SendUser(userName, methodName, args) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> Msg.User.SendToConnections
                    |> flip userAddressbook.tell userName

                | Msg.Manager.SendUsers(userNames, methodName, args) ->
                    Msg.Connection.SendToConnection(methodName, args)
                    |> Msg.User.SendToConnections
                    |> userAddressbook.tell
                    |> flip Seq.iter userNames
                    
                | Msg.Manager.AddToGroup(connectionId, groupName) ->
                    Msg.Groups.AddToGroup(groupName, connectionId)
                    |> Child.iterMsg mailbox Name.Groups

                | Msg.Manager.RemoveFromGroup(connectionId, groupName) ->
                    Msg.Groups.RemoveFromGroup(groupName, connectionId)
                    |> Child.iterMsg mailbox Name.Groups

                return! loop ()
            }

            loop ()
        |> spawn system Name.Manager
