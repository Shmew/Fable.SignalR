namespace Fable.SignalR

open Fable.Core
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

        /// A MessageHeaders dictionary containing headers attached to the message.
        abstract headers: Map<string,string> option

        /// The ID of the invocation relating to this message.
        /// 
        /// This is expected to be present for StreamInvocationMessage and CompletionMessage. It may
        /// be 'undefined' for an InvocationMessage if the sender does not expect a response.
        abstract invocationId: string option

    /// A hub message representing a non-streaming invocation.
    type InvocationMessage<'T> =
        inherit HubInvocationMessage

        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// The target method name.
        abstract target: string

        /// The target method arguments.
        abstract arguments: ResizeArray<'T option>

        /// The target methods stream IDs.
        abstract streamIds: ResizeArray<string> option

    /// A hub message representing a streaming invocation.
    type StreamInvocationMessage<'T> =
        inherit HubInvocationMessage
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// The invocation ID.
        abstract invocationId: string

        /// The target method name.
        abstract target: string

        /// The target method arguments.
        abstract arguments: ResizeArray<'T option>

        /// The target methods stream IDs.
        abstract streamIds: ResizeArray<string> option

    /// A hub message representing a single item produced as part of a result stream.
    type StreamItemMessage<'T> =
        inherit HubInvocationMessage
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// The invocation ID.
        abstract invocationId: string

        /// The item produced by the server.
        abstract item: 'T option

    /// A hub message representing the result of an invocation.
    type CompletionMessage<'T> =
        inherit HubInvocationMessage
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

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

        /// If true, clients with automatic reconnects enabled should attempt to reconnect after receiving the CloseMessage. Otherwise, they should not.
        abstract allowReconnect: bool option

    /// A hub message sent to request that a streaming invocation be canceled.
    type CancelInvocationMessage =
        inherit HubInvocationMessage
        /// A MessageType value indicating the type of this message.
        abstract ``type``: MessageType

        /// The invocation ID.
        abstract invocationId: string

    type HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi> =
        U7<InvocationMessage<'ServerApi>, 
           StreamItemMessage<'ServerStreamApi>, 
           CompletionMessage<'ServerApi>, 
           StreamInvocationMessage<'ClientStreamApi>, 
           CancelInvocationMessage, 
           PingMessage, 
           CloseMessage>

/// A protocol abstraction for communicating with SignalR Hubs.
type IHubProtocol<'ClientStreamApi,'ServerApi,'ServerStreamApi> =
    /// The name of the protocol. This is used by SignalR to resolve the protocol between the client and server.
    abstract name: string

    /// The version of the protocol.
    abstract version: float

    /// The TransferFormat of the protocol.
    abstract transferFormat: TransferFormat

    /// Creates an array of HubMessage objects from the specified serialized representation.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the `input` parameter must be a string, otherwise it must be an ArrayBuffer.
    abstract parseMessages : input: U3<string,JS.ArrayBuffer,Buffer> * ?logger: ILogger -> ResizeArray<Messages.HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi>>

    /// Writes the specified HubMessage to a string or ArrayBuffer and returns it.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the result of this method will be a string, otherwise it will be an ArrayBuffer.
    abstract writeMessage: message: Messages.HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi> -> U2<string, JS.ArrayBuffer>

/// Stream implementation to stream items to the server.
type Subject<'T> =
    abstract next: item: 'T -> unit

    abstract error: err: exn option -> unit

    abstract complete: unit -> unit

    abstract subscribe: observer: StreamSubscriber<'T> -> Subscription<'T>

[<EditorBrowsable(EditorBrowsableState.Never)>] 
type HubRegistration =
    /// Registers a handler that will be invoked when the connection is closed.
    abstract onClose: (exn option -> unit) -> unit

    /// Registers a handler that will be invoked when the connection successfully reconnects.
    abstract onReconnected: (string option -> unit) -> unit

    /// Registers a handler that will be invoked when the connection starts reconnecting.
    abstract onReconnecting: (exn option -> unit) -> unit

[<RequireQualifiedAccess;StringEnum(CaseRules.None)>]
type ConnectionState =
    | Connected
    | Connecting
    | Disconnected
    | Disconnecting
    | Reconnecting

type HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    interface HubRegistration with
        member this.onClose handler = this.onClose handler
        member this.onReconnected handler = this.onReconnected handler
        member this.onReconnecting handler = this.onReconnecting handler
    
    [<Emit("$0.baseUrl()")>]
    member _.baseUrl () : string = jsNative

    [<Emit("$0.connectionId()")>]
    member _.connectionId () : string option = jsNative

    [<Emit("$0.invoke($1...)")>]
    member _.invoke' (methodName: string, [<ParamArray>] args: obj) : JS.Promise<'ServerApi> = jsNative

    /// Invokes a hub method on the server using the specified name and arguments.
    /// 
    /// The Promise returned by this method resolves when the server indicates it has finished invoking the method. When the promise
    /// resolves, the server has finished invoking the method. If the server method returns a result, it is produced as the result of
    /// resolving the Promise.
    member inline this.invoke (msg: 'ClientApi) : Async<'ServerApi> = this.invoke'("Send", msg) |> Async.AwaitPromise
    
    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    [<Emit("$0.keepAliveIntervalInMilliseconds")>]
    member _.keepAliveIntervalInMilliseconds : int = jsNative
    
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.off($1)")>]
    member _.off' (methodName: string) : unit = jsNative
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.off($1, $2)")>]
    member _.off' (methodName: string, handler: 'ServerApi -> unit) : unit = jsNative

    /// Removes all handlers for the specified hub method.
    member inline this.off () = this.off'("Send")
    
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.on($1, $2)")>]
    member _.on<'T> (methodName: string, handler: 'T -> unit) : unit = jsNative

    /// Registers a handler that will be invoked when the connection is closed.
    [<Emit("$0.onclose($1)")>]
    member _.onClose (callback: (exn option -> unit)) : unit = jsNative

    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    member inline this.onMsg (callback: 'ServerApi -> unit) = this.on<'ServerApi>("Send", callback)
    
    /// Registers a handler that will be invoked when the connection starts reconnecting.
    [<Emit("$0.onreconnecting($1)")>]
    member _.onReconnecting (callback: (exn option -> unit)) : unit = jsNative
    
    /// Registers a handler that will be invoked when the connection successfully reconnects.
    [<Emit("$0.onreconnected($1)")>]
    member _.onReconnected (callback: (string option -> unit)) : unit = jsNative

    member inline this.onStreamFrom (callback: StreamResult<'ServerStreamApi> -> unit) = 
        this.on<StreamResult<'ServerStreamApi>>("StreamFrom", callback)
    
    member inline this.onStreamTo (callback: unit -> unit) = 
        this.on<unit>("StreamTo", callback)

    [<Emit("$0.send($1...)")>]
    member _.send' (methodName: string, [<ParamArray>] args: ResizeArray<obj>) : JS.Promise<unit> = jsNative

    /// Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
    /// 
    /// The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
    /// be processing the invocation.
    member inline this.send (msg: 'ClientApi) = this.send'("Send", ResizeArray [| msg :> obj |]) |> Async.AwaitPromise

    /// Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
    /// 
    /// The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
    /// be processing the invocation.
    member inline this.sendNow (msg: 'ClientApi) = this.send'("Send", ResizeArray [| msg :> obj |]) |> Promise.start

    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    [<Emit("$0.serverTimeoutInMilliseconds")>]
    member _.serverTimeoutInMilliseconds : int = jsNative

    /// Starts the connection.
    [<Emit("$0.start()")>]
    member _.startAsPromise () : JS.Promise<unit> = jsNative
    
    member inline this.start () = this.startAsPromise() |> Async.AwaitPromise

    /// Starts the connection immediately.
    member inline this.startNow () = this.startAsPromise() |> Promise.start

    /// Indicates the state of the HubConnection to the server.
    [<Emit("$0.state()")>]
    member _.state () = jsNative

    /// Stops the connection.
    [<Emit("$0.stop()")>]
    member _.stop' () : JS.Promise<unit> = jsNative
    
    member inline this.stop () = this.stop'() |> Async.AwaitPromise

    /// Stops the connection immediately.
    member inline this.stopNow () = this.stop'() |> Promise.start
    
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.stream($1...)")>]
    member _.stream (methodName: string, [<ParamArray>] args: ResizeArray<obj>) : StreamResult<'ServerStreamApi> = jsNative

    /// Streams from the hub.
    member inline this.streamFrom (msg: 'ClientStreamFromApi) : StreamResult<'ServerStreamApi> = this.stream("StreamFrom", ResizeArray [| msg :> obj |])
    
    /// Streams to the hub.
    member inline this.streamTo (subject: Subject<'ClientStreamToApi>) = this.send'("StreamTo", ResizeArray [| subject :> obj |]) |> Async.AwaitPromise

    /// Streams to the hub and starts the computation.
    member inline this.streamToNow (subject: Subject<'ClientStreamToApi>) = this.send'("StreamTo", ResizeArray [| subject :> obj |]) |> Promise.start
    
type internal IHubConnectionBuilder<'ClientApi,'ServerApi> =
    abstract configureLogging: logLevel: LogLevel -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract configureLogging: logger: ILogger -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract configureLogging: logLevel: string -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withUrl: url: string -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withUrl: url: string * transportType: TransportType -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withUrl: url: string * options: Http.ConnectionOptions -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withHubProtocol : protocol: IHubProtocol<'ClientStreamApi,'ServerApi,'ServerStreamApi> -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract withAutomaticReconnect: unit -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withAutomaticReconnect: retryDelays: seq<int> -> IHubConnectionBuilder<'ClientApi,'ServerApi>
    abstract withAutomaticReconnect: reconnectPolicy: RetryPolicy -> IHubConnectionBuilder<'ClientApi,'ServerApi>

    abstract build: unit -> HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>
