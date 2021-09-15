namespace SignalRApp

module SignalRHub =
    open System.Threading
    open Fable.SignalR
    open FSharp.Control
    open FSharp.Control.Tasks.V2
    open SignalRHub

    let update (msg: Action) =
        match msg with
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)

    let invoke (msg: Action) (hubContext: FableHub) =
        task { return update msg }

    let send (msg: Action) (hubContext: FableHub<Action,Response>) =
        update msg
        |> hubContext.Clients.Caller.Send

    [<RequireQualifiedAccess>]
    module Stream =
        let sendToClient (msg: StreamFrom.Action) (hubContext: FableHub<Action,Response>) (_: CancellationToken) =
            match msg with
            | StreamFrom.Action.AppleStocks ->
                Stocks.appleStocks
                |> AsyncSeq.mapAsync (fun stock ->
                    async {
                        do! Async.Sleep 25
                        return StreamFrom.Response.AppleStock stock
                    }
                )
                |> AsyncSeq.toAsyncEnum
