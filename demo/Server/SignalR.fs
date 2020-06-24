namespace SignalRApp

module SignalRHub =
    open Fable.SignalR
    open FSharp.Control
    open SignalRHub
    open System.Collections.Generic
    open FSharp.Control.Tasks.V2

    let update (msg: Action) (hubContext: FableHub<Action,Response>) =
        printfn "New Msg: %A" msg
            
        match msg with
        | Action.SayHello -> Response.Howdy
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)
        | Action.RandomCharacter ->
            let characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            
            System.Random().Next(0,characters.Length-1)
            |> fun i -> characters.ToCharArray().[i]
            |> string
            |> Response.RandomCharacter
        |> hubContext.Clients.Caller.Send

    [<RequireQualifiedAccess>]
    module Stream =
        let sendToClient (msg: StreamFrom.Action) (hubContext: FableHub<Action,Response>) =
            printfn "New stream msg: %A" msg

            match msg with
            | StreamFrom.Action.GenInts ->
                Response.Howdy
                |> hubContext.Clients.Caller.Send
                |> Async.AwaitTask |> Async.Start
                asyncSeq {
                    for i in [ 1 .. 100 ] do
                        do! Async.Sleep 100
                        printfn "%i" i
                        yield StreamFrom.Response.GetInts i
                }
                |> AsyncSeq.toAsyncEnum

        let getFromClient (clientStream: IAsyncEnumerable<StreamTo.Action>) (hubContext: FableHub<Action,Response>) =
            printfn "New client stream: %A" clientStream

            AsyncSeq.ofAsyncEnum clientStream
            |> AsyncSeq.iterAsync (function | StreamTo.Action.GiveInt i -> hubContext.Clients.Caller.Send(Response.NewCount i) |> Async.AwaitTask)
            |> Async.StartAsTask
