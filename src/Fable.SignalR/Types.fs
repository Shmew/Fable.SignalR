namespace Fable.SignalR

/// Represents a signal that can be monitored to 
/// determine if a request has been aborted.
type AbortSignal =
    /// Indicates if the request has been aborted.
    abstract aborted: bool

    /// Set this to a handler that will be invoked 
    /// when the request is aborted.
    abstract onabort: unit -> unit

[<RequireQualifiedAccess>]
type LogLevel =
    | Trace = 0
    | Debug = 1
    | Information = 2
    | Warning = 3
    | Error = 4
    | Critical = 5
    | None = 6

/// An abstraction that provides a sink for diagnostic messages.
type ILogger =
    /// <summary>Called by the framework to emit a diagnostic message.</summary>
    /// <param name="logLevel">The severity level of the message.</param>
    /// <param name="message">The message.</param>
    abstract log: logLevel: LogLevel * message: string -> unit

/// A logger that does nothing when log messages are sent to it.
type NullLogger =
    inherit ILogger

    abstract log: logLevel: LogLevel * message: string -> unit

[<RequireQualifiedAccess>] 
type TransportType =
    | None = 0
    | WebSockets = 1
    | ServerSentEvents = 2
    | LongPolling = 4

[<RequireQualifiedAccess>] 
type TransferFormat =
    | Text = 1
    | Binary = 2

type RetryContext =
    /// The number of consecutive failed tries so far.
    abstract previousRetryCount: int

    /// The amount of time in milliseconds spent retrying so far.
    abstract elapsedMilliseconds: int

    /// The error that forced the upcoming retry.
    abstract retryReason: exn

/// An abstraction that controls when the client attempts to reconnect and how many times it does so.
type IRetryPolicy =
    /// <summary>Called after the transport loses the connection.</summary>
    /// <param name="retryContext">Details related to the retry event to help determine how long to wait for the next retry.</param>
    abstract nextRetryDelayInMilliseconds: retryContext: RetryContext -> int option

/// Defines the expected type for a receiver of results streamed by the server.
type IStreamSubscriber<'T,'Error> =
    /// A boolean that will be set by the {@link @microsoft/signalr.IStreamResult} when the stream is closed.
    abstract closed: bool option with get, set

    /// Called by the framework when a new item is available.
    abstract next: value: 'T -> unit

    /// Called by the framework when an error has occurred.
    /// 
    /// After this method is called, no additional methods on the IStreamSubscriber will be called.
    abstract error: err: 'Error option -> unit

    /// Called by the framework when the end of the stream is reached.
    /// 
    /// After this method is called, no additional methods on the IStreamSubscriber will be called.
    abstract complete: unit -> unit

/// An interface that allows an IStreamSubscriber to be disconnected from a stream.
type ISubscription<'T> =
    /// Disconnects the IStreamSubscriber associated with this subscription from the stream.
    abstract dispose: unit -> unit

/// Defines the result of a streaming hub method.
type IStreamResult<'T> =
    /// Attaches a IStreamSubscriber, which will be invoked when new items are available from the stream.
    abstract subscribe: subscriber: IStreamSubscriber<'T,'Error> -> ISubscription<'T>
