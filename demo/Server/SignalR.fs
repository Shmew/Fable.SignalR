namespace SignalRApp

module SignalRHub =
    open Fable.SignalR
    open FSharp.Control
    open SignalRHub

    let invoke (msg: Action) =
        match msg with
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)

    let send (msg: Action) (hubContext: FableHub<Action,Response>) =
        invoke msg
        |> hubContext.Clients.Caller.Send

    [<RequireQualifiedAccess>]
    module Stream =
        let sendToClient (msg: StreamFrom.Action) (hubContext: FableHub<Action,Response>) =
            match msg with
            | StreamFrom.Action.AppleStocks ->
                let stocks = Stocks.appleStocks()
                asyncSeq {
                    for row in stocks do
                        do! Async.Sleep 25
                        yield StreamFrom.Response.AppleStock row
                }
                |> AsyncSeq.toAsyncEnum
