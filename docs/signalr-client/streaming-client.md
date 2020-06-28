# Client Streaming

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

## Elmish

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

## Feliz

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

## Native

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
