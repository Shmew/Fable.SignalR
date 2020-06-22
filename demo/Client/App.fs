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
              Hub: ElmishHub<Action,Response> option }

            interface System.IDisposable with
                member this.Dispose () =
                    this.Hub |> Option.iter (fun hub -> hub.Dispose())

        type Msg =
            | SignalRMsg of Response
            | IncrementCount
            | DecrementCount
            | RandomCharacter
            | SayHello
            | RegisterHub of ElmishHub<Action,Response>

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
                | _ -> model, Cmd.none
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
            ]
        )

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

        let buttons = React.functionComponent(fun (input: {| count: int; hub: HubRef<Action,Response> |}) ->
            React.fragment [
                Html.button [
                    prop.text "Testing"
                    prop.onClick <| fun _ -> input.hub.current.send (Action.IncrementCount input.count)
                ]
            ]
        )

        let render = React.functionComponent(fun () ->
            let count,setCount = React.useState 0
            let text,setText = React.useState ""
            let testing,setTesting = React.useState false

            let hub =
                React.useSignalR<Action,Response>({
                    config = 
                        fun hub -> 
                            hub.withUrl(Endpoints.Root)
                                .withAutomaticReconnect()
                                .configureLogging(LogLevel.Debug)

                    onMsg =
                        function
                        | Response.Howdy -> JS.console.log("Howdy!")
                        | Response.NewCount i -> setCount i
                        | Response.RandomCharacter str -> setText str
                        | _ -> ()
                }, [| testing :> obj |])
            
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
        let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
            Html.div [
                Html.div input.count
                Html.div input.text
            ])

        let buttons = React.functionComponent(fun (input: {| count: int; hub: HubRef<Action,Response> |}) ->
            React.fragment [
                Html.button [
                    prop.text "Stream"
                    prop.onClick <| fun _ -> 
                        promise {
                            let stream = input.hub.current.stream Action.GetInts
                            stream.subscribe (
                                {| closed = false
                                   next = fun (msg: Response) -> 
                                    match msg with
                                    | Response.GetInts i ->
                                        JS.console.log(i)
                                    | _ -> ()
                                   complete = fun () -> JS.console.log("Complete!")
                                   error = fun err -> JS.console.log(err) |}
                                |> unbox
                            ) |> unbox
                        }
                        |> Promise.start
                ]
            ]
        )

        let render = React.functionComponent(fun () ->
            let count,setCount = React.useState 0
            let text,setText = React.useState ""
            let testing,setTesting = React.useState false

            let hub =
                React.useSignalR<Action,Response>({
                    config = 
                        fun hub -> 
                            hub.withUrl(Endpoints.Root)
                                .withAutomaticReconnect()
                                .configureLogging(LogLevel.Debug)

                    onMsg =
                        function
                        | Response.Howdy -> JS.console.log("Howdy!")
                        | Response.NewCount i -> setCount i
                        | Response.RandomCharacter str -> setText str
                        | _ -> ()
                }, [| testing :> obj |])
            
            React.useEffect(fun () ->
                if count > 5 then setTesting true
            )

            Html.div [
                prop.children [
                    textDisplay {| count = count; text = text |}
                    buttons {| count = count; hub = hub |}
                ]
            ])

    let render = React.functionComponent(fun () ->
        Html.div [
            Elmish.render()
            Hook.render()
            StreamingHook.render()
        ])



    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
