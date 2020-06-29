namespace SignalRApp

module App =
    open Elmish
    open Fable.Core
    open Fable.SignalR
    open Fable.SignalR.Elmish
    open Feliz
    open Feliz.UseElmish
    open SignalRHub

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
            | RegisterHub of Elmish.Hub<Action,Response>

        let init =
            { Count = 0
              Text = ""
              Hub = None }
            , Cmd.SignalR.connect RegisterHub (fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .onMessage SignalRMsg)

        let update msg model =
            match msg with
            | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
            | SignalRMsg rsp ->
                match rsp with
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

        let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
            React.fragment [
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
            React.fragment [
                Html.button [
                    prop.text "Increment"
                    prop.onClick <| fun _ -> input.dispatch IncrementCount
                ]
                Html.button [
                    prop.text "Decrement"
                    prop.onClick <| fun _ -> input.dispatch DecrementCount
                ]
                Html.button [
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
            | RegisterHub of Elmish.Hub<Action,Response>

        let init =
            { Count = 0
              Text = ""
              Hub = None }
            , Cmd.SignalR.connect RegisterHub (fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug))

        let update msg model =
            match msg with
            | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
            | SignalRMsg rsp ->
                match rsp with
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

        let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
            React.fragment [
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
            React.fragment [
                Html.button [
                    prop.text "Increment"
                    prop.onClick <| fun _ -> input.dispatch IncrementCount
                ]
                Html.button [
                    prop.text "Decrement"
                    prop.onClick <| fun _ -> input.dispatch DecrementCount
                ]
                Html.button [
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
            { Count: int
              Text: string
              Hub: Hub option
              SFCount: int
              StreamSubscription: ISubscription option
              StreamStatus: StreamStatus
              ClientStreamStatus: StreamStatus }

            interface System.IDisposable with
                member this.Dispose () =
                    this.Hub |> Option.iter (fun hub -> hub.Dispose())
                    this.StreamSubscription |> Option.iter (fun ss -> ss.dispose())

        type Msg =
            | SignalRMsg of Response
            | SignalRStreamMsg of StreamFrom.Response
            | IncrementCount
            | DecrementCount
            | RandomCharacter
            | StartClientStream
            | StartServerStream
            | RegisterHub of Hub
            | Subscription of ISubscription
            | StreamStatus of StreamStatus
            | ClientStreamStatus of StreamStatus

        let init =
            { Count = 0
              Text = ""
              Hub = None
              SFCount = 0
              StreamSubscription = None
              StreamStatus = StreamStatus.NotStarted
              ClientStreamStatus = StreamStatus.NotStarted }
            , Cmd.SignalR.Stream.Bidrectional.connect RegisterHub (fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .onMessage SignalRMsg)

        let update msg model =
            match msg with
            | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
            | SignalRMsg rsp ->
                match rsp with
                | Response.RandomCharacter str ->
                    { model with Text = str }, Cmd.none
                | Response.NewCount i ->
                    { model with Count = i }, Cmd.none
            | SignalRStreamMsg (StreamFrom.Response.GetInts i) ->
                { model with SFCount = i }, Cmd.none
            | IncrementCount ->
                model, Cmd.SignalR.send model.Hub (Action.IncrementCount model.Count)
            | DecrementCount ->
                model, Cmd.SignalR.send model.Hub (Action.DecrementCount model.Count)
            | RandomCharacter ->
                model, Cmd.SignalR.send model.Hub Action.RandomCharacter
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
                    prop.text input.Count
                ]
                Html.div [
                    prop.text input.Text
                ]
                Html.div [
                    prop.text input.SFCount
                ]
                Html.div [
                    prop.textf "%A" input.StreamStatus
                ]
                Html.div [
                    prop.textf "%A" input.ClientStreamStatus
                ]
            ])

        let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
            React.fragment [
                Html.button [
                    prop.text "Increment"
                    prop.onClick <| fun _ -> input.dispatch IncrementCount
                ]
                Html.button [
                    prop.text "Decrement"
                    prop.onClick <| fun _ -> input.dispatch DecrementCount
                ]
                Html.button [
                    prop.text "Get Random Character"
                    prop.onClick <| fun _ -> input.dispatch RandomCharacter
                ]
                Html.button [
                    prop.text "Start Server Stream"
                    prop.onClick <| fun _ -> input.dispatch StartServerStream
                ]
                Html.button [
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
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
            React.fragment [
                Html.button [
                    prop.text "Increment"
                    prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
                ]
                Html.button [
                    prop.text "Decrement"
                    prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
                ]
                Html.button [
                    prop.text "Get Random Character"
                    prop.onClick <| fun _ -> input.hub.current.sendNow Action.RandomCharacter
                ]
            ])

        let render = React.functionComponent(fun () ->
            let count,setCount = React.useState 0
            let text,setText = React.useState ""

            let hub =
                React.useSignalR<Action,Response>(fun hub -> 
                    hub.withUrl(Endpoints.Root)
                        .withAutomaticReconnect()
                        .configureLogging(LogLevel.Debug)
                        .onMessage <|
                            function
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
                    Html.div count
                    Html.div text
                ]
                Html.button [
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
                    hub.withUrl(Endpoints.Root)
                        .withAutomaticReconnect()
                        .configureLogging(LogLevel.Debug)
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
                    prop.textf "%b" clientIsComplete
                ]
                Html.div [
                    prop.textf "%b" serverIsComplete
                ]
                Html.button [
                    prop.text "Stream To"
                    prop.onClick <| fun _ -> 
                        async {
                            let subject = SignalR.Subject()
                                    
                            do! input.hub.current.streamTo(subject)
                                    
                            for i in [1..100] do
                                do! Async.Sleep 50
                                subject.next (StreamTo.Action.GiveInt i)
                            setClientIsComplete(true)
                        }
                        |> Async.StartImmediate
                ]
                Html.button [
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
                    hub.withUrl(Endpoints.Root)
                        .withAutomaticReconnect()
                        .configureLogging(LogLevel.Debug)
                )
        
            Html.div [
                prop.children [
                    textDisplay {| count = count |}
                    display {| hub = hub; callback = setCount |}
                ]
            ])

    let render = React.functionComponent(fun () ->
        Html.div [
            Html.div "Elmish"
            Elmish.render()
            Html.div "InvokeElmish"
            InvokeElmish.render()
            Html.div "StreamingElmish"
            StreamingElmish.render()
            Html.div "Hook"
            Hook.render()
            Html.div "InvokeHook"
            InvokeHook.render()
            Html.div "StreamingHook"
            StreamingHook.render()
        ])

    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
