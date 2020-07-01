namespace SignalRApp

module App =
    open Elmish
    open Fable.Core
    open Fable.SignalR
    open Fable.SignalR.Elmish
    open Feliz
    open Feliz.Plotly
    open Feliz.UseElmish
    open SignalRHub
    open System

    type Hub = StreamHub.ServerToClient<Action,StreamFrom.Action,Response,StreamFrom.Response>
        
    let display = React.functionComponent(fun (input: {| hub: Hub |}) ->
        let dates,setDates = React.useState([] : DateTime list)
        let lows,setLows = React.useState([]: float list)
        let highs,setHighs = React.useState([] : float list)
        let subscription = React.useRef(None : ISubscription option)

        let subscriber = 
            { next = fun (msg: StreamFrom.Response) -> 
                match msg with
                | StreamFrom.Response.AppleStock s ->
                    setDates(dates @ [ s.Date ])
                    setLows(lows @ [ s.Low ])
                    setHighs(highs @ [ s.High ])
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
                    title.text "AppleStock Historical Data"
                ]
                layout.xaxis [
                    xaxis.range [ DateTime(2015, 2, 17); DateTime(2017, 2, 16) ]
                    xaxis.type'.date
                ]
                layout.yaxis [
                    yaxis.range [ 86.8700008333; 138.870004167 ]
                    yaxis.type'.linear
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
        
    let render = React.functionComponent(fun () ->
        let hub =
            React.useSignalR<Action,StreamFrom.Action,Response,StreamFrom.Response>(fun hub -> 
                hub.withUrl(Endpoints.Root)
                    .withAutomaticReconnect()
                    .configureLogging(LogLevel.Debug)
                    .onMessage <| printfn "%A"
            )
        
        Html.div [
            prop.children [
                display {| hub = hub |}
            ]
        ])

    ReactDOM.render(render, Browser.Dom.document.getElementById "app")
