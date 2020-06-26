module SignalR

open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.SignalR
open SignalRApp
open SignalRApp.SignalRHub

type Model =
    { Count: int
      Text: string }

let hub : HubConnection<Action,unit,unit,Response,unit> =
    SignalR.connect<Action,_,_,Response,_>(fun hub ->
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Critical))

type HubModel () =
    let mutable state =
        { Count = 0
          Text = "" }

    do 
        hub.onMessage <|
            function
            | Response.Howdy -> ()
            | Response.NewCount i -> state <- { state with Count = i }
            | Response.RandomCharacter str -> state <- { state with Text = str }
        
        if hub.state = ConnectionState.Disconnected then hub.startNow()

    member _.IsConnected () =  async { return hub.state = ConnectionState.Connected  }

    member _.State = state

    member _.SendIncrement () =
        async {
            let initCount = state.Count
            
            do!
                Action.IncrementCount state.Count
                |> hub.send
            
            while state.Count = initCount do 
                do! Async.Sleep 10
        }
        
    member _.SendDecrement () =
        async {
            let initCount = state.Count
            
            do!
                Action.DecrementCount state.Count
                |> hub.send
            
            while state.Count = initCount do 
                do! Async.Sleep 10
        }

    member _.InvokeIncrement () =
        async {
            let! msg = Action.IncrementCount state.Count |> hub.invoke

            match msg with
            | Response.NewCount i -> state <- { state with Count = i }
            | _ -> ()
        }

    member _.InvokeDecrement () =
        async {
            let! msg = Action.DecrementCount state.Count |> hub.invoke

            match msg with
            | Response.NewCount i -> state <- { state with Count = i }
            | _ -> ()
        }

module Commands =
    type SendIncrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    do! real.SendIncrement()
                    Jest.expect(real.State.Count).toBeGreaterThan(model.State.Count)
                    do! model.SendIncrement()
                }
            member _.toString () = "Send Increment"

    type SendDecrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    do! real.SendDecrement()
                    Jest.expect(real.State.Count).toBeLessThan(model.State.Count)
                    do! model.SendDecrement()
                }
            member _.toString () = "Send Decrement"
    
    type InvokeIncrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    do! real.InvokeIncrement()
                    Jest.expect(real.State.Count).toBeGreaterThan(model.State.Count)
                    do! model.InvokeIncrement()
                }
            member _.toString () = "Invoke Increment"

    type InvokeDecrement () =
        interface IAsyncCommand<HubModel,HubModel> with
            member _.check (model: HubModel) = model.IsConnected()
            member _.run (model: HubModel, real: HubModel) =
                async {
                    do! real.InvokeDecrement()
                    Jest.expect(real.State.Count).toBeLessThan(model.State.Count)
                    do! model.InvokeDecrement()
                }
            member _.toString () = "Invoke Decrement"
    
let commandArb = Arbitrary.asyncCommands [
    Arbitrary.constant (Commands.SendIncrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.SendDecrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.InvokeIncrement() :> IAsyncCommand<HubModel,HubModel>)
    Arbitrary.constant (Commands.InvokeDecrement() :> IAsyncCommand<HubModel,HubModel>)
]

Jest.test.prop("Hub model tests", commandArb, fun cmds ->
    FastCheck.asyncModelRun(HubModel(), HubModel(), cmds)
)
