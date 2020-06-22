namespace SignalRApp

module SignalRHub =
    open Fable.SignalR
    open FSharp.Control
    open SignalRHub

    let update (msg: Action) (hubContext: FableHub<Action,Response>) =
        printfn "New Msg: %A" msg

        match msg with
        | Action.SayHello ->
            Response.Howdy
            |> hubContext.Clients.Caller.Send
        | Action.IncrementCount i ->
            Response.NewCount(i + 1)
            |> hubContext.Clients.Caller.Send
        | Action.DecrementCount i ->
            Response.NewCount(i - 1)
            |> hubContext.Clients.Caller.Send
        | Action.RandomCharacter ->
            let characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

            System.Random().Next(0,characters.Length-1)
            |> fun i -> characters.ToCharArray().[i]
            |> string
            |> Response.RandomCharacter
            |> hubContext.Clients.Caller.Send
        | _ -> failwith "bad"

    [<RequireQualifiedAccess>]
    module Stream =
        open FSharp.Control

        let update (msg: Action) (hubContext: FableHub<Action,Response>) =
            printfn "New stream msg: %A" msg

            match msg with
            | Action.GetInts ->
                Response.Howdy
                |> hubContext.Clients.Caller.Send
                |> Async.AwaitTask |> Async.Start
                asyncSeq {
                    for i in [ 1 .. 100 ] do
                        do! Async.Sleep 100
                        printfn "%i" i
                        yield Response.GetInts i
                }
                |> AsyncSeq.toAsyncEnum
            | _ -> failwith "Invalid"