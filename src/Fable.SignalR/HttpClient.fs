namespace Fable.SignalR

open Fable.Core
open System.ComponentModel

[<RequireQualifiedAccess>]
module Http =
    [<StringEnum(CaseRules.LowerFirst);RequireQualifiedAccess>]
    type XMLHttpRequestResponseType =
        | [<CompiledName("")>] None
        | Arraybuffer
        | Blob
        | Document
        | Json
        | Text

    [<StringEnum(CaseRules.None)>]
    type Method =
        | GET
        | POST
        | PUT
        | PATCH
        | DELETE
        | HEAD
        | OPTIONS
        
    type Request =
        /// The HTTP method to use for the request.
        abstract method: Method option

        /// The URL for the request.
        abstract url: string option

        /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
        abstract content: U2<string, JS.ArrayBuffer> option

        /// An object describing headers to apply to the request.
        abstract headers: Map<string,string> option

        /// The XMLHttpRequestResponseType to apply to the request.
        abstract responseType: XMLHttpRequestResponseType option

        /// An AbortSignal that can be monitored for cancellation.
        abstract abortSignal: AbortSignal option

        /// The time to wait for the request to complete before throwing a TimeoutError. Measured in milliseconds.
        abstract timeout: int option

        /// This controls whether credentials such as cookies are sent in cross-site requests.
        abstract withCredentials: bool option

    type IRequestOptionsRecord =
        { method: Method option
          url: string option
          content: U2<string, JS.ArrayBuffer> option
          headers: Map<string,string> option
          responseType: XMLHttpRequestResponseType option
          abortSignal: AbortSignal option
          timeout: int option
          withCredentials: bool option }

    type RequestOptions [<EditorBrowsable(EditorBrowsableState.Never)>] (state: IRequestOptionsRecord) =
        /// The HTTP method to use for the request.
        member _.method (value: Method) =
            { state with method = Some value }
            |> RequestOptions

        /// The URL for the request.
        member _.url (value: string) =
            { state with url = Some value }
            |> RequestOptions

        /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
        member _.content (body: string) =
            { state with content = Some(U2.Case1(body)) }
            |> RequestOptions
        /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
        member _.content (body: JS.ArrayBuffer) =
            { state with content = Some(U2.Case2(body)) }
            |> RequestOptions

        /// An object describing headers to apply to the request.
        member _.headers (value: Map<string,string>) =
            { state with headers = Some value }
            |> RequestOptions

        /// The XMLHttpRequestResponseType to apply to the request.
        member _.responseType (value: XMLHttpRequestResponseType) =
            { state with responseType = Some value }
            |> RequestOptions

        /// An AbortSignal that can be monitored for cancellation.
        member _.abortSignal (signal: AbortSignal) =
            { state with abortSignal = Some signal }
            |> RequestOptions

        /// The time to wait for the request to complete before throwing a TimeoutError. Measured in milliseconds.
        member _.timeout (value: int) = 
            { state with timeout = Some value }
            |> RequestOptions

        /// This controls whether credentials such as cookies are sent in cross-site requests.
        member _.withCredentials (value: bool) =
            { state with withCredentials = Some value }
            |> RequestOptions

        member _.build () =
            unbox<Request> state

        static member Create () =
            { method = None
              url = None
              content = None
              headers = None
              responseType = None
              abortSignal = None
              timeout = None
              withCredentials = None }
            |> RequestOptions

    type Response = 
        { statusCode: int
          statusText: string option
          content : U2<string,JS.ArrayBuffer> }

    /// Abstraction over an HTTP client.
    /// 
    /// This class provides an abstraction over an HTTP client so that a different implementation can be provided on different platforms.
    type Client =
        /// <summary>Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        abstract get: url: string -> JS.Promise<Response>
        /// <summary>Issues an HTTP GET request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="options">Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.</param>
        abstract get: url: string * options: Request -> JS.Promise<Response>

        /// <summary>Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        abstract post: url: string -> JS.Promise<Response>
        /// <summary>Issues an HTTP POST request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="options">Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.</param>
        abstract post: url: string * options: Request -> JS.Promise<Response>

        /// <summary>Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        abstract delete: url: string -> JS.Promise<Response>
        /// <summary>Issues an HTTP DELETE request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="url">The URL for the request.</param>
        /// <param name="options">Additional options to configure the request. The 'url' field in this object will be overridden by the url parameter.</param>
        abstract delete: url: string * options: Request -> JS.Promise<Response>

        /// <summary>Issues an HTTP request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.</summary>
        /// <param name="request">An {@link</param>
        abstract send: request: Request -> JS.Promise<Response>

        /// <summary>Gets all cookies that apply to the specified URL.</summary>
        /// <param name="url">The URL that the cookies are valid for.</param>
        abstract getCookieString: url: string -> string

    type DefaultClient =
        inherit Client

        /// Issues an HTTP request to the specified URL, returning a Promise that resolves with an HttpResponse representing the result.
        abstract send: request: Request -> JS.Promise<Response>

        /// Gets all cookies that apply to the specified URL.
        abstract getCookieString: url: string -> string

    [<EditorBrowsable(EditorBrowsableState.Never)>]
    type IConnectionOptions =
        abstract headers: Map<string,string> option with get, set
        abstract httpClient: Client option with get, set
        abstract transport: TransportType option with get, set
        abstract logger: U2<ILogger, LogLevel> option with get, set
        abstract accessTokenFactory: (unit -> U2<string, JS.Promise<string>>) option with get, set
        abstract logMessageContent: bool option with get, set
        abstract skipNegotiation: bool option with get, set
        abstract withCredentials: bool option with get, set

    type IConnectionOptionsRecord =
        { headers: Map<string,string> option
          httpClient: Client option
          transport: TransportType option
          logger: U2<ILogger,LogLevel> option
          accessTokenFactory: (unit -> U2<string, JS.Promise<string>>) option
          logMessageContent: bool option
          skipNegotiation: bool option
          withCredentials: bool option }

    type ConnectionOptions [<EditorBrowsable(EditorBrowsableState.Never)>] (state: IConnectionOptionsRecord) =
        /// Custom headers to be sent with every HTTP request. Note, setting headers in the browser will not work for WebSockets or the ServerSentEvents stream.
        member _.header (headers: Map<string,string>) =
            { state with headers = Some headers }
            |> ConnectionOptions

        /// An HttpClient that will be used to make HTTP requests.
        member _.httpClient (client: Client) = 
            { state with httpClient = Some client }
            |> ConnectionOptions

        /// An HttpTransportType value specifying the transport to use for the connection.
        member _.transport (transportType: TransportType) =
            { state with transport = Some transportType }
            |> ConnectionOptions

        /// Configures the logger used for logging.
        /// 
        /// Provide an ILogger instance, and log messages will be logged via that instance. Alternatively, provide a value from
        /// the LogLevel enumeration and a default logger which logs to the Console will be configured to log messages of the specified
        /// level (or higher).
        member _.logger (logger: ILogger) =
            { state with logger = Some (U2.Case1(logger)) }
            |> ConnectionOptions
        /// Configures the logger used for logging.
        /// 
        /// Provide an ILogger instance, and log messages will be logged via that instance. Alternatively, provide a value from
        /// the LogLevel enumeration and a default logger which logs to the Console will be configured to log messages of the specified
        /// level (or higher).
        member _.logger (logLevel: LogLevel) =
            { state with logger = Some (U2.Case2(logLevel)) }
            |> ConnectionOptions

        /// A function that provides an access token required for HTTP Bearer authentication.
        member _.accessTokenFactory (factory: unit -> string) =
            { state with accessTokenFactory = Some (factory >> U2.Case1) }
            |> ConnectionOptions
        /// A function that provides an access token required for HTTP Bearer authentication.
        member _.accessTokenFactory (factory: unit -> JS.Promise<string>) =
            { state with accessTokenFactory = Some (factory >> U2.Case2) }
            |> ConnectionOptions
        /// A function that provides an access token required for HTTP Bearer authentication.
        member _.accessTokenFactory (factory: unit -> Async<string>) = 
            { state with accessTokenFactory = Some (factory >> Async.StartAsPromise >> U2.Case2) }
            |> ConnectionOptions

        /// A boolean indicating if message content should be logged.
        /// 
        /// Message content can contain sensitive user data, so this is disabled by default.
        member _.logMessageContent (value: bool) =
            { state with logMessageContent = Some value }
            |> ConnectionOptions

        /// A boolean indicating if negotiation should be skipped.
        /// 
        /// Negotiation can only be skipped when the IHttpConnectionOptions.transport property is set to 'HttpTransportType.WebSockets'.
        member _.skipNegotiation (value: bool) =
            { state with skipNegotiation = Some value }
            |> ConnectionOptions

        /// Default value is 'true'.
        /// This controls whether credentials such as cookies are sent in cross-site requests.
        /// 
        /// Cookies are used by many load-balancers for sticky sessions which is required when your app is deployed with multiple servers.
        member _.withCredentials (value: bool) =
            { state with withCredentials = Some value }
            |> ConnectionOptions

        [<EditorBrowsable(EditorBrowsableState.Never)>]
        static member Create () =
            { headers = None
              httpClient = None 
              transport = None
              logger = None
              accessTokenFactory = None
              logMessageContent = None
              skipNegotiation = None
              withCredentials = None }
            |> ConnectionOptions
