namespace Fable.SignalR

open Fable.Core
open Fable.SignalR.Shared
open System
open System.ComponentModel

[<Erase>]
module Messages =
    [<RequireQualifiedAccess>]
    type MessageType =
        | Invocation = 1
        | StreamItem = 2
        | Completion = 3
        | StreamInvocation = 4
        | CancelInvocation = 5
        | Ping = 6
        | Close = 7

    /// Defines properties common to all Hub messages.
    type HubMessageBase =
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

    /// Defines properties common to all Hub messages relating to a specific invocation.
    type HubInvocationMessage =
        inherit HubMessageBase

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The ID of the invocation relating to this message.
        /// 
        /// This is expected to be present for StreamInvocationMessage and CompletionMessage. It may
        /// be 'undefined' for an InvocationMessage if the sender does not expect a response.
        abstract invocationId: string option

    /// A hub message representing a non-streaming invocation.
    type InvocationMessage<'T> =
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType
        
        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The ID of the invocation relating to this message.
        /// 
        /// This is expected to be present for StreamInvocationMessage and CompletionMessage. It may
        /// be 'undefined' for an InvocationMessage if the sender does not expect a response.
        abstract invocationId: string option

        /// The target method name.
        abstract target: string

        /// The target method arguments.
        abstract arguments: ResizeArray<'T>

        /// The target methods stream IDs.
        abstract streamIds: ResizeArray<string> option

    /// A hub message representing a streaming invocation.
    type StreamInvocationMessage<'T> =
        inherit HubInvocationMessage

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType
        
        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The invocation ID.
        abstract invocationId: string

        /// The target method name.
        abstract target: string

        /// The target method arguments.
        abstract arguments: ResizeArray<'T>

        /// The target methods stream IDs.
        abstract streamIds: ResizeArray<string> option

    /// A hub message representing a single item produced as part of a result stream.
    type StreamItemMessage<'T> =
        inherit HubInvocationMessage

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType
        
        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The ID of the invocation relating to this message.
        abstract invocationId: string

        /// The item produced by the server.
        abstract item: 'T

    /// A hub message representing the result of an invocation.
    type CompletionMessage<'T> =
        inherit HubInvocationMessage

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType
        
        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The invocation ID.
        abstract invocationId: string

        /// The error produced by the invocation, if any.
        /// 
        /// Either CompletionMessage.error or CompletionMessage.result must be defined, but not both.
        abstract error: string option

        /// The result produced by the invocation, if any.
        /// 
        /// Either CompletionMessage.error or CompletionMessage.result must be defined, but not both.
        abstract result: 'T option

    /// A hub message indicating that the sender is still active.
    type PingMessage =
        inherit HubMessageBase

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

    /// A hub message indicating that the sender is closing the connection.
    /// 
    /// If CloseMessage.error is defined, the sender is closing the connection due to an error.
    type CloseMessage =
        inherit HubMessageBase

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// The error that triggered the close, if any.
        /// 
        /// If this property is undefined, the connection was closed normally and without error.
        abstract error: string option

        /// If true, clients with automatic reconnects enabled should attempt to reconnect after 
        /// receiving the CloseMessage. Otherwise, they should not.
        abstract allowReconnect: bool option

    /// A hub message sent to request that a streaming invocation be canceled.
    type CancelInvocationMessage =
        inherit HubInvocationMessage

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType
        
        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The ID of the invocation relating to this message.
        /// 
        /// This is expected to be present for StreamInvocationMessage and CompletionMessage. It may
        /// be 'undefined' for an InvocationMessage if the sender does not expect a response.
        abstract invocationId: string option

    type HubMessage<'StreamInvocation,'Args,'Completion,'StreamItem> =
        U8<InvocationMessage<'Args>, 
           InvocationMessage<InvokeArg<'Args>>, 
           StreamItemMessage<'StreamItem>, 
           CompletionMessage<'Completion>, 
           StreamInvocationMessage<'StreamInvocation>, 
           CancelInvocationMessage, 
           PingMessage, 
           CloseMessage>

/// A protocol abstraction for communicating with SignalR Hubs.
type IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    /// The name of the protocol. This is used by SignalR to resolve the protocol between the client 
    /// and server.
    abstract name: string

    /// The version of the protocol.
    abstract version: int

    /// The TransferFormat of the protocol.
    abstract transferFormat: TransferFormat

    /// Creates an array of HubMessage objects from the specified serialized representation.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the `input` parameter must be a string, otherwise 
    /// it must be an ArrayBuffer.
    abstract parseMessages : input: U2<string,JS.ArrayBuffer> * logger: ILogger 
        -> ResizeArray<Messages.HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi>>

    /// Writes the specified HubMessage to a string or ArrayBuffer and returns it.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the result of this method will be a string, 
    /// otherwise it will be an ArrayBuffer.
    abstract writeMessage: message: Messages.HubMessage<'ClientStreamFromApi,'ClientApi,'ClientApi,'ClientStreamToApi> 
        -> U2<string, JS.ArrayBuffer>

/// A stream interface to stream items to the server.
type ISubject<'T> =
    abstract next: item: 'T -> unit

    abstract error: err: exn -> unit

    abstract complete: unit -> unit

    abstract subscribe: observer: #IStreamSubscriber<'T> -> System.IDisposable

/// Stream implementation to stream items to the server.
type Subject<'T> =
    interface ISubject<'T> with
        member this.next (item: 'T) = this.next(item)
        member this.error (err: exn) = this.error(err)
        member this.complete () = this.complete()
        member this.subscribe (observer: #IStreamSubscriber<'T>) = this.subscribe(observer)

    [<Emit("$0.next($1)")>]
    member _.next (item: 'T) : unit = jsNative

    [<Emit("$0.error($1)")>]
    member _.error (err: exn) : unit = jsNative

    [<Emit("$0.complete()")>]
    member _.complete () : unit = jsNative

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.subscribe($1)")>]
    member _.subscribe' (observer: #IStreamSubscriber<'T>) : ISubscription = jsNative
    
    member inline this.subscribe (observer: #IStreamSubscriber<'T>) =
        this.subscribe'(observer)
        |> fun sub -> { new System.IDisposable with member _.Dispose () = sub.dispose() }

    member inline this.subscribe (observer: StreamSubscriber<'T>) =
        this.subscribe(unbox<IStreamSubscriber<'T>> observer)

/// The connection state to the hub.
[<RequireQualifiedAccess;StringEnum(CaseRules.None)>]
type ConnectionState =
    | Connected
    | Connecting
    | Disconnected
    | Disconnecting
    | Reconnecting

[<Erase>]
type internal IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    [<Emit("$0.baseUrl")>]
    member _.baseUrl : string = jsNative
    
    [<Emit("$0.connectionId")>]
    member _.connectionId : string option = jsNative
    
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.invoke($1...)")>]
    member _.invoke' (methodName: string, [<ParamArray>] args: ResizeArray<obj>) : JS.Promise<unit> = jsNative

    member inline this.invoke (msg: 'ClientApi, invocationId: System.Guid) : Async<unit> =
        this.invoke'(HubMethod.Invoke, ResizeArray [| msg :> obj; invocationId :> obj |]) |> Async.AwaitPromise
    
    [<Emit("$0.keepAliveIntervalInMilliseconds")>]
    member _.keepAliveInterval : int = jsNative
    
    [<Emit("$0.off($1)")>]
    member _.off' (methodName: string) : unit = jsNative
    [<Emit("$0.off($1, $2)")>]
    member _.off'<'T> (methodName: string, handler: 'T -> unit) : unit = jsNative

    member inline this.off () = this.off'(HubMethod.Send)
    
    [<Emit("$0.on($1, $2)")>]
    member _.on<'T> (methodName: string, handler: 'T -> unit) : unit = jsNative

    [<Emit("$0.onclose($1)")>]
    member _.onClose (callback: (exn option -> unit)) : unit = jsNative

    member inline this.onMessage (callback: 'ServerApi -> unit) = 
        this.on<'ServerApi>(HubMethod.Send, callback)
    
    [<Emit("$0.onreconnected($1)")>]
    member _.onReconnected (callback: (string option -> unit)) : unit = jsNative

    [<Emit("$0.onreconnecting($1)")>]
    member _.onReconnecting (callback: (exn option -> unit)) : unit = jsNative
        
    [<Emit("$0.send($1...)")>]
    member _.send' (methodName: string, [<ParamArray>] args: ResizeArray<obj>) : JS.Promise<unit> = jsNative

    member inline this.send (msg: 'ClientApi) = 
        this.send'(HubMethod.Send, ResizeArray [| msg :> obj |]) |> Async.AwaitPromise     

    [<Emit("$0.serverTimeoutInMilliseconds")>]
    member _.serverTimeout : int = jsNative
    
    member inline this.start () = this.startAsPromise() |> Async.AwaitPromise

    [<Emit("$0.start()")>]
    member _.startAsPromise () : JS.Promise<unit> = jsNative

    [<Emit("$0.state")>]
    member _.state : ConnectionState = jsNative
    
    member inline this.stop () = this.stopAsPromise() |> Async.AwaitPromise

    [<Emit("$0.stop()")>]
    member _.stopAsPromise () : JS.Promise<unit> = jsNative

    member inline this.stopNow () = this.stopAsPromise() |> Promise.start
    
    [<Emit("$0.stream($1...)")>]
    member _.stream (methodName: string, [<ParamArray>] args: ResizeArray<obj>) : StreamResult<'ServerStreamApi> = jsNative

    member inline this.streamFrom (msg: 'ClientStreamFromApi) : StreamResult<'ServerStreamApi> = 
        this.stream(HubMethod.StreamFrom, ResizeArray [| msg :> obj |])
    
    [<Emit("$0.send($1, $2)")>]
    member _.streamTo' (methodName: string, subscriber: ISubject<'ClientStreamToApi>) : JS.Promise<unit> = jsNative

    member inline this.streamTo (subject: ISubject<'ClientStreamToApi>) = 
        this.streamTo'(HubMethod.StreamTo, subject) |> Async.AwaitPromise

type internal IHubConnectionBuilder<'ClientApi,'ServerApi> =
    abstract configureLogging: logLevel: LogLevel -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract configureLogging: logger: ILogger -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract configureLogging: logLevel: string -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withUrl: url: string -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withUrl: url: string * transportType: TransportType -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withUrl: url: string * options: Http.ConnectionOptions -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withHubProtocol : protocol: IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withAutomaticReconnect: unit -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withAutomaticReconnect: retryDelays: seq<int> -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withAutomaticReconnect: reconnectPolicy: RetryPolicy -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract build: unit -> IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>

module internal Bindings =
    open Fable.Core.JsInterop

    [<Erase>]
    type SignalR =
        [<Emit("new $0.HubConnectionBuilder()")>]
        member _.HubConnectionBuilder () : IHubConnectionBuilder<'ClientApi,'ServerApi> = jsNative

        [<Emit("new $0.HttpClient($1)")>]
        member _.HttpClient (logger: ILogger) : Http.DefaultClient = jsNative

        [<Emit("$0.NullLogger.instance")>]
        member _.NullLogger () : NullLogger = jsNative

        [<Emit("new $0.Subject()")>]
        member _.Subject<'T> () : Subject<'T> = jsNative

    let signalR : SignalR = importAll "@microsoft/signalr"

[<Erase>]
type Hub<'ClientApi,'ServerApi> =
    /// The base url of the hub connection.
    abstract baseUrl : string

    /// The connectionId to the hub of this client.
    abstract connectionId : string option
    
    /// Invokes a hub method on the server.
    /// 
    /// The async returned by this method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the async call.
    abstract invoke: msg: 'ClientApi -> Async<'ServerApi>

    /// Invokes a hub method on the server.
    /// 
    /// The promise returned by this method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the promise.
    abstract invokeAsPromise: msg: 'ClientApi -> JS.Promise<'ServerApi>

    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    abstract keepAliveInterval : int
    
    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The async returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    abstract send: msg: 'ClientApi -> Async<unit>

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The promise returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    abstract sendAsPromise: msg: 'ClientApi -> JS.Promise<unit>

    /// Invokes a hub method on the server. Does not wait for a response from the receiver. The server may still
    /// be processing the invocation.
    abstract sendNow: msg: 'ClientApi -> unit
    
    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be 
    /// terminated with an error.
    ///
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    abstract serverTimeout : int

    /// The state of the hub connection to the server.
    abstract state : ConnectionState

[<RequireQualifiedAccess>]
module StreamHub =
    [<Erase>]
    type ClientToServer<'ClientApi,'ClientStreamApi,'ServerApi> =
        inherit Hub<'ClientApi,'ServerApi>

        /// Returns an async that when invoked, starts streaming to the hub.
        abstract streamTo: subject: ISubject<'ClientStreamApi> -> Async<unit>

        /// Returns a promise that when invoked, starts streaming to the hub.
        abstract streamToAsPromise: subject: ISubject<'ClientStreamApi> -> JS.Promise<unit>

        /// Streams to the hub immediately.
        abstract streamToNow: subject: ISubject<'ClientStreamApi> -> unit
    
    [<Erase>]   
    type ServerToClient<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
        inherit Hub<'ClientApi,'ServerApi>

        /// Streams from the hub.
        abstract streamFrom: msg: 'ClientStreamApi -> Async<StreamResult<'ServerStreamApi>>

        /// Streams from the hub.
        abstract streamFromAsPromise: msg: 'ClientStreamApi -> JS.Promise<StreamResult<'ServerStreamApi>>

    [<Erase>]
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
    { onClose: (exn option -> unit) option
      onMessage: (obj -> unit) option
      onReconnected: (string option -> unit) option
      onReconnecting: (exn option -> unit) option }

    member inline this.apply (hub: IHubConnection<_,_,_,_,_>) =
        Option.iter hub.onClose this.onClose
        Option.iter hub.onMessage (unbox this.onMessage)
        Option.iter hub.onReconnected this.onReconnected
        Option.iter hub.onReconnecting this.onReconnecting

    static member empty =
        { onClose = None
          onMessage = None
          onReconnecting = None
          onReconnected = None }

type HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
    internal (hub: IHubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>, handlers: Handlers) =

    let cts = new System.Threading.CancellationTokenSource()
    
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
                        if hub.state = ConnectionState.Connected then
                            processSends waitingConnections
                            |> fun a -> Async.StartImmediate(a, cts.Token)
                            []
                        else waitingConnections

                    let! msg = inbox.Receive()
                    
                    let hubId = hub.connectionId
                    
                    return!
                        match msg with
                        | HubMailbox.ProcessSends ->
                            processSends waitingConnections
                            |> fun a -> Async.StartImmediate(a, cts.Token)

                            loop waitingInvocations []
                        | HubMailbox.Send action ->
                            let newConnections =
                                if hub.state = ConnectionState.Connected then 
                                    action() |> fun a -> Async.StartImmediate(a, cts.Token)
                                    []
                                else [ action ]

                            loop waitingInvocations (newConnections @ waitingConnections)
                        | HubMailbox.ServerRsp (connectionId, invocationId, msg) ->
                            match hubId,connectionId, msg with
                            | Some hubId, connectionId, msg when hubId = connectionId ->
                                waitingInvocations.TryFind(invocationId)
                                |> Option.iter(fun reply -> reply.Reply(msg))

                                loop (waitingInvocations.Remove(invocationId)) waitingConnections
                            | _ -> loop waitingInvocations waitingConnections
                        | HubMailbox.StartInvocation (serverMsg, reply) ->
                            let newGuid = System.Guid.NewGuid()

                            let newConnections =
                                if hub.state = ConnectionState.Connected then
                                    hub.invoke(serverMsg, newGuid) |> fun a -> Async.StartImmediate(a, cts.Token)
                                    []
                                else [ fun () -> hub.invoke(serverMsg, newGuid) ]
                            loop (waitingInvocations.Add(newGuid, reply)) (newConnections @ waitingConnections)
                }

            loop Map.empty []
        , cancellationToken = cts.Token)

    let onRsp = HubMailbox.ServerRsp >> mailbox.Post

    do 
        { handlers with 
            onReconnected =
                handlers.onReconnected
                |> Option.map(fun f -> 
                    fun strOpt ->
                        mailbox.Post(HubMailbox.ProcessSends)
                        f strOpt)
                |> Option.defaultValue (fun _ -> mailbox.Post(HubMailbox.ProcessSends))
                |> Some }
        |> fun handlers -> handlers.apply(hub)
        hub.on<InvokeArg<'ServerApi>>(HubMethod.Invoke, fun rsp -> onRsp(rsp.connectionId, rsp.invocationId, rsp.message))

    interface System.IDisposable with
        member _.Dispose () =
            hub.off'(HubMethod.Invoke, onRsp)
            cts.Cancel()
            cts.Dispose()
            hub.stopNow()

    interface Hub<'ClientApi,'ServerApi> with
        member this.baseUrl = this.baseUrl
        member this.connectionId = this.connectionId
        member this.invoke msg = this.invoke msg
        member this.invokeAsPromise msg = this.invokeAsPromise msg
        member this.keepAliveInterval = this.keepAliveInterval
        member this.send msg = this.send msg
        member this.sendAsPromise msg = this.sendAsPromise msg
        member this.sendNow (msg: 'ClientApi) = this.sendNow(msg)
        member this.serverTimeout = this.serverTimeout
        member this.state = this.state

    interface StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> with
        member this.streamTo subject = this.streamTo subject
        member this.streamToAsPromise subject = this.streamToAsPromise subject
        member this.streamToNow (subject) = this.streamToNow(subject)

    interface StreamHub.ServerToClient<'ClientApi,'ClientStreamFromApi,'ServerApi,'ServerStreamApi> with
        member this.streamFrom msg = this.streamFrom msg
        member this.streamFromAsPromise msg = this.streamFromAsPromise msg

    interface StreamHub.Bidrectional<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>

    /// The base url of the hub connection.
    member _.baseUrl = hub.baseUrl
    
    /// The connectionId to the hub of this client.
    member _.connectionId = hub.connectionId
    
    /// Invokes a hub method on the server.
    /// 
    /// The async returned by this method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the async call.
    member _.invoke (msg: 'ClientApi) =
        mailbox.PostAndAsyncReply(fun reply -> HubMailbox.StartInvocation(msg, reply))
        
    /// Invokes a hub method on the server.
    /// 
    /// The promise returned by this method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the promise.
    member this.invokeAsPromise (msg: 'ClientApi) = 
        this.invoke(msg) |> Async.StartAsPromise

    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    member _.keepAliveInterval = hub.keepAliveInterval
    
    /// Removes all handlers.
    member _.off () = hub.off()
    
    /// Registers a handler that will be invoked when the connection is closed.
    member _.onClose (callback: (exn option -> unit)) = hub.onClose(callback)

    /// Callback when a new message is recieved.
    member _.onMessage (callback: 'ServerApi -> unit) = hub.onMessage(callback)
    
    /// Callback when the connection successfully reconnects.
    member _.onReconnected (callback: (string option -> unit)) = hub.onReconnected(callback)

    /// Callback when the connection starts reconnecting.
    member _.onReconnecting (callback: (exn option -> unit)) = hub.onReconnecting(callback)
        
    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The async returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    member _.send (msg: 'ClientApi) = 
        async { return mailbox.Post(HubMailbox.Send(fun () -> hub.send(msg))) }

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The promise returned by this method resolves when the client has sent the invocation to the server. 
    /// The server may still be processing the invocation.
    member this.sendAsPromise (msg: 'ClientApi) = this.send(msg) |> Async.StartAsPromise

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    member this.sendNow (msg: 'ClientApi) = this.send(msg) |> fun a -> Async.StartImmediate(a, cts.Token)

    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be 
    /// terminated with an error.
    ///
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    member _.serverTimeout = hub.serverTimeout
    
    /// Starts the connection.
    member _.start () = 
        async {
            if hub.state = ConnectionState.Disconnected then 
                do! hub.start()
                mailbox.Post(HubMailbox.ProcessSends)
        }
        
    /// Starts the connection.
    member this.startAsPromise () = this.start() |> Async.StartAsPromise

    /// Starts the connection immediately.
    member this.startNow () = 
        this.start() |> fun a -> Async.StartImmediate(a, cts.Token)

    /// The state of the hub connection to the server.
    member _.state = hub.state
    
    /// Stops the connection.
    member _.stop () = 
        async {
            if hub.state <> ConnectionState.Disconnected then
                do! hub.stop()
        }
        
    /// Stops the connection.
    member this.stopAsPromise () = 
        this.stop() |> Async.StartAsPromise

    /// Stops the connection immediately.
    member this.stopNow () = this.stop() |> fun a -> Async.StartImmediate(a, cts.Token)
    
    /// Streams from the hub.
    member _.streamFrom (msg: 'ClientStreamFromApi) = 
        mailbox.PostAndAsyncReply <| fun reply -> 
            HubMailbox.Send <| fun () -> 
                async { return reply.Reply(hub.streamFrom(msg)) }

    /// Streams from the hub.
    member this.streamFromAsPromise (msg: 'ClientStreamFromApi) = 
        this.streamFrom(msg) |> Async.StartAsPromise

    /// Returns an async that when invoked, starts streaming to the hub.
    member _.streamTo (subject: ISubject<'ClientStreamToApi>) =
        async { return mailbox.Post(HubMailbox.Send(fun () -> hub.streamTo(subject))) }
        
    /// Returns a promise that when invoked, starts streaming to the hub.
    member this.streamToAsPromise (subject: ISubject<'ClientStreamToApi>) = 
        this.streamTo(subject) |> Async.StartAsPromise

    /// Streams to the hub immediately.
    member this.streamToNow (subject: ISubject<'ClientStreamToApi>) = 
        this.streamTo(subject) |> fun a -> Async.StartImmediate(a, cts.Token)
