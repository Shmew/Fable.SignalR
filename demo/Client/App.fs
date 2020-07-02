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
        
    let display = React.functionComponent(fun (input: {| hub: Hub |}) ->
        let dates,setDates = React.useState([] : System.DateTime list)
        let lows,setLows = React.useState([]: float list)
        let highs,setHighs = React.useState([] : float list)
        let subscription = React.useRef(None : ISubscription option)
        
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
        
        React.useEffectOnce(fun () ->
            async {
                let! streamResult =
                    StreamFrom.Action.AppleStocks
                    |> input.hub.current.streamFrom

                subscription.current <- Some (streamResult.subscribe(subscriber))
            }
            |> Async.StartImmediate
            
            React.createDisposable(fun () -> subscription.current |> Option.iter(fun s -> s.dispose()))
        )
                    
        Plotly.plot [
            plot.traces [
                traces.scatter [
                    scatter.mode.lines
                    scatter.name "AAPL High"
                    scatter.x dates
                    scatter.y highs
                    scatter.line [
                        line.color "#17BECF"
                    ]
                ]
                traces.scatter [
                    scatter.mode.lines
                    scatter.name "AAPL Low"
                    scatter.x dates
                    scatter.y lows
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

    let inline bulmaCol (children: ReactElement list) =
        Html.div [
            prop.classes [ Bulma.Column ]
            prop.style [ style.padding (length.em 1) ]
            prop.children children
        ]

    let render = React.functionComponent(fun () ->
        let count,setCount = React.useState 0

        let hub =
            React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response> <| fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .onMessage(fun (Response.NewCount i) -> setCount i)
        
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
                            Html.div [
                                prop.classes [ Bulma.HasTextCentered ]
                                prop.style [ style.paddingTop (length.em 5) ]
                                prop.children [
                                    Html.div [
                                        prop.classes [ Bulma.Container; Bulma.Box ]
                                        prop.style [ style.maxWidth (length.em 12) ]
                                        prop.children [
                                            Html.textf "Count: %i" count
                                        ]
                                    ]
                                    countButtons {| hub = hub; count = count |}
                                ]   
                            ]
                        ]
                    ]
                ]
            ]
        ])

    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
