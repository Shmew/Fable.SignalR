module SignalR

open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.SignalR
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

type Msg =
    | ServerMsg of Response
    | ServerStreamMsg of StreamFrom.Response
    | SetStreamState of StreamStatus
    | SetClientStreamState of StreamStatus
    | GetState of (Model -> bool) * AsyncReplyChannel<Model>
    | SetState of (Model -> Model)
    | IsConnected of AsyncReplyChannel<bool>

type Hub = HubConnection<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>

let hub : Hub =
    SignalR.connect<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub ->
        hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None))

let msgPackHub : Hub =
    SignalR.connect<Action,StreamFrom.Action,StreamTo.Action,Response,StreamFrom.Response>(fun hub ->
        hub.withUrl("http://0.0.0.0:8085" + Endpoints.Root2)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None))

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
                        | IsConnected reply ->
                            async {
                                if hub.state = ConnectionState.Disconnected then 
                                    do! hub.start()

                                while hub.state <> ConnectionState.Connected do
                                    do! Async.Sleep 10

                                do reply.Reply(hub.state = ConnectionState.Connected)

                                return! loop state waitingState
                            }
                }

            loop { 
                Count = 0
                Text = ""
                SFCount = 0
                StreamState = StreamStatus.NotStarted
                StreamToState = StreamStatus.NotStarted 
            } []

    do hub.onMessage (ServerMsg >> mailbox.Post)

    member _.IsConnected () =  
        async {
            if hub.state = ConnectionState.Disconnected then 
                do! hub.start()
            return hub.state = ConnectionState.Connected  
        }

    member _.GetState (predicate: Model -> bool) = 
        mailbox.PostAndAsyncReply(fun reply -> GetState(predicate,reply))

    member this.SendIncrement () =
        async {
            let! initState = this.GetState(fun _ -> true)
            let initCount = initState.Count

            do!
                Action.IncrementCount initCount
                |> hub.send

            return! this.GetState(fun  m -> m.Count <> initCount)
        }
        
    member this.SendDecrement () =
        async {
            let! initState = this.GetState(fun _ -> true)
            let initCount = initState.Count

            do!
                Action.DecrementCount initCount
                |> hub.send

            return! this.GetState(fun  m -> m.Count <> initCount)
        }

    member this.InvokeIncrement () =
        async {
            let! initState = this.GetState(fun _ -> true)

            match! Action.IncrementCount initState.Count |> hub.invoke with
            | Response.NewCount i -> mailbox.Post(SetState(fun m -> { m with Count = i }))
            | _ -> ()

            return! this.GetState(fun _ -> true)
        }

    member this.InvokeDecrement () =
        async {
            let! initState = this.GetState(fun _ -> true)

            match! Action.DecrementCount initState.Count |> hub.invoke with
            | Response.NewCount i -> mailbox.Post(SetState(fun m -> { m with Count = i }))
            | _ -> ()

            return! this.GetState(fun _ -> true)
        }

    member this.StreamFrom () =
        async {
            mailbox.Post (SetStreamState StreamStatus.Streaming)

            let subscriber =
                { next = fun (msg: StreamFrom.Response) -> 
                    match msg with
                    | StreamFrom.Response.GetInts i ->
                        mailbox.Post(SetState(fun m -> { m with SFCount = m.SFCount + i }))
                  complete = fun () -> mailbox.Post(SetState(fun m -> { m with StreamState = StreamStatus.Finished }))
                  error = fun e -> mailbox.Post(SetState(fun m -> { m with StreamState = StreamStatus.Error e })) }

            let! streamResult = hub.streamFrom StreamFrom.Action.GenInts

            use sub =
                streamResult
                |> StreamResult.subscribe subscriber

            return! this.GetState(fun m -> m.StreamState = StreamStatus.Finished)
        }

    member this.StreamTo () =
        async {
            mailbox.Post (SetClientStreamState StreamStatus.Streaming)

            let subject = SignalR.subject()

            try
                do! hub.streamTo(subject)

                for i in [1..100] do
                    subject.next (StreamTo.Action.GiveInt i)
            
                subject.complete()
                mailbox.Post (SetClientStreamState StreamStatus.Finished)
            with e -> mailbox.Post (SetClientStreamState <| StreamStatus.Error(Some e))

            return! this.GetState(fun m -> m.StreamState <> StreamStatus.Streaming)
        }

module Commands =
    type SendIncrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! modelState = model.GetState(fun _ -> true)
                    let! newReal = real.SendIncrement()

                    Jest.expect(newReal.Count).toBeGreaterThan(modelState.Count)

                    do! model.SendIncrement() |> Async.Ignore
                }
            member _.toString () = "Send Increment"

    type SendDecrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! modelState = model.GetState(fun _ -> true)
                    let! newReal = real.SendDecrement()
                    
                    Jest.expect(newReal.Count).toBeLessThan(modelState.Count)

                    do! model.SendDecrement() |> Async.Ignore
                }
            member _.toString () = "Send Decrement"
    
    type InvokeIncrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! modelState = model.GetState(fun _ -> true)
                    let! newReal = real.InvokeIncrement()
                    
                    Jest.expect(newReal.Count).toBeGreaterThan(modelState.Count)

                    do! model.InvokeIncrement() |> Async.Ignore
                }
            member _.toString () = "Invoke Increment"

    type InvokeDecrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! modelState = model.GetState(fun _ -> true)
                    let! newReal = real.InvokeDecrement()
                    
                    Jest.expect(newReal.Count).toBeLessThan(modelState.Count)

                    do! model.InvokeDecrement() |> Async.Ignore
                }
            member _.toString () = "Invoke Decrement"

    type StreamFrom () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! modelState = model.GetState(fun _ -> true)
                    let! newReal = real.StreamFrom()
                    
                    Jest.expect(newReal.SFCount).toBe(modelState.SFCount + 5050)
                    Jest.expect(newReal.StreamState).toEqual(StreamStatus.Finished)
                    
                    do! model.StreamFrom() |> Async.Ignore
                }
            member _.toString () = "Stream From"
    
    type StreamTo () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    let! newReal = real.StreamTo()

                    Jest.expect(newReal.StreamToState).toEqual(StreamStatus.Finished)

                    do! model.StreamTo() |> Async.Ignore
                }
            member _.toString () = "Stream To"
    
let commandArb = Arbitrary.asyncCommands [
    Arbitrary.constant (Commands.SendIncrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.SendDecrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.InvokeIncrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.InvokeDecrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.StreamFrom() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.StreamTo() :> IAsyncCommand<HubModel,HubModel>)
]

Jest.describe("Native SignalR", fun () ->
    Jest.test.prop("Normal model tests run", commandArb, fun cmds ->
        FastCheck.asyncModelRun(HubModel(hub), HubModel(hub), cmds)
    , timeout = 60000)
    
    Jest.test.prop("MessagePack works", commandArb, fun cmds ->
        FastCheck.asyncModelRun(HubModel(msgPackHub), HubModel(msgPackHub), cmds)
    , timeout = 60000)

    Jest.test.prop("Can run two hubs at once", commandArb, commandArb, fun cmds1 cmds2 ->
        async {
            do! FastCheck.asyncModelRun(HubModel(hub), HubModel(hub), cmds1)
            do! FastCheck.asyncModelRun(HubModel(msgPackHub), HubModel(msgPackHub), cmds2)
        }
    , timeout = 60000)

    Jest.test("Can execute many invocations at once", async {
        let! res =
            [ 1 .. 50 ]
            |> List.map (fun _ -> 
                async {
                    let! msg = hub.invoke(Action.IncrementCount 0)

                    return
                        match msg with
                        | Response.NewCount i -> i
                        | _ -> failwith "Invalid server response"
                }
            )
            |> Async.Parallel

        Jest.expect(res |> Array.sum).toBe(50)
    })

    Jest.test("Can execute many invocations at once with MsgPack", async {
        let! res =
            [ 1 .. 50 ]
            |> List.map (fun _ -> 
                async {
                    let! msg = msgPackHub.invoke(Action.IncrementCount 0)

                    return
                        match msg with
                        | Response.NewCount i -> i
                        | _ -> failwith "Invalid server response"
                }
            )
            |> Async.Parallel

        Jest.expect(res |> Array.sum).toBe(50)
    })
)