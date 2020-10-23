namespace Fable.SignalR.DotNet.Tests

module Generation =
    open FsCheck
    open Microsoft.AspNetCore.TestHost

    module Model =
        open Fable.SignalR
        open FSharp.Control
        open Microsoft.AspNetCore.SignalR.Client
        open Microsoft.Extensions.Logging
        open SignalRApp
        open SignalRApp.SignalRHub

        [<RequireQualifiedAccess>]
        type StreamStatus =
            | NotStarted
            | Error of exn option
            | Streaming
            | Finished

        type Model =
            { Count: int
              Text: string
              SFCount: int
              StreamState: StreamStatus
              StreamToState: StreamStatus }

            static member empty =
                { Count = 0
                  Text = ""
                  SFCount = 0
                  StreamState = StreamStatus.NotStarted
                  StreamToState = StreamStatus.NotStarted }

        type Msg =
            | ServerMsg of Response
            | ServerStreamMsg of StreamFrom.Response
            | SetStreamState of StreamStatus
            | SetClientStreamState of StreamStatus
            | GetState of (Model -> bool) * AsyncReplyChannel<Model>
            | SetState of (Model -> Model)

        type Hub = HubConnection<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>

        let hub (server: TestServer) : Hub =
            SignalR.Connect<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub ->
                hub.WithUrl("http://localhost:8085" + Endpoints.Root, fun o -> o.HttpMessageHandlerFactory <- (fun _ -> server.CreateHandler()))
                    .WithAutomaticReconnect()
                    .ConfigureLogging(fun logBuilder -> logBuilder.SetMinimumLevel(LogLevel.None)))

        let msgPackHub (server: TestServer) : Hub =
            SignalR.Connect<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub ->
                hub.WithUrl("http://localhost:8085" + Endpoints.Root2, fun o -> o.HttpMessageHandlerFactory <- (fun _ -> server.CreateHandler()))
                    .WithAutomaticReconnect()
                    .ConfigureLogging(fun logBuilder -> logBuilder.SetMinimumLevel(LogLevel.None)))

        type HubModel (hub: Hub) =
            let replyIfNew newState (waiting: ((Model -> bool) * AsyncReplyChannel<Model>) list) =
                waiting
                |> List.choose (fun (pred,reply) -> 
                    if pred newState then
                        reply.Reply newState
                        None
                    else Some(pred,reply))

            let mailbox =
                MailboxProcessor.Start <| fun inbox ->
                    let rec loop (state: Model) (waitingState: ((Model -> bool) * AsyncReplyChannel<Model>) list) =
                        async {
                            let! msg = inbox.Receive()

                            return!
                                match msg with
                                | ServerMsg msg ->
                                    match msg with
                                    | Response.NewCount i -> { state with Count = i }
                                    | Response.RandomCharacter str -> { state with Text = str }
                                    |> fun newState ->
                                        replyIfNew newState waitingState
                                        |> loop newState
                                | ServerStreamMsg msg ->
                                    match msg with
                                    | StreamFrom.Response.GetInts i ->
                                        let newState = { state with SFCount = i }

                                        replyIfNew newState waitingState
                                        |> loop newState
                                | SetStreamState streamState ->
                                    loop { state with StreamState = streamState } waitingState
                                | SetClientStreamState streamState ->
                                    loop { state with StreamToState = streamState } waitingState
                                | GetState (pred,reply) -> 
                                    if pred state then 
                                        reply.Reply(state)
                                        waitingState
                                    else (pred,reply)::waitingState
                                    |> loop state
                                | SetState newState ->
                                    let newState = newState state

                                    replyIfNew newState waitingState
                                    |> loop newState
                        }

                    loop { 
                        Count = 0
                        Text = ""
                        SFCount = 0
                        StreamState = StreamStatus.NotStarted
                        StreamToState = StreamStatus.NotStarted 
                    } []

            let _ = hub.OnMessage (ServerMsg >> mailbox.Post >> Async.lift)

            member _.IsConnected () =  
                async {
                    if hub.State = HubConnectionState.Disconnected then 
                        do! hub.Start()
                    return hub.State = HubConnectionState.Connected  
                }

            member _.GetState (predicate: Model -> bool) = 
                mailbox.PostAndAsyncReply(fun reply -> GetState(predicate,reply))

            member this.SendIncrement () =
                async {
                    let! initState = this.GetState(fun _ -> true)
                    let initCount = initState.Count

                    do!
                        Action.IncrementCount initCount
                        |> hub.Send

                    return! this.GetState(fun  m -> m.Count <> initCount)
                }
        
            member this.SendDecrement () =
                async {
                    let! initState = this.GetState(fun _ -> true)
                    let initCount = initState.Count

                    do!
                        Action.DecrementCount initCount
                        |> hub.Send

                    return! this.GetState(fun  m -> m.Count <> initCount)
                }

            member this.InvokeIncrement () =
                async {
                    let! initState = this.GetState(fun _ -> true)

                    match! Action.IncrementCount initState.Count |> hub.Invoke with
                    | Response.NewCount i -> mailbox.Post(SetState(fun m -> { m with Count = i }))
                    | _ -> ()

                    return! this.GetState(fun _ -> true)
                }

            member this.InvokeDecrement () =
                async {
                    let! initState = this.GetState(fun _ -> true)

                    match! Action.DecrementCount initState.Count |> hub.Invoke with
                    | Response.NewCount i -> mailbox.Post(SetState(fun m -> { m with Count = i }))
                    | _ -> ()

                    return! this.GetState(fun _ -> true)
                }

            member this.StreamFrom () =
                async {
                    mailbox.Post (SetStreamState StreamStatus.Streaming)

                    try
                        let! stream = hub.StreamFrom StreamFrom.Action.GenInts
                        
                        do!
                            stream
                            |> AsyncSeq.ofAsyncEnum
                            |> AsyncSeq.iter (fun msg ->
                                match msg with
                                | StreamFrom.Response.GetInts i ->
                                    mailbox.Post(SetState(fun m -> { m with SFCount = m.SFCount + i }))
                            )

                        mailbox.Post(SetState(fun m -> { m with StreamState = StreamStatus.Finished }))

                    with e -> mailbox.Post(SetState(fun m -> { m with StreamState = StreamStatus.Error (Some e) }))
                    
                    return! this.GetState(fun m -> m.StreamState = StreamStatus.Finished)
                }

            member this.StreamTo () =
                async {
                    mailbox.Post (SetClientStreamState StreamStatus.Streaming)

                    try
                        let stream =
                            asyncSeq {
                                for i in [1..100] do
                                    yield StreamTo.Action.GiveInt i
                            }
                            |> AsyncSeq.toAsyncEnum
            
                        do! hub.StreamTo stream
                        
                        mailbox.Post (SetClientStreamState StreamStatus.Finished)
                    with e -> mailbox.Post (SetClientStreamState <| StreamStatus.Error(Some e))

                    return! this.GetState(fun m -> m.StreamState <> StreamStatus.Streaming)
                }

    module Commands =
        type SendIncrement () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.SendIncrement()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = { m with Count = m.Count + 1 }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Send Increment"
        
        type SendDecrement () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.SendDecrement()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = { m with Count = m.Count - 1 }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Send Decrement"
            
        type InvokeIncrement () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.InvokeIncrement()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = { m with Count = m.Count + 1 }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Invoke Increment"

        type InvokeDecrement () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.InvokeDecrement()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = { m with Count = m.Count - 1 }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Invoke Decrement"

        type StreamFrom () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.StreamFrom()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = 
                { m with 
                    SFCount = m.SFCount + 55
                    StreamState = Model.StreamStatus.Finished }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Stream From"
            
        type StreamTo () =
            inherit Command<Model.HubModel, Model.Model>()

            override _.RunActual hm = 
                hm.StreamTo()
                |> Async.Ignore
                |> Async.RunSynchronously

                hm

            override _.RunModel m = { m with StreamToState = Model.StreamStatus.Finished }

            override _.Post (hm, m) = 
                async {
                    let! actual = hm.GetState(fun _ -> true)

                    return actual = m |@ sprintf "model: %A <> %A" actual m 
                }
                |> Async.RunSynchronously

            override _.ToString() = "Stream To"

        let commandGen (server: TestServer) =
            { new ICommandGenerator<Model.HubModel, Model.Model> with
                member _.InitialActual = 
                    let actual = Model.HubModel(Model.hub server)
                    
                    actual.IsConnected()
                    |> Async.RunSynchronously
                    |> function
                    | true -> ()
                    | false -> failwith "Hub not running"

                    actual

                member _.InitialModel = Model.Model.empty
                member _.Next _ = 
                    Gen.elements [
                        SendIncrement()
                        SendDecrement()
                        InvokeIncrement()
                        InvokeDecrement()
                        StreamFrom()
                        StreamTo()
                    ] }
            |> Command.toProperty

        let msgPackCommandGen (server: TestServer) =
            { new ICommandGenerator<Model.HubModel, Model.Model> with
                member _.InitialActual = 
                    let actual = Model.HubModel(Model.msgPackHub server)

                    actual.IsConnected()
                    |> Async.RunSynchronously
                    |> function
                    | true -> ()
                    | false -> failwith "Hub not running"

                    actual

                member _.InitialModel = Model.Model.empty
                member _.Next _ = 
                    Gen.elements [
                        SendIncrement()
                        SendDecrement()
                        InvokeIncrement()
                        InvokeDecrement()
                        StreamFrom()
                        StreamTo()
                    ] }
            |> Command.toProperty
