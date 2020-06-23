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
    /// <summary>Called by the framework to emit a diagnostic message.</summary>
    /// <param name="logLevel">The severity level of the message.</param>
    /// <param name="message">The message.</param>
    [<Emit("$0.log($1, $2)")>]
    abstract log: logLevel: LogLevel -> message: string -> unit

/// A logger that does nothing when log messages are sent to it.
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
      /// retryContext - Details related to the retry event to help determine how long to wait for the next retry.
      nextRetryDelayInMilliseconds: RetryContext -> int option }

/// An interface that allows an IStreamSubscriber to be disconnected from a stream.
type Subscription<'T> =
    [<EditorBrowsable(EditorBrowsableState.Never)>]
    [<Emit("$0.dispose()")>]
    member _.dispose' () : unit = jsNative

type StreamSubscriber<'T> =
    { next: 'T -> unit
      error: exn option -> unit
      complete: unit -> unit }

type StreamResult<'T> =
    /// Attaches a StreamSubscriber, which will be invoked when new items are available from the stream.
    [<Emit("$0.subscribe($1)")>]
    member _.subscribe (subscriber: StreamSubscriber<'T>) : Subscription<'T> = jsNative
