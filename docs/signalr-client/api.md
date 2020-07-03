# API Reference

## AbortSignal

Represents a signal that can be monitored to 
determine if a request has been aborted.

Signature:
```fsharp
type AbortSignal =
    /// Indicates if the request has been aborted.
    abstract aborted: bool

    /// Set  to a handler that will be invoked 
    /// when the request is aborted.
    abstract onAbort: (unit -> unit)
```

## LogLevel

Signature:
```fsharp
type LogLevel =
    | Trace = 0
    | Debug = 1
    | Information = 2
    | Warning = 3
    | Error = 4
    | Critical = 5
    | None = 6
```

## ILogger

An abstraction that provides a sink for diagnostic messages.

Signature:
```fsharp
type ILogger =
    /// Called by the framework to emit a diagnostic message.
    abstract log: logLevel: LogLevel -> message: string -> unit
```

## NullLogger

A logger that does nothing when log messages are sent to it.

Signature:
```fsharp
type NullLogger =
    interface ILogger

    member log (logLevel: LogLevel) (message: string) : unit
```

## TransportType

Signature:
```fsharp
type TransportType =
    | None = 0
    | WebSockets = 1
    | ServerSentEvents = 2
    | LongPolling = 4
```

## TransferFormat

Signature:
```fsharp
type TransferFormat =
    | Text = 1
    | Binary = 2
```

## RetryContext

Signature:
```fsharp
type RetryContext =
    { /// The number of consecutive failed tries so far.
      previousRetryCount: int
      /// The amount of time in milliseconds spent retrying so far.
      elapsedMilliseconds: int
      /// The error that forced the upcoming retry.
      retryReason: exn }
```

## RetryPolicy

Controls when the client attempts to reconnect and how many times it does so.

Signature:
```fsharp
type RetryPolicy =
    { /// Called after the transport loses the connection.
      ///
      /// retryContext - Details related to the retry event to help determine how 
      /// long to wait for the next retry.
      nextRetryDelayInMilliseconds: RetryContext -> int option }
```

## IStreamSubscriber

Interface to observe a stream.

Signature:
```fsharp
type IStreamSubscriber<'T> =
    /// Sends a new item to the server.
    abstract next: value: 'T -> unit
    /// Sends an error to the server.
    abstract error: exn option -> unit
    /// Completes the stream.
    abstract complete: unit -> unit
```

## StreamSubscriber

Used to observe a stream.

Signature:
```fsharp
type StreamSubscriber<'T> =
    { /// Sends a new item to the server.
      next: 'T -> unit
      /// Sends an error to the server.
      error: exn option -> unit
      /// Completes the stream.
      complete: unit -> unit }

    /// Casts  StreamSubscriber to an IStreamSubscriber.
    member cast () : IStreamSubscriber<'T>
```

### StreamSubscriber.cast

Casts a StreamSubscriber to an IStreamSubscriber.

Signature:
```fsharp
StreamSubscriber<'T> -> IStreamSubscriber<'T>
```

## StreamResult

Allows attaching a subscribr to a stream.

Signature:
```fsharp
type StreamResult<'T> =
    /// Attaches an IStreamSubscriber, which will be invoked when new items are 
    /// available from the stream.
    member subscribe (subscriber: IStreamSubscriber<'T>) : System.IDisposable
    member subscribe (subscriber: StreamSubscriber<'T>) : System.IDisposable
```

### StreamResult.subscribe

Attaches a StreamSubscriber, which will be invoked when new items are 
available from the stream.

Signature:
```fsharp
IStreamSubscriber<'T> -> System.IDisposable
StreamSubscriber<'T> -> System.IDisposable
```

## IHubProtocol

A protocol abstraction for communicating with SignalR Hubs.

Signature:
```fsharp
type IHubProtocol<'ClientStreamApi,'ServerApi,'ServerStreamApi> =
    /// The name of the protocol.  is used by SignalR to resolve the protocol between the client 
    /// and server.
    abstract name: string

    /// The version of the protocol.
    abstract version: float

    /// The TransferFormat of the protocol.
    abstract transferFormat: TransferFormat

    /// Creates an array of HubMessage objects from the specified serialized representation.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the `input` parameter must be a string, otherwise 
    /// it must be an ArrayBuffer.
    abstract parseMessages : input: U3<string,JS.ArrayBuffer,Buffer> * ?logger: ILogger 
        -> ResizeArray<Messages.HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi>>

    /// Writes the specified HubMessage to a string or ArrayBuffer and returns it.
    /// 
    /// If IHubProtocol.transferFormat is 'Text', the result of  method will be a string, 
    /// otherwise it will be an ArrayBuffer.
    abstract writeMessage: 
        message: Messages.HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi> 
            -> U2<string, JS.ArrayBuffer>
```

## ISubject

A stream interface to stream items to the server.

Signature:
```fsharp
type ISubject<'T> =
    abstract next: item: 'T -> unit

    abstract error: err: exn -> unit

    abstract complete: unit -> unit

    abstract subscribe: observer: #IStreamSubscriber<'T> -> System.IDisposable
```

## Subject

Stream implementation to stream items to the server.

Signature:
```fsharp
type Subject<'T> =
    interface ISubject<'T>

    member next (item: 'T) : unit
    member error (err: exn) : unit
    member complete () : unit
    member subscribe (observer: #IStreamSubscriber<'T>) : System.IDisposable
    member subscribe (observer: StreamSubscriber<'T>) : System.IDisposable
```

## ConnectionState

The connection state to the hub.

Signature:
```fsharp
type ConnectionState =
    | Connected
    | Connecting
    | Disconnected
    | Disconnecting
    | Reconnecting
```

## HubConnection

Signature:
```fsharp
type HubConnection<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    /// The base url of the hub connection.
    member baseUrl : string
    
    /// The connectionId to the hub of  client.
    member connectionId : string option
    
    /// Invokes a hub method on the server.
    /// 
    /// The async returned by  method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the async call.
    member invoke (msg: 'ClientApi) : Async<'ServerApi>
    
    /// Invokes a hub method on the server.
    /// 
    /// The promise returned by  method resolves when the server indicates it has finished invoking 
    /// the method. When it finishes, the server has finished invoking the method. If the server 
    /// method returns a result, it is produced as the result of resolving the promise.
    member invokeAsPromise (msg: 'ClientApi) : JS.Promise<'ServerApi>

    /// Default interval at which to ping the server.
    /// 
    /// The default value is 15,000 milliseconds (15 seconds).
    /// Allows the server to detect hard disconnects (like when a client unplugs their computer).
    member keepAliveInterval : int
    
    /// Removes all handlers.
    member off () : unit
    
    /// Registers a handler that will be invoked when the connection is closed.
    member onClose (callback: (exn option -> unit)) : unit

    /// Callback when a new message is recieved.
    member onMessage (callback: 'ServerApi -> unit) : unit
    
    /// Callback when the connection successfully reconnects.
    member onReconnected (callback: (string option -> unit)) : unit

    /// Callback when the connection starts reconnecting.
    member onReconnecting (callback: (exn option -> unit)) : unit

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The async returned by  method resolves when the client has sent the invocation to the 
    /// server. The server may still be processing the invocation.
    member send (msg: 'ClientApi) : Async<unit>

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    /// 
    /// The promise returned by  method resolves when the client has sent the invocation to the 
    /// server. The server may still be processing the invocation.
    member sendAsPromise (msg: 'ClientApi) : JS.Promise<unit>

    /// Invokes a hub method on the server. Does not wait for a response from the receiver.
    member sendNow (msg: 'ClientApi) : unit

    /// The server timeout in milliseconds.
    /// 
    /// If  timeout elapses without receiving any messages from the server, the connection will be 
    /// terminated with an error.
    ///
    /// The default timeout value is 30,000 milliseconds (30 seconds).
    member serverTimeout : int
    
    /// Starts the connection.
    member start () : Async<unit>

    /// Starts the connection.
    member startAsPromise () : JS.Promise<unit>

    /// Starts the connection immediately.
    member startNow () : unit

    /// The state of the hub connection to the server.
    member state : ConnectionState

    /// Stops the connection.
    member stop () : Async<unit>

    /// Stops the connection.
    member stopAsPromise () : JS.Promise<unit>

    /// Stops the connection immediately.
    member stopNow () : unit

    /// Streams from the hub.
    member streamFrom (msg: 'ClientStreamFromApi) : Async<StreamResult<'ServerStreamApi>>
    
    /// Streams from the hub.
    member streamFromAsPromise (msg: 'ClientStreamFromApi) : Async<StreamResult<'ServerStreamApi>>

    /// Returns an async that when invoked, starts streaming to the hub.
    member streamTo (subject: ISubject<'ClientStreamToApi>) : Async<unit>

    /// Returns a promise that when invoked, starts streaming to the hub.
    member inline streamToAsPromise (subject: ISubject<'ClientStreamToApi>) : Promise<unit>

    /// Streams to the hub immediately.
    member streamToNow (subject: ISubject<'ClientStreamToApi>) : unit
```

## HubConnectionBuilder

A builder for configuring HubConnection instances.

Signature:
```fsharp
type HubConnectionBuilder
    <'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> =
    
    /// Configures console logging for the HubConnection.
    member configureLogging (logLevel: LogLevel) : HubConnectionBuilder
    member configureLogging (logger: ILogger) : HubConnectionBuilder
    member configureLogging (logLevel: string) : HubConnectionBuilder
        
    /// Configures the HubConnection to use HTTP-based transports to connect 
    /// to the specified URL.
    /// 
    /// The transport will be selected automatically based on what the server 
    /// and client support.
    member withUrl (url: string) : HubConnectionBuilder
        
    /// Configures the HubConnection to use the specified HTTP-based transport
    /// to connect to the specified URL.
    member withUrl (url: string, transportType: TransportType) : HubConnectionBuilder
        
    /// Configures the HubConnection to use HTTP-based transports to connect to
    /// the specified URL.
    member withUrl (url: string, options: Http.ConnectionBuilder -> Http.ConnectionBuilder) 
        : HubConnectionBuilder
        
    /// Configures the HubConnection to use the specified Hub Protocol.
    member withHubProtocol 
        (protocol: IHubProtocol<'ClientStreamFromApi,'ServerApi,'ServerStreamApi>) 
        : HubConnectionBuilder
        
    /// Configures the HubConnection to automatically attempt to reconnect 
    /// if the connection is lost.
    /// 
    /// By default, the client will wait 0, 2, 10 and 30 seconds respectively 
    /// before trying up to 4 reconnect attempts.
    member withAutomaticReconnect () : HubConnectionBuilder
        
    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    /// 
    /// An array containing the delays in milliseconds before trying each reconnect 
    /// attempt. The length of the array represents how many failed reconnect attempts 
    /// it takes before the client will stop attempting to reconnect.
    member withAutomaticReconnect (retryDelays: int list) : HubConnectionBuilder
        
    /// Configures the HubConnection to automatically attempt to reconnect if the 
    /// connection is lost.
    member withAutomaticReconnect (reconnectPolicy: RetryPolicy) : HubConnectionBuilder

    /// Callback when the connection is closed.
    member onClose callback : HubConnectionBuilder
        
    /// Callback when a new message is recieved.
    member onMessage (callback: 'ServerApi -> unit) : HubConnectionBuilder
        
    /// Callback when the connection successfully reconnects.
    member onReconnected (callback: (string option -> unit)) : HubConnectionBuilder
        
    /// Callback when the connection starts reconnecting.
    member onReconnecting (callback: (exn option -> unit)) : HubConnectionBuilder
```

## SignalR

Main entry point.

The full type restrictions for a [HubConnection](#hubconnection) and [HubConnectionBuilder](#hubconnectionbuilder) are:
 * `'ClientApi` - Sent **to** the server for `send` and `invoke`.
 * `'ClientStreamFromApi` - Sent **to** the server for `streamFrom`.
 * `'ClientStreamToApi` - Sent **to** the server for `streamTo`.
 * `'ServerApi` - Sent **from** the server in response to `send` and `invoke`.
 * `'ServerStreamApi` - Sent **from** the server in response to `streamFrom`.

These are aliased as `<Types>` for readability:

### SignalR.connect`<Types>`

Starts a connection to a SignalR hub.

Signature:
```fsharp
(config: HubConnectionBuilder<Types> -> HubConnectionBuilder<Types>) -> HubConnection<Types> 
```

### SignalR.httpClient

Creates the default http client.

Signature:
```fsharp
ILogger -> Http.DefaultClient
```

### SignalR.logger

Creates an ILogger from a logging function.

Signature:
```fsharp
(handler: LogLevel -> string -> unit) -> ILogger
```

### SignalR.nullLogger

Gets an instance of the NullLogger.

Signature:
```fsharp
unit -> NullLogger
```
### SignalR.subject`<'T>`

Creates a new stream implementation to stream items to the server.

Signature:
```fsharp
unit -> Subject<'T>
```

## Elmish

All of the commands are in the `Cmd.SignalR` namespace.

### baseUrl

Returns the base url of the hub connection.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string -> 'Msg) : Cmd<'Msg>
```

### connectionId

Returns the connectionId to the hub of this client.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: string option -> 'Msg) : Cmd<'Msg>
```

### connect

Starts a connection to a SignalR hub.

Signature:
```fsharp
(registerHub: Elmish.Hub<'ClientApi,'ServerApi> -> 'Msg)
(config: Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg> 
    -> Elmish.HubConnectionBuilder<'ClientApi,unit,unit,'ServerApi,unit,'Msg>) : Cmd<'Msg>
```

### attempt

Invokes a hub method on the server and maps the error.

This method resolves when the server indicates it has finished invoking the method. When it finishes, 
the server has finished invoking the method. If the server method returns a result, it is produced as the result of
resolving the async call.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) 
(msg: 'ClientApi) 
(onError: exn -> 'Msg) : Cmd<'Msg>
```
### either

Invokes a hub method on the server and maps the success or error.

This method resolves when the server indicates it has finished invoking the method. When it finishes, 
the server has finished invoking the method. If the server method returns a result, it is produced as the result of
resolving the async call.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) 
(msg: 'ClientApi) 
(onSuccess: 'ServerApi -> 'Msg) 
(onError: exn -> 'Msg) : Cmd<'Msg>
```

### perform

Invokes a hub method on the server and maps the success.

This method resolves when the server indicates it has finished invoking the method. When it finishes, 
the server has finished invoking the method. If the server method returns a result, it is produced as the result of
resolving the async call.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) 
(msg: 'ClientApi) 
(onSuccess: 'ServerApi -> 'Msg) : Cmd<'Msg>
```

### send

Invokes a hub method on the server. Does not wait for a response from the receiver.

This method resolves when the client has sent the invocation to the server. The server may still
be processing the invocation.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: 'ClientApi) : Cmd<'Msg>
```

### state

Returns the state of the Hub connection to the server.

Signature:
```fsharp
(hub: #Elmish.Hub<'ClientApi,'ServerApi> option) (msg: ConnectionState -> 'Msg) : Cmd<'Msg>
```

### streamFrom

Streams from the hub.

Signature:
```fsharp
(hub: Elmish.StreamHub.ServerToClient
    <'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi> option)
(msg: 'ClientStreamApi) 
(subscription: System.IDisposable -> 'Msg) 
(subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) : Cmd<'Msg>

(hub: Elmish.StreamHub.Bidrectional
    <'ClientApi,'ClientStreamFromApi,_,'ServerApi,'ServerStreamApi> option)
(msg: 'ClientStreamApi) 
(subscription: System.IDisposable -> 'Msg) 
(subscriber: ('Msg -> unit) -> StreamSubscriber<'ServerStreamApi>) : Cmd<'Msg>
```

### streamTo

Streams to the hub.

Signature:
```fsharp
(hub: Elmish.StreamHub.ClientToServer<'ClientApi,'ClientStreamToApi,'ServerApi> option)
(subject: #ISubject<'ClientStreamToApi>) : Cmd<'Msg>

(hub: Elmish.StreamHub.Bidrectional<'ClientApi,_,'ClientStreamToApi,'ServerApi,_> option)
(subject: #ISubject<'ClientStreamToApi>) : Cmd<'Msg>
```

## Feliz

The api exposed from the Feliz extension package is quite simple:

```fsharp
React.useSignalR<Types> (config: HubConnectionBuilder -> HubConnectionBuilder) -> Hub
```

The type of builder depends on the type restrictions given to `useSignalR`:
* No streaming - React.useSignalR<'ClientApi,'ServerApi>
* Client streaming - React.useSignalRReact.useSignalR<'ClientApi,'ClientStreamApi,'ServerApi>
* Server streaming - React.useSignalRReact.useSignalR<'ClientApi,'ClientStreamApi,'ServerApi,'ServerStreamApi>
* Bidirectional streaming - React.useSignalRReact.useSignalR<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi>

## Http

### XMLHttpRequestResponseType

Signature:
```fsharp
type XMLHttpRequestResponseType =
    | None
    | Arraybuffer
    | Blob
    | Document
    | Json
    | Text
```

### Method

Signature:
```fsharp
type Method =
    | GET
    | POST
    | PUT
    | PATCH
    | DELETE
    | HEAD
    | OPTIONS
```

### Request

Settings builder for creating Http requests.

Signature:
```fsharp
type Request =
    /// The HTTP method to use for the request.
    member method (value: Method) : Request

    /// The URL for the request.
    member url (value: string) : Request

    /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
    member content (body: string) : Request
    member content (body: JS.ArrayBuffer) : Request

    /// An object describing headers to apply to the request.
    member headers (value: Map<string,string>) : Request

    /// The XMLHttpRequestResponseType to apply to the request.
    member responseType (value: XMLHttpRequestResponseType) : Request

    /// An AbortSignal that can be monitored for cancellation.
    member abortSignal (signal: AbortSignal) : Request

    /// The time to wait for the request to complete before throwing a TimeoutError. 
    /// 
    /// Measured in milliseconds.
    member timeout (value: int) : Request

    ///  controls whether credentials such as cookies are sent in cross-site requests.
    member withCredentials (value: bool) : Request
```

### Response

A http request response.

Signature:
```fsharp
type Response = 
    { statusCode: int
      statusText: string option
      content : U2<string,JS.ArrayBuffer> }
```

### Client

Abstraction over an HTTP client.

 class provides an abstraction over an HTTP client so that a different 
implementation can be provided on different platforms.

Signature:
```fsharp
type Client =
    /// Issues an HTTP GET request to the specified URL, returning a Promise that 
    /// resolves with an HttpResponse representing the result.
    abstract get: url: string -> JS.Promise<Response>
    abstract get: url: string * options: Request -> JS.Promise<Response>

    /// Issues an HTTP POST request to the specified URL, returning a Promise 
    /// that resolves with an HttpResponse representing the result.
    abstract post: url: string -> JS.Promise<Response>
    abstract post: url: string * options: Request -> JS.Promise<Response>

    /// Issues an HTTP DELETE request to the specified URL, returning a Promise 
    /// that resolves with an HttpResponse representing the result.
    abstract delete: url: string -> JS.Promise<Response>
    abstract delete: url: string * options: Request -> JS.Promise<Response>

    /// Issues an HTTP request to the specified URL, returning a Promise 
    /// that resolves with an HttpResponse representing the result.
    abstract send: request: Request -> JS.Promise<Response>

    ///Gets all cookies that apply to the specified URL.
    abstract getCookieString: url: string -> string
```

### DefaultClient

Signature:
```fsharp
type DefaultClient =
    inherit Client

    /// Issues an HTTP request to the specified URL, returning a Promise 
    /// that resolves with an HttpResponse representing the result.
    abstract send: request: Request -> JS.Promise<Response>
```

### ConnectionBuilder

Configures the SignalR connection.

Signature:
```fsharp
type ConnectionBuilder =
    /// Custom headers to be sent with every HTTP request. Note, setting headers in 
    /// the browser will not work for WebSockets or the ServerSentEvents stream.
    member header (headers: Map<string,string>) : ConnectionBuilder

    /// An HttpClient that will be used to make HTTP requests.
    member httpClient (client: Client) : ConnectionBuilder

    /// An HttpTransportType value specifying the transport to use for the connection.
    member transport (transportType: TransportType) : ConnectionBuilder

    /// Configures the logger used for logging.
    /// 
    /// Provide an ILogger instance, and log messages will be logged via that instance. 
    /// Alternatively, provide a value from the LogLevel enumeration and a default 
    /// logger which logs to the Console will be configured to log messages of the specified
    /// level (or higher).
    member logger (logger: ILogger) : ConnectionBuilder
    member logger (logLevel: LogLevel) : ConnectionBuilder

    /// A function that provides an access token required for HTTP Bearer authentication.
    member accessTokenFactory (factory: unit -> string) : ConnectionBuilder
    member accessTokenFactory (factory: unit -> JS.Promise<string>) : ConnectionBuilder
    member accessTokenFactory (factory: unit -> Async<string>) : ConnectionBuilder

    /// A boolean indicating if message content should be logged.
    /// 
    /// Message content can contain sensitive user data, so  is disabled by default.
    member logMessageContent (value: bool) : ConnectionBuilder

    /// A boolean indicating if negotiation should be skipped.
    /// 
    /// Negotiation can only be skipped when the IHttpConnectionOptions.transport property 
    /// is set to 'HttpTransportType.WebSockets'.
    member skipNegotiation (value: bool) : ConnectionBuilder

    /// Default value is 'true'.
    /// This controls whether credentials such as cookies are sent in cross-site requests.
    /// 
    /// Cookies are used by many load-balancers for sticky sessions which is required when 
    /// your app is deployed with multiple servers.
    member withCredentials (value: bool) : ConnectionBuilder
```

## Messages

These types are only used if defining a custom protocol.

### MessageType

Signature:
```fsharp
type MessageType =
    | Invocation = 1
    | StreamItem = 2
    | Completion = 3
    | StreamInvocation = 4
    | CancelInvocation = 5
    | Ping = 6
    | Close = 7
```

### HubMessageBase

Defines properties common to all Hub messages.

Signature:
```fsharp
type HubMessageBase =
    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType
```

### HubMessageBase

Defines properties common to all Hub messages relating to a specific invocation.

Signature:
```fsharp
type HubInvocationMessage =
    inherit HubMessageBase

    /// A MessageHeaders dictionary containing headers attached to the message.
    abstract headers: Map<string,string> option

    /// The ID of the invocation relating to  message.
    /// 
    ///  is expected to be present for StreamInvocationMessage and CompletionMessage. It may
    /// be 'undefined' for an InvocationMessage if the sender does not expect a response.
    abstract invocationId: string option
```

### InvocationMessage

A hub message representing a non-streaming invocation.

Signature:
```fsharp
type InvocationMessage<'T> =
    inherit HubInvocationMessage

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType

    /// The target method name.
    abstract target: string

    /// The target method arguments.
    abstract arguments: ResizeArray<'T option>

    /// The target methods stream IDs.
    abstract streamIds: ResizeArray<string> option
```

### StreamInvocationMessage

A hub message representing a streaming invocation.

Signature:
```fsharp
type StreamInvocationMessage<'T> =
    inherit HubInvocationMessage

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType

    /// The invocation ID.
    abstract invocationId: string

    /// The target method name.
    abstract target: string

    /// The target method arguments.
    abstract arguments: ResizeArray<'T option>

    /// The target methods stream IDs.
    abstract streamIds: ResizeArray<string> option
```

### StreamItemMessage

A hub message representing a single item produced as part of a result stream.

Signature:
```fsharp
type StreamItemMessage<'T> =
    inherit HubInvocationMessage

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType

    /// The invocation ID.
    abstract invocationId: string

    /// The item produced by the server.
    abstract item: 'T option
```

### CompletionMessage

A hub message representing the result of an invocation.

Signature:
```fsharp
type CompletionMessage<'T> =
    inherit HubInvocationMessage

    /// A MessageType value indicating the type of  message.
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
```

### PingMessage

A hub message indicating that the sender is still active.

Signature:
```fsharp
type PingMessage =
    inherit HubMessageBase

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType
```

### CloseMessage

A hub message indicating that the sender is closing the connection.

If CloseMessage.error is defined, the sender is closing the connection due to an error.

Signature:
```fsharp
type CloseMessage =
    inherit HubMessageBase

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType

    /// The error that triggered the close, if any.
    /// 
    /// If  property is undefined, the connection was closed normally and without error.
    abstract error: string option

    /// If true, clients with automatic reconnects enabled should attempt to reconnect after 
    /// receiving the CloseMessage. Otherwise, they should not.
    abstract allowReconnect: bool option
```

### CancelInvocationMessage

A hub message sent to request that a streaming invocation be canceled.

Signature:
```fsharp
type CancelInvocationMessage =
    inherit HubInvocationMessage

    /// A MessageType value indicating the type of  message.
    abstract ``type``: MessageType

    /// The invocation ID.
    abstract invocationId: string
```

### HubMessage

Signature:
```fsharp
type HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi> =
    U8<InvocationMessage<'ServerApi>,
       InvocationMessage<{| connectionId: string; message: 'ServerApi |}>, 
       StreamItemMessage<'ServerStreamApi>, 
       CompletionMessage<'ServerApi>, 
       StreamInvocationMessage<'ClientStreamApi>, 
       CancelInvocationMessage, 
       PingMessage, 
       CloseMessage>
```
