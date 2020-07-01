namespace SignalRApp

module SignalRHub =
    open Fable.SignalR
    open FSharp.Control
    open Microsoft.AspNetCore.SignalR
    open SignalRHub
    open System.Collections.Generic
    open FSharp.Control.Tasks.V2   

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
            printfn "sendToClient called with %A" msg
            match msg with
            | StreamFrom.Action.AppleStocks ->
                let stocks = Stocks.appleStocks()
                asyncSeq {
                    for row in stocks do
                        do! Async.Sleep 300
                        yield StreamFrom.Response.AppleStock row
                }
                |> AsyncSeq.toAsyncEnum
