module Components

open Elmish
open Fable.Core
open Fable.SignalR
open Fable.SignalR.Elmish
open Feliz
open Feliz.UseElmish
open SignalRApp
open SignalRApp.SignalRHub

module Elmish =
    type Model =
        { Count: int
          Text: string
          Hub: Elmish.Hub<Action,Response> option }

        interface System.IDisposable with
            member this.Dispose () =
                this.Hub |> Option.iter (fun hub -> hub.Dispose())

    type Msg =
        | SignalRMsg of Response
        | IncrementCount
        | DecrementCount
        | RandomCharacter
        | SayHello
        | RegisterHub of Elmish.Hub<Action,Response>

    let init =
        { Count = 0
          Text = ""
          Hub = None }
        , Cmd.batch [
            Cmd.SignalR.connect RegisterHub (fun hub -> 
                hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None)
                    .onMessage SignalRMsg)
        ]
        
    let update msg model =
        match msg with
        | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
        | SignalRMsg rsp ->
            match rsp with
            | Response.Howdy -> model, Cmd.none
            | Response.RandomCharacter str ->
                { model with Text = str }, Cmd.none
            | Response.NewCount i ->
                { model with Count = i }, Cmd.none
        | IncrementCount ->
            model, Cmd.SignalR.send model.Hub (Action.IncrementCount model.Count)
        | DecrementCount ->
            model, Cmd.SignalR.send model.Hub (Action.DecrementCount model.Count)
        | RandomCharacter ->
            model, Cmd.SignalR.send model.Hub Action.RandomCharacter
        | SayHello ->
            model, Cmd.SignalR.send model.Hub Action.SayHello

    let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
        React.fragment [
            Html.div [
                prop.testId "count"
                prop.text input.count
            ]
            Html.div [
                prop.testId "text"
                prop.text input.text
            ]
        ])

    let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
        React.fragment [
            Html.button [
                prop.testId "increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> input.dispatch IncrementCount
            ]
            Html.button [
                prop.testId "decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.dispatch DecrementCount
            ]
            Html.button [
                prop.testId "random"
                prop.text "Get Random Character"
                prop.onClick <| fun _ -> input.dispatch RandomCharacter
            ]
        ])

    let render = React.functionComponent(fun () ->
        let state,dispatch = React.useElmish(init, update, [||])

        Html.div [
            prop.children [
                textDisplay {| count = state.Count; text = state.Text |}
                buttons {| dispatch = dispatch |}
            ]
        ])

module InvokeElmish =
    type Model =
        { Count: int
          Text: string
          Hub: Elmish.Hub<Action,Response> option }

        interface System.IDisposable with
            member this.Dispose () =
                this.Hub |> Option.iter (fun hub -> hub.Dispose())

    type Msg =
        | SignalRMsg of Response
        | IncrementCount
        | DecrementCount
        | RandomCharacter
        | SayHello
        | RegisterHub of Elmish.Hub<Action,Response>

    let init =
        { Count = 0
          Text = ""
          Hub = None }
        , Cmd.SignalR.connect RegisterHub (fun hub -> 
            hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None))

    let update msg model =
        match msg with
        | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
        | SignalRMsg rsp ->
            match rsp with
            | Response.Howdy -> model, Cmd.none
            | Response.RandomCharacter str ->
                { model with Text = str }, Cmd.none
            | Response.NewCount i ->
                { model with Count = i }, Cmd.none
        | IncrementCount ->
            model, Cmd.SignalR.perform model.Hub (Action.IncrementCount model.Count) SignalRMsg 
        | DecrementCount ->
            model, Cmd.SignalR.perform model.Hub (Action.DecrementCount model.Count) SignalRMsg
        | RandomCharacter ->
            model, Cmd.SignalR.perform model.Hub Action.RandomCharacter SignalRMsg
        | SayHello ->
            model, Cmd.SignalR.perform model.Hub Action.SayHello SignalRMsg

    let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
        React.fragment [
            Html.div [
                prop.testId "count"
                prop.text input.count
            ]
            Html.div [
                prop.testId "text"
                prop.text input.text
            ]
        ])

    let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
        React.fragment [
            Html.button [
                prop.testId "increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> input.dispatch IncrementCount
            ]
            Html.button [
                prop.testId "decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.dispatch DecrementCount
            ]
            Html.button [
                prop.testId "random"
                prop.text "Get Random Character"
                prop.onClick <| fun _ -> input.dispatch RandomCharacter
            ]
        ])

    let render = React.functionComponent(fun () ->
        let state,dispatch = React.useElmish(init, update, [||])

        Html.div [
            prop.children [
                textDisplay {| count = state.Count; text = state.Text |}
                buttons {| dispatch = dispatch |}
            ]
        ])

module StreamingElmish =
    type Hub = Elmish.StreamHub.Bidrectional<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>

    [<RequireQualifiedAccess>]
    type StreamStatus =
        | NotStarted
        | Error of exn option
        | Streaming
        | Finished

    type Model =
        { Hub: Hub option
          Count: int
          StreamSubscription: ISubscription option
          StreamStatus: StreamStatus
          ClientStreamStatus: StreamStatus }

        interface System.IDisposable with
            member this.Dispose () =
                this.Hub |> Option.iter (fun hub -> hub.Dispose())
                this.StreamSubscription |> Option.iter (fun ss -> ss.dispose())

    type Msg =
        | SignalRStreamMsg of StreamFrom.Response
        | StartClientStream
        | StartServerStream
        | RegisterHub of Hub
        | Subscription of ISubscription
        | StreamStatus of StreamStatus
        | ClientStreamStatus of StreamStatus

    let init =
        { Hub = None
          Count = 0
          StreamSubscription = None
          StreamStatus = StreamStatus.NotStarted
          ClientStreamStatus = StreamStatus.NotStarted }
        , Cmd.SignalR.Stream.Bidrectional.connect RegisterHub (fun hub -> 
            hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None))

    let update msg model =
        match msg with
        | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
        | SignalRStreamMsg (StreamFrom.Response.GetInts i) ->
            { model with Count = i }, Cmd.none
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
        | StartServerStream ->
            let subscriber dispatch =
                { next = SignalRStreamMsg >> dispatch
                  complete = fun () -> StreamStatus.Finished |> StreamStatus |> dispatch
                  error = StreamStatus.Error >> StreamStatus >> dispatch }

            { model with StreamStatus = StreamStatus.Streaming }, Cmd.SignalR.streamFrom model.Hub StreamFrom.Action.GenInts Subscription subscriber
        | Subscription sub ->
            { model with StreamSubscription = Some sub }, Cmd.none
        | StreamStatus ss ->
            { model with StreamStatus = ss }, Cmd.none
        | ClientStreamStatus ss ->
            { model with ClientStreamStatus = ss }, Cmd.none

    let display = React.functionComponent(fun (input: Model) ->
        React.fragment [
            Html.div [
                prop.testId "count"
                prop.text input.Count
            ]
            Html.div [
                prop.testId "server-stream-status"
                prop.textf "%A" input.StreamStatus
            ]
            Html.div [
                prop.testId "client-stream-status"
                prop.textf "%A" input.ClientStreamStatus
            ]
        ])

    let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
        React.fragment [
            Html.button [
                prop.testId "start-server-stream"
                prop.text "Start Server Stream"
                prop.onClick <| fun _ -> input.dispatch StartServerStream
            ]
            Html.button [
                prop.testId "start-client-stream"
                prop.text "Start Client Stream"
                prop.onClick <| fun _ -> input.dispatch StartClientStream
            ]
        ])

    let render = React.functionComponent(fun () ->
        let state,dispatch = React.useElmish(init, update, [||])

        Html.div [
            prop.children [
                display state
                buttons {| dispatch = dispatch |}
            ]
        ])

module Hook =
    let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
        React.fragment [
            Html.div [
                prop.testId "count"
                prop.text input.count
            ]
            Html.div [
                prop.testId "text"
                prop.text input.text
            ]
        ])

    let buttons = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
        React.fragment [
            Html.button [
                prop.testId "increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
            ]
            Html.button [
                prop.testId "decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
            ]
            Html.button [
                prop.testId "random"
                prop.text "Get Random Character"
                prop.onClick <| fun _ -> input.hub.current.sendNow Action.RandomCharacter
            ]
        ])

    let render = React.functionComponent(fun () ->
        let count,setCount = React.useState 0
        let text,setText = React.useState ""

        let hub =
            React.useSignalR<Action,Response>(fun hub -> 
                hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None)
                    .onMessage <|
                        function
                        | Response.Howdy -> JS.console.log("Howdy!")
                        | Response.NewCount i -> setCount i
                        | Response.RandomCharacter str -> setText str
            )
        
        Html.div [
            prop.children [
                textDisplay {| count = count; text = text |}
                buttons {| count = count; hub = hub |}
            ]
        ])

module InvokeHook =
    let display = React.functionComponent(fun (input: {| hub: Hub<Action,Response> |}) ->
        let count,setCount = React.useState 0
        let text,setText = React.useState ""

        React.fragment [
            Html.div [
                Html.div [
                    prop.testId "count"
                    prop.text count
                ]
                Html.div [
                    prop.testId "text"
                    prop.text text
                ]
            ]
            Html.button [
                prop.testId "increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> 
                    async {
                        let! rsp = input.hub.current.invoke (Action.IncrementCount count)
                        
                        match rsp with
                        | Response.NewCount i -> setCount i
                        | _ -> ()
                    }
                    |> Async.StartImmediate
            ]
            Html.button [
                prop.testId "decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> 
                    promise {
                        let! rsp = input.hub.current.invokeAsPromise (Action.DecrementCount count)
                        
                        match rsp with
                        | Response.NewCount i -> setCount i
                        | _ -> ()
                    }
                    |> Promise.start
            ]
            Html.button [
                prop.testId "random"
                prop.text "Get Random Character"
                prop.onClick <| fun _ -> 
                    async {
                        let! rsp = input.hub.current.invoke Action.RandomCharacter
                        
                        match rsp with
                        | Response.RandomCharacter str -> setText str
                        | _ -> ()
                    }
                    |> Async.StartImmediate
            ]
        ])

    let render = React.functionComponent(fun () ->
        let hub =
            React.useSignalR<Action,Response>(fun hub -> 
                hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None)
                    .onMessage <| fun (msg: Response) -> JS.console.log("")
            )
        
        Html.div [
            prop.children [
                display {| hub = hub |}
            ]
        ])

module StreamingHook =
    type Hub = StreamHub.Bidrectional<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>

    let textDisplay = React.functionComponent(fun (input: {| count: int |}) ->
        Html.div [
            prop.testId "count"
            prop.textf "%i" input.count
        ])

    let display = React.functionComponent(fun (input: {| hub: Hub; callback: int -> unit |}) ->
        let clientIsComplete,setClientIsComplete = React.useState false
        let serverIsComplete,setServerIsComplete = React.useState false

        let subscriber = 
            { next = fun (msg: StreamFrom.Response) -> 
                match msg with
                | StreamFrom.Response.GetInts i -> input.callback i
              complete = fun () -> setServerIsComplete true
              error = fun _ -> () }
            
        React.fragment [
            Html.div [
                prop.testId "client-complete"
                prop.textf "%b" clientIsComplete
            ]
            Html.div [
                prop.testId "server-complete"
                prop.textf "%b" serverIsComplete
            ]
            Html.button [
                prop.testId "start-client-stream"
                prop.text "Stream To"
                prop.onClick <| fun _ -> 
                    async {
                        let subject = SignalR.Subject()
                            
                        do! input.hub.current.streamTo(subject)
                            
                        for i in [1..100] do
                            subject.next (StreamTo.Action.GiveInt i)
                        setClientIsComplete(true)
                    }
                    |> Async.StartImmediate
            ]
            Html.button [
                prop.testId "start-server-stream"
                prop.text "Stream From"
                prop.onClick <| fun _ -> 
                    async {
                        let! stream = input.hub.current.streamFrom StreamFrom.Action.GenInts
                        stream.subscribe(subscriber)
                        |> ignore
                    }
                    |> Async.StartImmediate
            ]
        ])

    let render = React.functionComponent(fun () ->
        let count,setCount = React.useState 0

        let setCount = React.useCallback(setCount, [||])

        let hub =
            React.useSignalR<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub -> 
                hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.None)
            )

        Html.div [
            prop.children [
                textDisplay {| count = count |}
                display {| hub = hub; callback = setCount |}
            ]
        ])
