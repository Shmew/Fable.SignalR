namespace Fable.SignalR

open Fable.SignalR.Shared
open Microsoft.AspNetCore.SignalR.Client
open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type internal IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> (hub: HubConnection) =
    member _.ConnectionId = 
        match hub.ConnectionId with
        | null -> None
        | id -> Some id

    member _.Hub = hub

    member _.Invoke (msg: 'ClientApi, invocationId: System.Guid, ?cancellationToken: CancellationToken) =
        match cancellationToken with
        | Some ct -> hub.InvokeAsync<unit>(HubMethod.Invoke, msg, invocationId, ct)
        | None -> hub.InvokeAsync<unit>(HubMethod.Invoke, msg, invocationId)
        |> Async.AwaitTask

    member _.KeepAliveInterval = hub.KeepAliveInterval
    
    member _.OnClosed (f: exn option -> Async<unit>) = 
        wrapEvent f
        |> hub.add_Closed
    
    member _.OnMessage (callback: 'ServerApi -> Async<unit>) = 
        hub.On<'ServerApi>(HubMethod.Send, System.Func<'ServerApi,Task>(callback >> Async.StartAsTask >> genTask))

    member _.OnReconnecting (f: exn option -> Async<unit>) =
        wrapEvent f
        |> hub.add_Reconnecting

    member _.OnReconnected (f: string option -> Async<unit>) =
        wrapEvent f
        |> hub.add_Reconnected

    member _.RemoveClosed (f: exn option -> Async<unit>) = 
        wrapEvent f
        |> hub.remove_Closed
    
    member _.RemoveInvoke () = hub.Remove(HubMethod.Invoke)

    member _.RemoveReconnecting (f: exn option -> Async<unit>) =
        wrapEvent f
        |> hub.remove_Reconnecting

    member _.RemoveReconnected (f: string option -> Async<unit>) =
        wrapEvent f
        |> hub.remove_Reconnected
    
    member _.RemoveSend () = hub.Remove(HubMethod.Send)
    
    member _.Send (msg: 'ClientApi, ?cancellationToken: CancellationToken) = 
        match cancellationToken with
        | Some ct -> hub.SendAsync(HubMethod.Send, msg, ct)
        | None -> hub.SendAsync(HubMethod.Send, msg)
        |> Async.AwaitTask

    member this.SendNow (msg: 'ClientApi, ?cancellationToken: CancellationToken) = 
        this.Send(msg, ?cancellationToken = cancellationToken)
        |> Async.Start

    member _.ServerTimeout = hub.ServerTimeout
    
    member _.Start (?cancellationToken: CancellationToken) =
        match cancellationToken with
        | Some ct -> hub.StartAsync(ct)
        | None -> hub.StartAsync()
        |> Async.AwaitTask

    member this.StartNow (?cancellationToken: CancellationToken) = 
        this.Start(?cancellationToken = cancellationToken)
        |> Async.Start

    member _.State = hub.State
    
    member _.Stop (?cancellationToken: CancellationToken) = 
        match cancellationToken with
        | Some ct -> hub.StopAsync(ct)
        | None -> hub.StopAsync()
        |> Async.AwaitTask

    member this.StopNow (?cancellationToken: CancellationToken) = 
        this.Stop(?cancellationToken = cancellationToken)
        |> Async.Start
        
    member _.StreamFrom (msg: 'ClientStreamFromApi, ?cancellationToken: CancellationToken) =
        match cancellationToken with
        | Some ct -> hub.StreamAsync<'ServerStreamApi>(HubMethod.StreamFrom, msg, ct)
        | None -> hub.StreamAsync<'ServerStreamApi>(HubMethod.StreamFrom, msg)
    
    member _.StreamTo (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>, ?cancellationToken: CancellationToken) =
        match cancellationToken with
        | Some ct -> hub.SendAsync(HubMethod.StreamTo, asyncEnum, ct)
        | None -> hub.SendAsync(HubMethod.StreamTo, asyncEnum)
        |> Async.AwaitTask

    member this.StreamToNow (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>, ?cancellationToken: CancellationToken) =
        this.StreamTo(asyncEnum, ?cancellationToken = cancellationToken)
        |> Async.Start

// fsharplint:disable-next-line
type Hub<'ClientApi,'ServerApi> =
    /// The connectionId to the hub of this client.
    abstract ConnectionId : string option
    
    /// Invokes a hub method on the server.
    /// 
    /// This method resolves when the server indicates it has finished invoking the method. 
    /// When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the async call.
    abstract Invoke: msg: 'ClientApi -> Async<'ServerApi>

    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    abstract KeepAliveInterval : TimeSpan
    
    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The async returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    abstract Send: msg: 'ClientApi * ?cancellationToken: CancellationToken -> Async<unit>
    
    /// Invokes a hub method on the server. Does not wait for a response from the receiver. The server may still
    /// be processing the invocation.
    abstract SendNow: msg: 'ClientApi * ?cancellationToken: CancellationToken -> unit

    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be 
    /// terminated with an error.
    ///
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    abstract ServerTimeout : TimeSpan

    /// The state of the hub connection to the server.
    abstract State : HubConnectionState

[<RequireQualifiedAccess>]
module StreamHub =
    // fsharplint:disable-next-line
    type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> =
        inherit Hub<'ClientApi,'ServerApi>

        /// Returns an asynchronous computation that when invoked, starts streaming to the hub.
        abstract StreamTo: asyncEnum: IAsyncEnumerable<'ClientStreamApi> * ?cancellationToken: CancellationToken -> Async<unit>

        /// Streams to the hub immediately.
        abstract StreamToNow: asyncEnum: IAsyncEnumerable<'ClientStreamApi> * ?cancellationToken: CancellationToken -> unit
    
    // fsharplint:disable-next-line
    type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
        inherit Hub<'ClientApi,'ServerApi>

        /// Streams from the hub.
        abstract StreamFrom: msg: 'ClientStreamApi -> Async<IAsyncEnumerable<'ServerStreamApi>>
        
    // fsharplint:disable-next-line
    type Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> = 
        inherit Hub<'ClientApi,'ServerApi>
        inherit ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi>
        inherit ServerToClient<'ClientApi,'ClientStreamFromApi,'ServerApi,'ServerStreamApi>
    
[<NoComparison;NoEquality>]
[<RequireQualifiedAccess>]
type internal HubMailbox<'ClientApi,'ServerApi> =
    | ProcessSends
    | Send of callback:(unit -> Async<unit>)
    | ServerRsp of connectionId:string * invocationId: System.Guid * rsp:'ServerApi
    | StartInvocation of msg:'ClientApi * replyChannel:AsyncReplyChannel<'ServerApi>

[<NoComparison;NoEquality>]
type internal Handlers =
    { OnClosed: (exn option -> Async<unit>) option
      OnReconnected: (string option -> Async<unit>) option
      OnReconnecting: (exn option -> Async<unit>) option }

    member inline this.Apply (hub: IHubConnection<_,_,_,_,_>) =
        Option.iter hub.OnClosed this.OnClosed
        Option.iter hub.OnReconnected this.OnReconnected
        Option.iter hub.OnReconnecting this.OnReconnecting

    static member empty =
        { OnClosed = None
          OnReconnecting = None
          OnReconnected = None }

type HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    internal (hub: IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>, handlers: Handlers) =

    let cts = new CancellationTokenSource()
    
    let getLinkedCT (ct: CancellationToken option) =
        match ct with
        | None -> cts.Token
        | Some ct -> CancellationTokenSource.CreateLinkedTokenSource(cts.Token, ct).Token

    let processSends (pendingActions: (unit -> Async<unit>) list) =
        async {
            pendingActions
            |> List.iter (fun a -> Async.StartImmediate(a(), cts.Token))
        }
        
    let mailbox =
        MailboxProcessor.Start (fun inbox ->
            let rec loop (waitingInvocations: Map<System.Guid,AsyncReplyChannel<'ServerApi>>) (waitingConnections: (unit -> Async<unit>) list) =
                async {
                    let waitingConnections =
                        if hub.State = HubConnectionState.Connected then
                            processSends waitingConnections
                            |> fun a -> Async.Start(a, cts.Token)
                            []
                        else waitingConnections

                    let! msg = inbox.Receive()
                    
                    let hubId = hub.ConnectionId
                    
                    return!
                        match msg with
                        | HubMailbox.ProcessSends ->
                            processSends waitingConnections
                            |> fun a -> Async.Start(a, cts.Token)

                            loop waitingInvocations []
                        | HubMailbox.Send action ->
                            let newConnections =
                                if hub.State = HubConnectionState.Connected then 
                                    action() |> fun a -> Async.Start(a, cts.Token)
                                    []
                                else [ action ]

                            loop waitingInvocations (newConnections @ waitingConnections)
                        | HubMailbox.ServerRsp (connectionId, invocationId, msg) ->
                            match hubId, connectionId, msg with
                            | Some hubId, connectionId, msg when hubId = connectionId ->
                                waitingInvocations.TryFind(invocationId)
                                |> Option.iter(fun reply -> reply.Reply(msg))

                                loop (waitingInvocations.Remove(invocationId)) waitingConnections
                            | _ -> loop waitingInvocations waitingConnections
                        | HubMailbox.StartInvocation (serverMsg, reply) ->
                            let newGuid = System.Guid.NewGuid()

                            let newConnections =
                                if hub.State = HubConnectionState.Connected then
                                    hub.Invoke(serverMsg, newGuid, cts.Token) |> fun a -> Async.Start(a, cts.Token)
                                    []
                                else [ fun () -> hub.Invoke(serverMsg, newGuid, cts.Token) ]
                            loop (waitingInvocations.Add(newGuid, reply)) (newConnections @ waitingConnections)
                }

            loop Map.empty []
        , cancellationToken = cts.Token)

    let onRsp rsp = 
        async {
            return
                HubMailbox.ServerRsp (rsp.connectionId, rsp.invocationId, rsp.message)
                |> mailbox.Post
        }
        |> Async.StartAsTask
        |> genTask
        
    let invokeHandler =
        hub.Hub.On<InvokeArg<'ServerApi>>(HubMethod.Invoke, System.Func<_,_> onRsp)

    do 
        { handlers with 
            OnReconnected =
                handlers.OnReconnected
                |> Option.map(fun f -> 
                    fun strOpt ->
                        mailbox.Post(HubMailbox.ProcessSends)
                        f strOpt)
                |> Option.defaultValue (fun _ -> async { return mailbox.Post(HubMailbox.ProcessSends) })
                |> Some }
        |> fun handlers -> handlers.Apply(hub)

    interface System.IDisposable with
        member _.Dispose () =
            async {
                do! hub.Hub.DisposeAsync().AsTask() |> Async.AwaitTask
                do cts.Cancel()
                do cts.Dispose()
                
                return invokeHandler.Dispose()
            }
            |> Async.Start
            
    interface Hub<'ClientApi,'ServerApi> with
        member this.ConnectionId = this.ConnectionId
        member this.Invoke msg = this.Invoke msg
        member this.KeepAliveInterval = this.KeepAliveInterval
        member this.Send (msg, ?ct) = this.Send(msg, ?cancellationToken = ct)
        member this.SendNow (msg: 'ClientApi, ?ct) = this.SendNow(msg, ?cancellationToken = ct)
        member this.ServerTimeout = this.ServerTimeout
        member this.State = this.State

    interface StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> with
        member this.StreamTo (asyncEnum, ?ct) = this.StreamTo(asyncEnum, ?cancellationToken = ct)
        member this.StreamToNow (asyncEnum, ?ct) = this.StreamToNow(asyncEnum, ?cancellationToken = ct)

    interface StreamHub.ServerToClient<'ClientApi,'ClientStreamFromApi,'ServerApi,'ServerStreamApi> with
        member this.StreamFrom msg = this.StreamFrom msg

    interface StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    
    /// The connectionId to the hub of this client.
    member _.ConnectionId = hub.ConnectionId
    
    /// Invokes a hub method on the server.
    /// 
    /// The async returned by this method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the async call.
    member _.Invoke (msg: 'ClientApi) =
        mailbox.PostAndAsyncReply(fun reply -> HubMailbox.StartInvocation(msg, reply))

    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    member _.KeepAliveInterval = hub.KeepAliveInterval
    
    /// Registers a handler that will be invoked when the connection is closed.
    member _.OnClosed (callback: (exn option -> Async<unit>)) = hub.OnClosed(callback)

    /// Callback when a new message is recieved.
    member _.OnMessage (callback: 'ServerApi -> Async<unit>) = hub.OnMessage(callback)
    
    /// Callback when the connection successfully reconnects.
    member _.OnReconnected (callback: (string option -> Async<unit>)) = hub.OnReconnected(callback)

    /// Callback when the connection starts reconnecting.
    member _.OnReconnecting (callback: (exn option -> Async<unit>)) = hub.OnReconnecting(callback)
        
    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The async returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    member _.Send (msg: 'ClientApi, ?cancellationToken: CancellationToken) =
        let ct = getLinkedCT cancellationToken

        async { return mailbox.Post(HubMailbox.Send(fun () -> hub.Send(msg, ct))) }

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    member this.SendNow (msg: 'ClientApi, ?cancellationToken: CancellationToken) = 
        let ct = getLinkedCT cancellationToken

        this.Send(msg, ct) 
        |> fun a -> Async.Start(a, ct)

    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be 
    /// terminated with an error.
    ///
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    member _.ServerTimeout = hub.ServerTimeout
    
    /// Starts the connection.
    member _.Start (?cancellationToken: CancellationToken) = 
        let ct = getLinkedCT cancellationToken

        async {
            if hub.State = HubConnectionState.Disconnected then 
                do! hub.Start(ct)
                mailbox.Post(HubMailbox.ProcessSends)
        }

    /// Starts the connection immediately.
    member this.StartNow (?cancellationToken: CancellationToken) = 
        let ct = getLinkedCT cancellationToken

        this.Start(ct) 
        |> fun a -> Async.Start(a, ct)

    /// The state of the hub connection to the server.
    member _.State = hub.State
    
    /// Stops the connection.
    member _.Stop (?cancellationToken: CancellationToken) = 
        let ct = getLinkedCT cancellationToken

        async {
            if hub.State <> HubConnectionState.Disconnected then
                do! hub.Stop(ct)
        }

    /// Stops the connection immediately.
    member this.StopNow (?cancellationToken: CancellationToken) = 
        let ct = getLinkedCT cancellationToken

        this.Stop(ct) 
        |> fun a -> Async.Start(a, ct)
    
    /// Streams from the hub.
    member _.StreamFrom (msg: 'ClientStreamFromApi) = 
        mailbox.PostAndAsyncReply <| fun reply -> 
            HubMailbox.Send <| fun () -> 
                async { return reply.Reply(hub.StreamFrom(msg)) }

    /// Returns an async that when invoked, starts streaming to the hub.
    member _.StreamTo (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>, ?cancellationToken: CancellationToken) =
        let ct = getLinkedCT cancellationToken

        async { return mailbox.Post(HubMailbox.Send(fun () -> hub.StreamTo(asyncEnum, ct))) }

    /// Streams to the hub immediately.
    member this.StreamToNow (asyncEnum: IAsyncEnumerable<'ClientStreamToApi>, ?cancellationToken: CancellationToken) =
        let ct = getLinkedCT cancellationToken

        this.StreamTo(asyncEnum, ct) 
        |> fun a -> Async.StartImmediate(a, ct)
    