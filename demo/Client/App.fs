namespace SignalRApp

module App =
    open Fable.Core
    open Fable.SignalR
    open Feliz
    open Feliz.Plotly
    open SignalRHub
    open Zanaptak.TypedCssClasses

    type Bulma = CssClasses<"https://cdnjs.cloudflare.com/ajax/libs/bulma/0.7.5/css/bulma.min.css", Naming.PascalCase>

    type Hub = StreamHub.ServerToClient<Action,StreamFrom.Action,Response,StreamFrom.Response>
        
    let private graph = React.functionComponent(fun (input: {| dates: System.DateTime list; lows: float list; highs: float list |}) ->
        Plotly.plot [
            plot.traces [
                traces.scatter [
                    scatter.mode.lines
                    scatter.name "AAPL High"
                    scatter.x input.dates
                    scatter.y input.highs
                    scatter.line [
                        line.color "#17BECF"
                    ]
                ]
                traces.scatter [
                    scatter.mode.lines
                    scatter.name "AAPL Low"
                    scatter.x input.dates
                    scatter.y input.lows
                    scatter.line [
                        line.color "#7F7F7F"
                    ]
                ]
            ]
            plot.layout [
                layout.title [
                    title.text "Apple Stock Price"
                ]
                layout.xaxis [
                    xaxis.range [ System.DateTime(2015, 2, 17); System.DateTime(2017, 2, 16) ]
                    xaxis.type'.date
                    xaxis.fixedrange true
                ]
                layout.yaxis [
                    yaxis.range [ 86.8700008333; 138.870004167 ]
                    yaxis.type'.linear
                    yaxis.fixedrange true
                ]
                layout.transition [
                    transition.duration 0
                    transition.easing.linear
                    transition.ordering.tracesFirst
                ]
            ]
            plot.config [
                config.displayModeBar.false'
            ]
        ])

    let display = React.functionComponent(fun (input: {| hub: Hub |}) ->
        let dates,setDates = React.useState([] : System.DateTime list)
        let lows,setLows = React.useState([]: float list)
        let highs,setHighs = React.useState([] : float list)
        let subscription = React.useRef(None : System.IDisposable option)
        let isRunning,setIsRunning = React.useState(false)
        
        let next =
            React.useCallbackRef(fun (msg: StreamFrom.Response) -> 
                match msg with
                | StreamFrom.Response.AppleStock s ->
                    setDates(dates @ [ s.Date ])
                    setLows(lows @ [ s.Low ]) 
                    setHighs(highs @ [ s.High ]) 
            )

        let subscriber = 
            { next = next
              complete = fun () -> JS.console.log("Complete!")
              error = fun e -> JS.console.log(e) }
        
        let startStream =
            async {
                setIsRunning true
                let! streamResult =
                    StreamFrom.Action.AppleStocks
                    |> input.hub.current.streamFrom

                subscription.current <- Some (streamResult.subscribe(subscriber))
            }

        React.useEffectOnce(fun () ->
            Async.StartImmediate startStream
            
            React.createDisposable(fun () -> subscription.current |> Option.iter(fun sub -> sub.Dispose()))
        )
                    
        React.fragment [
            graph {| dates = dates; lows = lows; highs = highs |}
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite 
                ]
                prop.disabled (not isRunning)
                prop.text "Stop"
                prop.onClick <| fun _ -> 
                    setIsRunning false
                    subscription.current |> Option.iter(fun sub -> sub.Dispose())
            ]
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite
                ]
                prop.disabled isRunning
                prop.text "Restart"
                prop.onClick <| fun _ ->
                    setDates []
                    setLows []
                    setHighs []
                    
                    Async.StartImmediate startStream 
            ]
        ])

    let countButtons = React.functionComponent(fun (input: {| hub: Hub; count: int |}) ->
        React.fragment [
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite 
                ]
                prop.text "Increment"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
            ]
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite 
                ]
                prop.text "Decrement"
                prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
            ]
        ])

    let countInvokeButtons = React.functionComponent(fun (input: {| hub: Hub; count: int; callback: int -> unit |}) ->
        React.fragment [
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite 
                ]
                prop.text "Increment"
                prop.onClick <| fun _ ->
                    async {
                        let! res = input.hub.current.invoke (Action.IncrementCount input.count)

                        match res with
                        | Response.NewCount i -> input.callback i
                        | _ -> ()
                    }
                    |> Async.StartImmediate
            ]
            Html.button [
                prop.classes [ 
                    Bulma.Button
                    Bulma.HasBackgroundPrimary
                    Bulma.HasTextWhite 
                ]
                prop.text "Decrement"
                prop.onClick <| fun _ ->
                    async {
                        let! res = input.hub.current.invoke (Action.DecrementCount input.count)

                        match res with
                        | Response.NewCount i -> input.callback i
                        | _ -> ()
                    }
                    |> Async.StartImmediate
            ]
        ]) 

    let inline bulmaCol (children: ReactElement list) =
        Html.div [
            prop.classes [ Bulma.Column ]
            prop.style [ style.padding (length.em 1) ]
            prop.children children
        ]

    let inline bulmaTextContainer (label: string) (children: ReactElement list) =
        Html.div [
            prop.classes [ Bulma.HasTextCentered ]
            prop.style [ style.paddingTop (length.em 5) ]
            prop.children [
                Html.div [
                    prop.classes [ Bulma.Container; Bulma.Box ]
                    prop.style [ style.maxWidth (length.em 12) ]
                    prop.children [
                        Html.text label
                    ]
                ]
                yield! children
            ]
        ]

    let render = React.functionComponent(fun () ->
        let count,setCount = React.useState 0
        let tickerCount,setTickerCount = React.useState 0
        
        let invokeCount,setInvokeCount = React.useState 0

        let setInvokeCount = React.useCallback(setInvokeCount, [||])

        let hub =
            React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response> <| fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .useMessagePack()
                    .onMessage(fun msg ->
                        match msg with
                        | Response.NewCount i -> setCount i
                        | Response.TickerCount i -> setTickerCount i
                    )
        
        Html.div [
            prop.classes [ Bulma.Container; Bulma.IsFullheight ]
            prop.children [
                Html.div [
                    prop.classes [ Bulma.Columns ]
                    prop.children [
                        bulmaCol [
                            display {| hub = hub |}
                        ]
                        bulmaCol [
                            bulmaTextContainer (sprintf "Count: %i" count) [
                                countButtons {| hub = hub; count = count |}
                            ]
                            bulmaTextContainer (sprintf "Invoked Count: %i" invokeCount) [
                                countInvokeButtons {| hub = hub; count = invokeCount; callback = setInvokeCount |}
                            ]
                            bulmaTextContainer (sprintf "Ticker Count: %i" tickerCount) []
                        ]
                    ]
                ]
            ]
        ])

    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
