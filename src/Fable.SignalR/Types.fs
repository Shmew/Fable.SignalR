namespace Fable.SignalR

open Fable.Core
open System.ComponentModel

/// Represents a signal that can be monitored to 
/// determine if a request has been aborted.
type AbortSignal =
    /// Indicates if the request has been aborted.
    abstract aborted: bool

    /// Set this to a handler that will be invoked 
    /// when the request is aborted.
    abstract onAbort: (unit -> unit)

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
    /// Called by the framework to emit a diagnostic message.
    abstract log: logLevel: LogLevel -> message: string -> unit

/// A logger that does nothing when log messages are sent to it.
[<Erase>]
type NullLogger =
    interface ILogger with
        member this.log logLevel message = this.log logLevel message

    [<Emit("$0.log($1, $2)")>]
    member _.log (logLevel: LogLevel) (message: string) : unit = jsNative

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
    { /// The number of consecutive failed tries so far.
      previousRetryCount: int
      /// The amount of time in milliseconds spent retrying so far.
      elapsedMilliseconds: int
      /// The error that forced the upcoming retry.
      retryReason: exn }

/// Controls when the client attempts to reconnect and how many times it does so.
type RetryPolicy =
    { /// Called after the transport loses the connection.
      ///
      /// retryContext - Details related to the retry event to help determine how 
      /// long to wait for the next retry.
      nextRetryDelayInMilliseconds: RetryContext -> int option }

/// Interface to observe a stream.
type IStreamSubscriber<'T> =
    /// Sends a new item to the server.
    abstract next: value: 'T -> unit
    /// Sends an error to the server.
    abstract error: exn option -> unit
    /// Completes the stream.
    abstract complete: unit -> unit

/// Used to observe a stream.
type StreamSubscriber<'T> =
    { /// Sends a new item to the server.
      next: 'T -> unit
      /// Sends an error to the server.
      error: exn option -> unit
      /// Completes the stream.
      complete: unit -> unit }

    /// Casts this StreamSubscriber to an IStreamSubscriber.
    member inline this.cast () =
        unbox<IStreamSubscriber<'T>>(this)
    
    /// Casts a StreamSubscriber to an IStreamSubscriber.
    static member inline cast (subscriber: StreamSubscriber<'T>) =
        subscriber.cast()

[<EditorBrowsable(EditorBrowsableState.Never)>]
type ISubscription =
    abstract dispose: unit -> unit

/// Allows attaching a subscribr to a stream.
[<Erase>]
type StreamResult<'T> =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.subscribe($1)")>]
    member _.subscribe' (subscriber: IStreamSubscriber<'T>) : ISubscription = jsNative
    
    /// Attaches an IStreamSubscriber, which will be invoked when new items are 
    /// available from the stream.
    member inline this.subscribe (subscriber: IStreamSubscriber<'T>) =
        this.subscribe'(subscriber)
        |> fun sub -> { new System.IDisposable with member _.Dispose () = sub.dispose() }
            
    /// Attaches a StreamSubscriber, which will be invoked when new items are 
    /// available from the stream.
    member inline this.subscribe (subscriber: StreamSubscriber<'T>) =
        this.subscribe(unbox<IStreamSubscriber<'T>> subscriber)

    /// Attaches a StreamSubscriber, which will be invoked when new items are 
    /// available from the stream.
    static member inline subscribe (subscriber: IStreamSubscriber<'T>) =
        fun (streamRes: StreamResult<'T>) -> streamRes.subscribe(subscriber)
    /// Attaches a StreamSubscriber, which will be invoked when new items are 
    /// available from the stream.
    static member inline subscribe (subscriber: StreamSubscriber<'T>) =
        fun (streamRes: StreamResult<'T>) -> streamRes.subscribe(subscriber)
