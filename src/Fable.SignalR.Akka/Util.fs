namespace Fable.SignalR.Akka

open FSharp.Control.Tasks.NonAffine
open FsToolkit.ErrorHandling
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<AutoOpen>]
module Helpers =
    let flip (f: 'a -> 'b -> 'c) = fun b a -> f a b

[<RequireQualifiedAccess>]
module Async =
    let lift x = async { return x }
    let liftIter f = async { return f() }

[<AutoOpen>]
module Extensions =
    type Async with
        static member Start (ao: Async<Option<unit>>, ?cancellationToken: CancellationToken) =
            Async.map (Option.iter id) ao
            |> fun a -> Async.Start(a, ?cancellationToken = cancellationToken)

        static member StartAsTask (ao: Async<Option<unit>>, ?taskCreationOptions: TaskCreationOptions, ?cancellationToken: CancellationToken) =
            Async.map (Option.iter id) ao
            |> fun a -> Async.StartAsTask(a, ?taskCreationOptions = taskCreationOptions, ?cancellationToken = cancellationToken)

[<RequireQualifiedAccess>]
module List =
    let distinctSequential (xs: 'T list) =
        []
        |> List.foldBack (fun x xs ->
            match xs with
            | [] -> [x]
            | [h] when h = x -> xs
            | h::_ when h = x -> xs
            | _ -> x::xs
        ) xs

[<RequireQualifiedAccess>]
module Set =
    let asIReadOnlyList (set: Set<'T>) = (ResizeArray set) :> IReadOnlyList<'T>

[<RequireQualifiedAccess>]
module String =
    let toUpper (s: string) = s.ToUpper()

[<RequireQualifiedAccess>]
module Task =
    let toGen t = t :> Task
    let liftGen x = task { return x } |> toGen

[<RequireQualifiedAccess>]
module CancellationToken =
    let iterTask (ct: CancellationToken) (f: unit -> unit) =
        Async.liftIter f
        |> fun a -> Async.StartAsTask(a, cancellationToken = ct)
        |> Task.toGen

    let iter (ct: CancellationToken) (f: unit -> unit) =
        Async.liftIter f
        |> fun a -> Async.Start(a, cancellationToken = ct)

namespace Akkling

open Akka.Actor
open Akkling
open FSharp.Control.Tasks.NonAffine
open FsToolkit.ErrorHandling
open System
open System.Threading.Tasks

[<AutoOpen>]
module Helpers =
    let private tryCast (t: Task<obj>) : 'Message =
        match t.Result with
        | :? 'Message as m -> m
        | o ->
            let context = Internal.InternalCurrentActorCellKeeper.Current
            if isNull context
            then failwith "Cannot cast object outside the actor system context "
            else
                match o with
                | :? (byte[]) as bytes -> 
                    let serializer = context.System.Serialization.FindSerializerForType typeof<'Message>
                    serializer.FromBinary(bytes, typeof<'Message>) :?> 'Message
                | _ -> raise (InvalidCastException("Tried to cast object to " + typeof<'Message>.ToString()))
    
    let (<!?) (tellable: #ICanTell) (msg : 'Message) = 
        tellable.Tell(msg, ActorCell.GetCurrentSelfOrNoSender())

    let (<!?!) (tellable: #ICanTell) (msg : 'Message) = 
        task { return tellable.Tell(msg, ActorCell.GetCurrentSelfOrNoSender()) }
    
    let (<??!) (tellable: #ICanTell) (msg : 'Message) = 
        tellable.Ask(msg).ContinueWith(Func<_,'Message>(tryCast), TaskContinuationOptions.ExecuteSynchronously)

    let (<??) (tellable: #ICanTell) (msg : 'Message) = 
        tellable <??! msg |> Async.AwaitTask
        
    let (<!!) (actorRef : #ICanTell<'Message>) (msg : 'Message) : Task = 
        task { return actorRef <! msg } :> Task

    let (<?!) (tell : #ICanTell<'Message>) (msg : 'Message) : Task<'Response> =
        tell <? msg |> Async.map tryCast |> Async.StartAsTask

    let (|!!>) (computation : Task) (recipient : #ICanTell<unit>) = pipeTo ActorRefs.NoSender recipient (computation |> Async.AwaitTask)

    let (<!!|) (recipient : #ICanTell<unit>) (computation : Task) = pipeTo ActorRefs.NoSender recipient (computation |> Async.AwaitTask)

    let (@@) (path1: string) (path2: string) = path1 + "/" + path2

    let (!!) (path: string) = "/" + path

[<RequireQualifiedAccess>]
module Child =
    let tryGetUntyped (mailbox: Actor<'Message>) (name: string) =
        let child : IActorRef = mailbox.UntypedContext.Child(name)

        if child.IsNobody() then None
        else Some child

    let tryGet (mailbox: Actor<'ParentMsg>) (name: string) =
        tryGetUntyped mailbox name
        |> Option.map (fun child -> TypedActorRef<'Message>(child) :> IActorRef<'Message>)
        
    let create (mailbox: Actor<_>) (name: string) (f: IActorRefFactory -> IActorRef<'Message>) =
        tryGet mailbox name
        |> function
        | Some child -> child
        | None -> f mailbox.UntypedContext

    let createUntyped (mailbox: Actor<_>) (name: string) (f: IActorRefFactory -> IActorRef) =
        tryGetUntyped mailbox name
        |> function
        | Some child -> child
        | None -> f mailbox.UntypedContext

    let createIgnore (mailbox: Actor<_>) (name: string) (f: IActorRefFactory -> IActorRef<'Message>) =
        create mailbox name f |> ignore

    let private iter (mailbox: Actor<_>) (name: string) (f: IActorRef<'Message> -> _) =
        tryGet mailbox name
        |> Option.iter (f >> ignore)

    let tell (mailbox: Actor<_>) (name: string) (msg: 'Msg) =
        iter mailbox name (fun child -> child <! msg)

    let tryAsk (mailbox: Actor<_>) (name: string) (msg: 'Msg) =
        tryGet mailbox name
        |> Option.map (fun child -> child.Ask<'Response>(msg, None))
        |> function
        | Some rsp -> 
            async { 
                let! res = rsp 
                return Some res
            }
        | None -> async { return None }

    let tryAskThenTell (mailbox: Actor<_>) (name: string) (msg: 'Msg) =
        async {
            let! res = tryAsk mailbox name msg
            
            return mailbox.Sender().Tell(res, (mailbox.Self :> IInternalTypedActorRef).Underlying)
        }
        |> Async.Start

[<RequireQualifiedAccess>]
module Actor =
    let tell (actor: IActorRef<'Message>) (msg: 'Message) = actor <! msg
    let ask (actor: IActorRef<'Message>) (msg: 'Message) = actor <? msg

[<RequireQualifiedAccess>]
module ActorSelection =
    let tell msg (actorSel: ActorSelection) = actorSel.Tell msg

namespace Akka.Configuration

open Akka
open Akka.Cluster
open Akka.FSharp
open Microsoft.Extensions.Logging
open System

[<AutoOpen>]
module private Helpers =
    let logLevelList = [ 0 .. 6 ] |> List.map enum<LogLevel>

[<RequireQualifiedAccess>]
module Config =
    [<RequireQualifiedAccess>]
    module private AkkaLogLevels =
        let [<Literal>] Debug = "DEBUG"
        let [<Literal>] Error = "ERROR"
        let [<Literal>] Info = "INFO"
        let [<Literal>] Off = "OFF"
        let [<Literal>] Warning = "WARNING"

    let empty = Configuration.parse ""

    let private applyFallback (config: Config) (rawConfig: string) =
        rawConfig
        |> Configuration.parse
        |> config.SafeWithFallback

    let logLevel (predicate: LogLevel -> bool) (config: Config) =
        logLevelList
        |> List.tryFind predicate
        |> function
        | Some LogLevel.None
        | None -> config
        | Some logLevel ->
            let logStr =
                match logLevel with
                | LogLevel.Trace 
                | LogLevel.Debug -> AkkaLogLevels.Debug
                | LogLevel.Information -> AkkaLogLevels.Info
                | LogLevel.Warning -> AkkaLogLevels.Warning
                | LogLevel.Error 
                | LogLevel.Critical -> AkkaLogLevels.Error
                | _ -> AkkaLogLevels.Off

            match logLevel with
            | LogLevel.Trace 
            | LogLevel.Debug ->
                sprintf """
                    akka.actor {
                        stdout-loglevel = %s
                        loglevel = %s
                            
                        log-config-on-start = on

                        debug {
                            receive = on
                            autoreceive = on
                            lifecycle = on
                            event-stream = on
                            unhandled = on
                        }
                    }
                    """ logStr logStr
                |> applyFallback config
            | _ ->
                sprintf "akka.actor { stdout-loglevel = %s, loglevel = %s }" logStr logStr
                |> applyFallback config

    let seedNodes (systemName: string) (nodes: (string * uint16) list) (config: Config) =
        if List.distinct nodes <> nodes then
            sprintf "Duplicate seed-node names detected on system: %s!%s%A" 
                systemName System.Environment.NewLine nodes
            |> InvalidOperationException
            |> raise
        
        let nodesStr =
            nodes
            |> List.map (fun (node, port) -> sprintf "\"akka.tcp://%s@%s:%i\"" systemName node port)
            |> String.concat ","

        sprintf "akka.cluster.seed-nodes = [ %s ]" nodesStr
        |> applyFallback config

    let instanceInfo (hostname: string) (pubHostname: string) (port: uint16) (config: Config) =
        let instanceInfo =
            sprintf 
                """
                akka.remote.dot-netty.tcp {
                    public-hostname = "%s"
                    hostname = "%s"
                    port = %i
                }
                """
                hostname pubHostname port
        
        if isNull hostname || isNull pubHostname then
            sprintf "Invalid instance configuration provided: %s%s" 
                System.Environment.NewLine instanceInfo
            |> InvalidOperationException
            |> raise

        applyFallback config instanceInfo

    let extensions (extensions: string list) (config: Config) =
        extensions 
        |> List.map (sprintf "\"%s\"") 
        |> String.concat ","
        |> sprintf "akka.extensions = [%s]"
        |> applyFallback config

    let loggers (loggers: string list) (config: Config) =
        loggers 
        |> List.map (sprintf "\"%s\"") 
        |> String.concat ","
        |> sprintf "akka.loggers = [%s]"
        |> applyFallback config

    let provider (provider: string) (config: Config) =
        provider
        |> sprintf "akka.actor.provider = \"%s\""
        |> applyFallback config

    let autoDownUnreachable (seconds: int) (config: Config) =
        sprintf "akka.cluster.auto-down-unreachable-after = %is" seconds
        |> applyFallback config

    let roles (roles: string list) (config: Config) =
        roles 
        |> String.concat ","
        |> sprintf "akka.cluster.roles = [%s]"
        |> applyFallback config

    let pubSubRoutingLogicBroadcast (config: Config) =
        "akka.cluster.pub-sub.routing-logic = broadcast"
        |> applyFallback config

namespace Akka.Cluster

open Akka.Cluster
open Akkling

[<AutoOpen>]
module ClusterExtensions =
    type Cluster with
        member this.SubscribeToMemberEvent (actorRef: IActorRef<'Message>) =
            this.Subscribe(untyped actorRef, ClusterEvent.InitialStateAsEvents, [| typedefof<ClusterEvent.IMemberEvent> |])

        member this.SubscribeToMemberEventSnapshot (actorRef: IActorRef<'Message>) =
            this.Subscribe(untyped actorRef, ClusterEvent.InitialStateAsSnapshot, [| typedefof<ClusterEvent.IMemberEvent> |])

        member this.Unsubscribe (actorRef: IActorRef<'Message>) =
            this.Unsubscribe(untyped actorRef)
