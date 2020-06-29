namespace Fable.SignalR

open Fable.Core

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

    type internal RequestOptionsRecord =
        { method: Method option
          url: string option
          content: U2<string, JS.ArrayBuffer> option
          headers: Map<string,string> option
          responseType: XMLHttpRequestResponseType option
          abortSignal: AbortSignal option
          timeout: int option
          withCredentials: bool option }

    /// Settings builder for creating Http requests.
    type Request internal () =
        let mutable state =
            { method = None
              url = None
              content = None
              headers = None
              responseType = None
              abortSignal = None
              timeout = None
              withCredentials = None }

        /// The HTTP method to use for the request.
        member this.method (value: Method) =
            state <- { state with method = Some value }
            this

        /// The URL for the request.
        member this.url (value: string) =
            state <- { state with url = Some value }
            this

        /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
        member this.content (body: string) =
            state <- { state with content = Some(U2.Case1(body)) }
            this
        /// The body content for the request. May be a string or an ArrayBuffer (for binary data).
        member this.content (body: JS.ArrayBuffer) =
            state <- { state with content = Some(U2.Case2(body)) }
            this

        /// An object describing headers to apply to the request.
        member this.headers (value: Map<string,string>) =
            state <- { state with headers = Some value }
            this

        /// The XMLHttpRequestResponseType to apply to the request.
        member this.responseType (value: XMLHttpRequestResponseType) =
            state <- { state with responseType = Some value }
            this

        /// An AbortSignal that can be monitored for cancellation.
        member this.abortSignal (signal: AbortSignal) =
            state <- { state with abortSignal = Some signal }
            this

        /// The time to wait for the request to complete before throwing a TimeoutError. 
        ///
        /// Measured in milliseconds.
        member this.timeout (value: int) = 
            state <- { state with timeout = Some value }
            this

        /// This controls whether credentials such as cookies are sent in cross-site requests.
        member this.withCredentials (value: bool) =
            state <- { state with withCredentials = Some value }
            this

        member internal _.build () = state

    /// A http request response.
    type Response = 
        { statusCode: int
          statusText: string option
          content : U2<string,JS.ArrayBuffer> }

    /// Abstraction over an HTTP client.
    /// 
    /// This class provides an abstraction over an HTTP client so that a different 
    /// implementation can be provided on different platforms.
    type Client =
        /// Issues an HTTP GET request to the specified URL, returning a Promise that 
        /// resolves with an HttpResponse representing the result.
        abstract get: url: string -> JS.Promise<Response>
        /// Issues an HTTP GET request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract get: url: string * options: Request -> JS.Promise<Response>

        /// Issues an HTTP POST request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract post: url: string -> JS.Promise<Response>
        /// Issues an HTTP POST request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract post: url: string * options: Request -> JS.Promise<Response>

        /// Issues an HTTP DELETE request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract delete: url: string -> JS.Promise<Response>
        /// Issues an HTTP DELETE request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract delete: url: string * options: Request -> JS.Promise<Response>

        /// Issues an HTTP request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract send: request: Request -> JS.Promise<Response>

        ///Gets all cookies that apply to the specified URL.
        abstract getCookieString: url: string -> string

    type DefaultClient =
        inherit Client

        /// Issues an HTTP request to the specified URL, returning a Promise 
        /// that resolves with an HttpResponse representing the result.
        abstract send: request: Request -> JS.Promise<Response>

    type internal ConnectionOptions =
        { headers: Map<string,string> option
          httpClient: Client option
          transport: TransportType option
          logger: U2<ILogger,LogLevel> option
          accessTokenFactory: (unit -> U2<string, JS.Promise<string>>) option
          logMessageContent: bool option
          skipNegotiation: bool option
          withCredentials: bool option }

    /// Configures the SignalR connection.
    type ConnectionBuilder internal () =
        let mutable state =
            { headers = None
              httpClient = None
              transport = None
              logger = None
              accessTokenFactory = None
              logMessageContent = None
              skipNegotiation = None
              withCredentials = None }

        /// Custom headers to be sent with every HTTP request. Note, setting headers in 
        /// the browser will not work for WebSockets or the ServerSentEvents stream.
        member this.header (headers: Map<string,string>) =
            state <- { state with headers = Some headers }
            this

        /// An HttpClient that will be used to make HTTP requests.
        member this.httpClient (client: Client) = 
            state <- { state with httpClient = Some client }
            this

        /// An HttpTransportType value specifying the transport to use for the connection.
        member this.transport (transportType: TransportType) =
            state <- { state with transport = Some transportType }
            this

        /// Configures the logger used for logging.
        /// 
        /// Provide an ILogger instance, and log messages will be logged via that instance. 
        /// Alternatively, provide a value from the LogLevel enumeration and a default 
        /// logger which logs to the Console will be configured to log messages of the specified
        /// level (or higher).
        member this.logger (logger: ILogger) =
            state <- { state with logger = Some (U2.Case1(logger)) }
            this
        /// Configures the logger used for logging.
        /// 
        /// Provide an ILogger instance, and log messages will be logged via that instance. 
        /// Alternatively, provide a value from the LogLevel enumeration and a default logger 
        /// which logs to the Console will be configured to log messages of the specified
        /// level (or higher).
        member this.logger (logLevel: LogLevel) =
            state <- { state with logger = Some (U2.Case2(logLevel)) }
            this

        /// A function that provides an access token required for HTTP Bearer authentication.
        member this.accessTokenFactory (factory: unit -> string) =
            state <- { state with accessTokenFactory = Some (factory >> U2.Case1) }
            this
        /// A function that provides an access token required for HTTP Bearer authentication.
        member this.accessTokenFactory (factory: unit -> JS.Promise<string>) =
            state <- { state with accessTokenFactory = Some (factory >> U2.Case2) }
            this
        /// A function that provides an access token required for HTTP Bearer authentication.
        member this.accessTokenFactory (factory: unit -> Async<string>) = 
            state <- { state with accessTokenFactory = Some (factory >> Async.StartAsPromise >> U2.Case2) }
            this

        /// A boolean indicating if message content should be logged.
        /// 
        /// Message content can contain sensitive user data, so this is disabled by default.
        member this.logMessageContent (value: bool) =
            state <- { state with logMessageContent = Some value }
            this

        /// A boolean indicating if negotiation should be skipped.
        /// 
        /// Negotiation can only be skipped when the IHttpConnectionOptions.transport property 
        /// is set to 'HttpTransportType.WebSockets'.
        member this.skipNegotiation (value: bool) =
            state <- { state with skipNegotiation = Some value }
            this

        /// Default value is 'true'.
        /// This controls whether credentials such as cookies are sent in cross-site requests.
        /// 
        /// Cookies are used by many load-balancers for sticky sessions which is required when 
        /// your app is deployed with multiple servers.
        member this.withCredentials (value: bool) =
            state <- { state with withCredentials = Some value }
            this

        member internal _.build () = state
