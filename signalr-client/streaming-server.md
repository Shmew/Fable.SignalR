# Server Streaming

Streaming from the server allows you to call a request once
and get data sent to the provided subscriber. 

Once you're ready to start a stream you will need to create a `StreamSubscriber<'T>`.

This is simply a record that defines how to handle responses for the three different cases
that can/will occur: `next`, `complete`, and `error`. 

```fsharp
type StreamSubscriber<'T> =
    { /// Sends a new item to the server.
      next: 'T -> unit
      /// Sends an error to the server.
      error: exn option -> unit
      /// Completes the stream.
      complete: unit -> unit }
```

If you're not using Elmish for state management, you can
instead pass a type that interfaces `IStreamSubscriber<'T>`.

```fsharp
type IStreamSubscriber<'T> =
    /// Sends a new item to the server.
    abstract next: value: 'T -> unit
    /// Sends an error to the server.
    abstract error: exn option -> unit
    /// Completes the stream.
    abstract complete: unit -> unit
```

This enables you to use other libraries that follow observer/subscriber patterns
with SignalR streaming.

## Elmish

To enable streaming in your Elmish model you will need to call
a different `connect` function. Instead of `Cmd.SignalR.connect` you
will call `Cmd.SignalR.Stream.ServerToClient.connect`.

This would look like:
```fsharp
type Model =
    { ...
      Hub: Elmish.StreamHub.ServerToClient
        <Action,StreamFrom.Action,Response,StreamFrom.Response> option
      ... }

let init () =
    ...
    , Cmd.SignalR.Stream.ServerToClient.connect ...
```

The type definition can get pretty long, so creating a type alias can help 
keep your code more concise.

Since you can't access the dispatch inside your update function directly, the Cmd takes a 
function that given a dispatch returns your `StreamSubscriber<'T>`.

You then initiate a stream with the `Cmd.SignalR.streamFrom` command.

Putting it all together:

```fsharp
type Hub = Elmish.StreamHub.ServerToClient<Action,StreamFrom.Action,Response,StreamFrom.Response>

[<RequireQualifiedAccess>]
type StreamStatus =
    | NotStarted
    | Error of exn option
    | Streaming
    | Finished

type Model =
    { ...
      Hub: Hub option
      SFCount: int
      StreamStatus: StreamStatus
      StreamSubscription: System.IDisposable option }

type Msg =
    ...
    | SignalRStreamMsg of StreamFrom.Response
    | StartServerStream
    | StreamStatus of StreamStatus
    | Subscription of System.IDisposable

let init =
    { ...
      Hub = None
      SFCount = 0
      StreamSubscription = None
      StreamStatus = StreamStatus.NotStarted }
    , Cmd.SignalR.Stream.ServerToClient.connect RegisterHub (fun hub -> 
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug)
            .onMessage SignalRMsg)

let update msg model =
    match msg with
    ...
    | StartServerStream ->
        let subscriber dispatch =
            { next = SignalRStreamMsg >> dispatch
              complete = fun () -> StreamStatus.Finished |> StreamStatus |> dispatch
              error = StreamStatus.Error >> StreamStatus >> dispatch }

        { model with StreamStatus = StreamStatus.Streaming }
        , Cmd.SignalR.streamFrom model.Hub StreamFrom.Action.GenInts Subscription subscriber
    | StreamStatus ss -> { model with StreamStatus = ss }, Cmd.none
    | SignalRStreamMsg (StreamFrom.Response.GetInts i) -> { model with SFCount = i }, Cmd.none
    | Subscription sub -> { model with StreamSubscription = Some sub }, Cmd.none
```

## Feliz

The first thing to note is that to enable streaming you must add additional typing 
to your connection call:

Going from:
```fsharp
React.useSignalR<Action,Response>
```

To:
```fsharp
React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response>
```

Once you've done this you can initialize a server stream by calling the `streamFrom`
method on the Hub.

Putting it all together:
```fsharp
type Hub = StreamHub.ServerToClient<Action,StreamFrom.Action,Response,StreamFrom.Response>

let display = React.functionComponent(fun (input: {| hub: Hub |}) ->
    let count,setCount = React.useState(0)
    let subscription = React.useRef(None : System.IDisposable option)

    React.useEffectOnce(fun () -> 
        React.createDisposable <| fun () -> 
            subscription.current |> Option.iter (fun sub -> sub.Dispose()))

    let subscriber = 
        { next = fun (msg: StreamFrom.Response) -> 
            match msg with
            | StreamFrom.Response.GetInts i ->
                setCount(i)
          complete = fun () -> JS.console.log("Complete!")
          error = fun err -> JS.console.log(err) }

    React.fragment [
        Html.div count
        Html.button [
            prop.text "Stream From"
            prop.onClick <| fun _ -> 
                async {
                    let! stream = input.hub.current.streamFrom StreamFrom.Action.GenInts
                    subscription.current <- Some (stream.subscribe(subscriber))
                }
                |> Async.StartImmediate
        ]
    ])

let render = React.functionComponent(fun () ->
    let hub =
        React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response>(fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Debug))

    Html.div [
        prop.children [
            display {| hub = hub |}
        ]
    ])
```

## Native

The first thing to note is that to enable streaming you must add additional typing 
to your connection call:

Going from:
```fsharp
SignalR.connect<Action,_,_,Response,_>
```

To:
```fsharp
SignalR.connect<Action,StreamFrom.Action,_,Response,StreamFrom.Response>
```

Once you've done this you can initialize a server stream by calling the `streamFrom`
method on the Hub.

Putting it all together:
```fsharp
let subscriber = 
    { next = fun (msg: StreamFrom.Response) -> 
        match msg with
        | StreamFrom.Response.GetInts i ->
            JS.console.log(i)
      complete = fun () -> JS.console.log("Complete!")
      error = fun err -> JS.console.log(err) }

let hub =
    SignalR.connect<Action,StreamFrom.Action,unit,Response,StreamFrom.Response>(fun hub ->
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug))

hub.startNow()

let stream = hub.streamFrom StreamFrom.Action.GenInts
    
use sub = stream.subscribe(subscriber)
```
