module HubComponents

open Elmish
open Fable.Core
open Fable.SignalR
open Fable.SignalR.Elmish
open Fable.SignalR.Feliz
open Feliz
open Feliz.UseElmish
open SignalRApp
open SignalRApp.SignalRHub

module Elmish =
    type MyHub = Elmish.StreamHub.Bidrectional<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>

    [<RequireQualifiedAccess>]
    type StreamStatus =
        | NotStarted
        | Error of exn option
        | Streaming
        | Finished

    type Model =
        { Count: int
          Text: string
          Hub: MyHub option
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
        | SayHello
        | StartClientStream
        | StartServerStream
        | RegisterHub of MyHub
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
        , Cmd.SignalR.Stream.Bidrectional.connect RegisterHub SignalRMsg (fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Debug))

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
        | SignalRStreamMsg (StreamFrom.Response.GetInts i) ->
            { model with SFCount = i }, Cmd.none
        | IncrementCount ->
            model, Cmd.SignalR.send model.Hub (Action.IncrementCount model.Count)
        | DecrementCount ->
            model, Cmd.SignalR.send model.Hub (Action.DecrementCount model.Count)
        | RandomCharacter ->
            model, Cmd.SignalR.send model.Hub Action.RandomCharacter
        | SayHello ->
            model, Cmd.SignalR.send model.Hub Action.SayHello
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
                prop.testId "display-count"
                prop.text input.Count
            ]
            Html.div [
                prop.testId "display-text"
                prop.text input.Text
            ]
            Html.div [
                prop.testId "display-sfcount"
                prop.text input.SFCount
            ]
            Html.div [
                prop.testId "display-streamstatus"
                prop.textf "%A" input.StreamStatus
            ]
            Html.div [
                prop.testId "display-clientstreamstatus"
                prop.textf "%A" input.ClientStreamStatus
            ]
        ])

    let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
        React.fragment [
            Html.button [
                prop.testId "buttons-increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> input.dispatch IncrementCount
            ]
            Html.button [
                prop.testId "buttons-decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.dispatch DecrementCount
            ]
            Html.button [
                prop.testId "buttons-random"
                prop.text "Get Random Character"
                prop.onClick <| fun _ -> input.dispatch RandomCharacter
            ]
            Html.button [
                prop.testId "buttons-serverstream"
                prop.text "Start Server Stream"
                prop.onClick <| fun _ -> input.dispatch StartServerStream
            ]
            Html.button [
                prop.testId "buttons-clientstream"
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
                prop.testId "textDisplay-count"
                prop.text input.count
            ]
            Html.div [
                prop.testId "textDisplay-text"
                prop.text input.text
            ]
        ])

    let button = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
        React.fragment [
            Html.button [
                prop.testId "buttons-increment"
                prop.text "Increment"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
            ]
            Html.button [
                prop.testId "buttons-decrement"
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
            ]
            Html.button [
                prop.testId "buttons-random"
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
                        | Response.Howdy -> JS.console.log("Howdy!")
                        | Response.NewCount i -> setCount i
                        | Response.RandomCharacter str -> setText str
            )

        Html.div [
            prop.children [
                textDisplay {| count = count; text = text |}
                button {| count = count; hub = hub |}
            ]
        ])

module StreamingHook =
    let textDisplay = React.functionComponent(fun (input: {| count: int |}) ->
        Html.div [
            prop.testId "textDisplay-text"
            prop.textf "From server: %i" input.count
        ])

    let display = React.functionComponent(fun (input: {| hub: StreamHub.Bidrectional<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response> |}) ->
        let count,setCount = React.useState 0
            
        let subscriber = 
            { next = fun (msg: StreamFrom.Response) -> 
                match msg with
                | StreamFrom.Response.GetInts i ->
                    setCount(i)
              complete = fun () -> JS.console.log("Complete!")
              error = fun err -> JS.console.log(err) }
            
        React.fragment [
            Html.div [
                prop.testId "display-count"
                prop.textf "From client: %i" count
            ]
            Html.button [
                prop.testId "display-streamTo"
                prop.text "Stream To"
                prop.onClick <| fun _ -> 
                    async {
                        let subject = SignalR.Subject()
                            
                        do! input.hub.current.streamTo(subject)
                            
                        for i in [1..100] do
                            do! Async.Sleep 50
                            subject.next (StreamTo.Action.GiveInt i)
                    }
                    |> Async.StartImmediate
            ]
            Html.button [
                prop.testId "display-streamFrom"
                prop.text "Stream From"
                prop.onClick <| fun _ -> 
                    promise {
                        let stream = input.hub.current.streamFrom StreamFrom.Action.GenInts
                        stream.subscribe(subscriber)
                        |> ignore
                    }
                    |> Promise.start
            ]
        ])

    let render = React.functionComponent(fun () ->
        let count,setCount = React.useState 0

        let hub =
            React.useSignalR<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .onMessage <| 
                        function 
                        | Response.NewCount i -> setCount i
                        | _ -> ()
            )

        Html.div [
            prop.children [
                textDisplay {| count = count |}
                display {| hub = hub |}
            ]
        ])
