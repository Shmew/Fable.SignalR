# Streaming

Data can also be streamed from the client and server without
having to repeatedly send new requests.

## Server Streaming

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

### Elmish

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
      StreamStatus: StreamStatus }

type Msg =
    ...
    | SignalRStreamMsg of StreamFrom.Response
    | StartServerStream
    | StreamStatus of StreamStatus

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
```

### Feliz

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
                let stream = input.hub.current.streamFrom StreamFrom.Action.GenInts
                stream.subscribe(subscriber)
                |> ignore
        ]
    ])

let render = React.functionComponent(fun () ->
    let hub =
        React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response>(fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Debug)
                .onMessage <| function | _ -> ()
        )

    Html.div [
        prop.children [
            display {| hub = hub |}
        ]
    ])
```

### Native

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
            .configureLogging(LogLevel.Debug)
            .onMessage <|
                function
                | Response.Howdy -> JS.console.log("Howdy!")
                | Response.NewCount i -> JS.console.log(i)
                | Response.RandomCharacter str -> JS.console.log(str))

hub.startNow()

let stream = hub.streamFrom StreamFrom.Action.GenInts
    
stream.subscribe(subscriber)
|> ignore
```

## Client Streaming

Streaming from the client allows you to send data to a subscriber on the server. 

Once you're ready to start a stream you will need to create an `ISubject<'T>`.

```fsharp
type ISubject<'T> =
    abstract next: item: 'T -> unit

    abstract error: err: exn -> unit

    abstract complete: unit -> unit

    abstract subscribe: observer: #IStreamSubscriber<'T> -> ISubscription
```

Luckily you don't need to create your own implementation of a subject, as
the SignalR library has a native implementation. You can create this via
`SignalR.Subject<'T>()`. If you're using something besides Elmish for state
management you can implement the `ISubject<'T>` interface to use that (such as an
RxJS Subject).

### Elmish

To enable streaming in your Elmish model you will need to call
a different `connect` function. Instead of `Cmd.SignalR.connect` you
will call `Cmd.SignalR.Stream.ClientToServer.connect`.

This would look like:
```fsharp
type Model =
    { ...
      Hub: Elmish.StreamHub.ClientToServer<Action,StreamTo.Action,Response> option
      ... }

let init () =
    ...
    , Cmd.SignalR.Stream.ClientToServer.connect ...
```

You then initiate a stream with the `Cmd.SignalR.streamTo` command.

Putting it all together:

```fsharp
type Hub = Elmish.StreamHub.ClientToServer<Action,StreamFrom.Action,Response,StreamFrom.Response>

[<RequireQualifiedAccess>]
type StreamStatus =
    | NotStarted
    | Error of exn option
    | Streaming
    | Finished

type Model =
    { ...
      Hub: Hub option
      ClientStreamStatus: StreamStatus }

    interface System.IDisposable with
        member this.Dispose () =
            this.Hub |> Option.iter (fun hub -> hub.Dispose())
            this.StreamSubscription |> Option.iter (fun ss -> ss.dispose())

type Msg =
    ...
    | StartClientStream
    | Subscription of ISubscription
    | StreamStatus of StreamStatus

let init =
    { ...
      Hub = None
      ClientStreamStatus = StreamStatus.NotStarted }
    , Cmd.SignalR.Stream.ClientToServer.connect RegisterHub (fun hub -> 
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug)
            .onMessage SignalRMsg)

let update msg model =
    match msg with
    ...
    | StartClientStream ->
        let subject = SignalR.Subject<StreamTo.Action>()

        model, Cmd.batch [ 
            Cmd.SignalR.streamTo model.Hub subject
            Cmd.ofSub (fun dispatch ->
                let dispatch = ClientStreamStatus >> dispatch

                dispatch StreamStatus.Streaming

                async {
                    try
                        for i in [1..100] do
                            subject.next(StreamTo.Action.GiveInt i)
                        subject.complete()
                        dispatch StreamStatus.Finished
                    with e -> StreamStatus.Error(Some e) |> dispatch
                }
                |> Async.StartImmediate
            )
        ]
    | StreamStatus ss -> { model with StreamStatus = ss }, Cmd.none
    | Subscription sub -> { model with StreamSubscription = Some sub }, Cmd.none
```

### Feliz

The first thing to note is that to enable streaming you must add additional typing 
to your connection call:

Going from:
```fsharp
React.useSignalR<Action,Response>
```

To:
```fsharp
React.useSignalR<Action,StreamTo.Action.Action,Response>
```

Once you've done this you can initialize a server stream by calling the `streamTo`
method on the Hub.

Putting it all together:
```fsharp
type Hub = StreamHub.ClientToServer<Action,StreamTo.Action,Response>
            
let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
    React.fragment [
        Html.div input.count
        Html.div input.text
    ])

let display = React.functionComponent(fun (input: {| count: int; hub: Hub |}) ->
    Html.button [
        prop.text "Stream To"
        prop.onClick <| fun _ -> 
            async {
                let subject = SignalR.Subject()
                            
                do! input.hub.current.streamTo(subject)
                                    
                for i in [1..100] do
                    do! Async.Sleep 10
                    subject.next (StreamTo.Action.GiveInt i)

                subject.complete()
            }
            |> Async.StartImmediate
    ])

let render = React.functionComponent(fun () ->
    let count,setCount = React.useState 0
    let text,setText = React.useState ""

    let hub =
        React.useSignalR<Action,StreamTo.Action,Response>(fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Debug)
                .onMessage <|
                    function
                    | Response.Howdy -> JS.console.log("Howdy!")
                    | Response.NewCount i -> setCount i
                    | Response.RandomCharacter str -> setText str
        )

    Html.div [
        prop.children [
            textDisplay {| count = count; text = text |}
            display {| count = count; hub = hub |}
        ]
    ])
```

### Native

The first thing to note is that to enable streaming you must add additional typing 
to your connection call:

Going from:
```fsharp
SignalR.connect<Action,_,_,Response,_>
```

To:
```fsharp
SignalR.connect<Action,_,StreamTo.Action,Response,_>
```

Once you've done this you can initialize a server stream by calling the `streamTo`
method on the Hub.

Putting it all together:
```fsharp
let hub =
    SignalR.connect<Action,unit,StreamTo.Action,Response,unit>(fun hub ->
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug)
            .onMessage <|
                function
                | Response.Howdy -> JS.console.log("Howdy!")
                | Response.NewCount i -> JS.console.log(i)
                | Response.RandomCharacter str -> JS.console.log(str))

hub.startNow()

async {
    let subject = SignalR.Subject()

    do! hub.streamTo(subject)

    for i in [1..100] do
        subject.next (StreamTo.Action.GiveInt i)
            
    subject.complete()
}
|> Async.StartImmediate
```
