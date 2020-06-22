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

[<Erase>]
module HubInterfaces =
    open Messages
    
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
        abstract parseMessages : input: U3<string,JS.ArrayBuffer,Buffer> * ?logger: ILogger -> ResizeArray<HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi>>

        /// Writes the specified HubMessage to a string or ArrayBuffer and returns it.
        /// 
        /// If IHubProtocol.transferFormat is 'Text', the result of this method will be a string, otherwise it will be an ArrayBuffer.
        abstract writeMessage: message: HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi> -> U2<string, JS.ArrayBuffer>

    /// Represents a connection to a SignalR Hub.
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type IHubConnection<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
        /// The server timeout in milliseconds.
        /// 
        /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
        /// The default timeout value is 30,000 milliseconds (30 seconds).
        abstract serverTimeoutInMilliseconds: int

        /// Default interval at which to ping the server.
        /// 
        /// The default value is 15,000 milliseconds (15 seconds).
        /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
        abstract keepAliveIntervalInMilliseconds: int

        /// Starts the connection.
        abstract start: unit -> JS.Promise<unit>

        /// Stops the connection.
        abstract stop: unit -> JS.Promise<unit>

        /// Invokes a streaming hub method on the server using the specified name and arguments.
        abstract stream: methodName: string * [<ParamArray>] args: ResizeArray<'ClientStreamApi> -> IStreamResult<'ServerStreamApi>

        /// Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
        /// 
        /// The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
        /// be processing the invocation.
        abstract send: methodName: string * [<ParamArray>] args: ResizeArray<'ClientApi> -> JS.Promise<unit>

        /// Invokes a hub method on the server using the specified name and arguments.
        /// 
        /// The Promise returned by this method resolves when the server indicates it has finished invoking the method. When the promise
        /// resolves, the server has finished invoking the method. If the server method returns a result, it is produced as the result of
        /// resolving the Promise.
        abstract invoke: methodName: string * [<ParamArray>] args: ResizeArray<'ClientApi> -> JS.Promise<'T>

        /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
        abstract on: methodName: string * newMethod: (ResizeArray<'ServerApi> -> unit) -> unit

        /// Removes all handlers for the specified hub method.
        abstract off: methodName: string -> unit
        /// Removes the specified handler for the specified hub method.
        /// 
        /// You must pass the exact same Function instance as was previously passed to HubConnection.on. Passing a different instance (even if the function
        /// body is the same) will not remove the handler.
        abstract off: methodName: string * method: (ResizeArray<'ServerApi option> -> unit) -> unit

        /// Registers a handler that will be invoked when the connection is closed.
        abstract onclose: callback: (exn option -> unit) -> unit

        /// Registers a handler that will be invoked when the connection starts reconnecting.
        abstract onreconnecting: callback: (exn option -> unit) -> unit

        /// Registers a handler that will be invoked when the connection successfully reconnects.
        abstract onreconnected: callback: (string option -> unit) -> unit

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> =
        abstract configureLogging: logLevel: LogLevel -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract configureLogging: logger: ILogger -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract configureLogging: logLevel: string -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

        abstract withUrl: url: string -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract withUrl: url: string * transportType: TransportType -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract withUrl: url: string * options: Http.IConnectionOptions -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

        abstract withHubProtocol : protocol: IHubProtocol<'ClientStreamApi,'ServerApi,'ServerStreamApi> -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

        abstract withAutomaticReconnect: unit -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract withAutomaticReconnect: retryDelays: seq<int> -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
        abstract withAutomaticReconnect: reconnectPolicy: IRetryPolicy -> IHubConnectionBuilder<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

        abstract build: unit -> IHubConnection<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>

open HubInterfaces

type HubRegistration =
    /// Registers a handler that will be invoked when the connection is closed.
    abstract onClose: (exn option -> unit) -> unit

    /// Registers a handler that will be invoked when the connection successfully reconnects.
    abstract onReconnected: (string option -> unit) -> unit

    /// Registers a handler that will be invoked when the connection starts reconnecting.
    abstract onReconnecting: (exn option -> unit) -> unit

type HubConnection<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> [<EditorBrowsable(EditorBrowsableState.Never)>] 
    (hub: IHubConnection<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>) =

    interface HubRegistration with
        member this.onClose handler = this.onclose handler
        member this.onReconnected handler = this.onreconnected handler
        member this.onReconnecting handler = this.onreconnecting handler

    /// The server timeout in milliseconds.
    /// 
    /// If this timeout elapses without receiving any messages from the server, the connection will be terminated with an error.
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    member _.serverTimeoutInMilliseconds = hub.serverTimeoutInMilliseconds
    
    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    member _.keepAliveIntervalInMilliseconds = hub.keepAliveIntervalInMilliseconds
    
    /// Starts the connection.
    member _.start () = hub.start() |> Async.AwaitPromise
    
    /// Starts the connection immediately.
    member _.startNow () = hub.start() |> Promise.start

    /// Stops the connection.
    member _.stop () = hub.stop() |> Async.AwaitPromise
    
    /// Stops the connection immediately.
    member _.stopNow () = hub.stop() |> Promise.start

    /// Invokes a streaming hub method on the server using the specified name and arguments.
    member _.stream (msg: 'ClientStreamApi) = hub.stream("Stream", ResizeArray [| msg |])
    
    /// Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
    /// 
    /// The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
    /// be processing the invocation.
    member _.send (msg: 'ClientApi) = hub.send("Send", ResizeArray [| msg |]) |> Async.AwaitPromise
    /// Invokes a hub method on the server using the specified name and arguments. Does not wait for a response from the receiver.
    /// 
    /// The Promise returned by this method resolves when the client has sent the invocation to the server. The server may still
    /// be processing the invocation.
    member inline this.sendNow (msg: 'ClientApi) = this.send(msg) |> Async.StartImmediate
    
    /// Invokes a hub method on the server using the specified name and arguments.
    /// 
    /// The Promise returned by this method resolves when the server indicates it has finished invoking the method. When the promise
    /// resolves, the server has finished invoking the method. If the server method returns a result, it is produced as the result of
    /// resolving the Promise.
    member _.invoke (msg: 'ClientApi) : Async<'ServerApi> = hub.invoke("Send", ResizeArray [| msg |]) |> Async.AwaitPromise
    
    /// Registers a handler that will be invoked when the hub method with the specified method name is invoked.
    member _.onMsg (callback: 'ServerApi -> unit) = hub.on("Send", fun raw -> raw |> unbox<'ServerApi> |> callback)
    
    /// Removes all handlers for the specified hub method.
    member _.off () = hub.off("Send")
    
    /// Registers a handler that will be invoked when the connection is closed.
    member _.onclose (callback: (exn option -> unit)) = hub.onclose(callback)
    
    /// Registers a handler that will be invoked when the connection starts reconnecting.
    member _.onreconnecting (callback: (exn option -> unit)) = hub.onreconnecting(callback)
    
    /// Registers a handler that will be invoked when the connection successfully reconnects.
    member _.onreconnected (callback: (string option -> unit)) = hub.onreconnected(callback)

/// Stream implementation to stream items to the server.
type Subject<'T,'Error> =
    inherit IStreamResult<'T>

    abstract observers: ResizeArray<IStreamSubscriber<'T,'Error>> with get, set

    abstract cancelCallback: (unit -> JS.Promise<unit>) option with get, set

    abstract next: item: 'T -> unit

    abstract error: err: 'Error option -> unit

    abstract complete: unit -> unit

    abstract subscribe: observer: IStreamSubscriber<'T,'Error> -> ISubscription<'T>
