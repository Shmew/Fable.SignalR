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
            | SayHello
            | RegisterHub of Elmish.Hub<Action,Response>

        let init =
            { Count = 0
              Text = ""
              Hub = None }
            , Cmd.SignalR.connect RegisterHub SignalRMsg (fun hub -> 
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
            | IncrementCount ->
                model, Cmd.SignalR.send model.Hub (Action.IncrementCount model.Count)
            | DecrementCount ->
                model, Cmd.SignalR.send model.Hub (Action.DecrementCount model.Count)
            | RandomCharacter ->
                model, Cmd.SignalR.send model.Hub Action.RandomCharacter
            | SayHello ->
                model, Cmd.SignalR.send model.Hub Action.SayHello

        let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
            Html.div [
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| dispatch: Msg -> unit |}) ->
            React.fragment [
                Html.button [
                    prop.text "Testing"
                    prop.onClick <| fun _ -> input.dispatch IncrementCount
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

    module Hook =
        let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
            Html.div [
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
            React.fragment [
                Html.button [
                    prop.text "Testing"
                    prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
                ]
            ])

        let render = React.functionComponent(fun () ->
            let count,setCount = React.useState 0
            let text,setText = React.useState ""
            let testing,setTesting = React.useState false

            let hub =
                React.useSignalR<Action,Response>((fun hub -> 
                    hub.withUrl(Endpoints.Root)
                        .withAutomaticReconnect()
                        .configureLogging(LogLevel.Debug)
                        .onMessage <|
                            function
                            | Response.Howdy -> JS.console.log("Howdy!")
                            | Response.NewCount i -> setCount i
                            | Response.RandomCharacter str -> setText str
                ), [| testing :> obj |])
            
            React.useEffect(fun () ->
                if count > 5 then setTesting true
            )

            Html.div [
                prop.children [
                    textDisplay {| count = count; text = text |}
                    buttons {| count = count; hub = hub |}
                ]
            ])

    module StreamingHook =
        module Bidirectional =
            let textDisplay = React.functionComponent(fun (input: {| count: int |}) ->
                Html.div [
                    Html.text (sprintf "From server: %i" input.count)
                ])

            let buttons = React.functionComponent(fun (input: {| hub: StreamHub.Bidrectional<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response> |}) ->
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
                        prop.text (sprintf "By client: %i" count)
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
                            }
                            |> Async.StartImmediate
                    ]
                    Html.button [
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
                        buttons {| hub = hub |}
                    ]
                ])

        module ClientToServer =
            let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
                Html.div [
                    Html.div input.count
                    Html.div input.text
                ])

            let buttons = React.functionComponent(fun (input: {| count: int; hub: StreamHub.ClientToServer<Action,StreamTo.Action,Response> |}) ->
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
                let testing,setTesting = React.useState false

                let hub =
                    React.useSignalR<Action,StreamTo.Action,Response>((fun hub -> 
                        hub.withUrl(Endpoints.Root)
                            .withAutomaticReconnect()
                            .configureLogging(LogLevel.Debug)
                            .onMessage <|
                                function
                                | Response.Howdy -> JS.console.log("Howdy!")
                                | Response.NewCount i -> setCount i
                                | Response.RandomCharacter str -> setText str
                    ), [| testing :> obj |])
            
                React.useEffect(fun () ->
                    if count > 5 then setTesting true
                )

                Html.div [
                    prop.children [
                        textDisplay {| count = count; text = text |}
                        buttons {| count = count; hub = hub |}
                    ]
                ])

        module ServerToClient =
            let display = React.functionComponent(fun (input: {| hub: StreamHub.ServerToClient<Action,StreamFrom.Action,Response,StreamFrom.Response> |}) ->
                let count,setCount = React.useState(0)
                
                let subscriber = 
                    { next = fun (msg: StreamFrom.Response) -> 
                        match msg with
                        | StreamFrom.Response.GetInts i ->
                            setCount(i)
                      complete = fun () -> JS.console.log("Complete!")
                      error = fun err -> JS.console.log(err) }

                React.fragment [
                    Html.div [
                        Html.div count
                    ]
                    Html.button [
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

    let render = React.functionComponent(fun () ->
        Html.div [
            Elmish.render()
            Hook.render()
            StreamingHook.ClientToServer.render()
            StreamingHook.ServerToClient.render()
            StreamingHook.Bidirectional.render()
        ])

    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
