module Tests

open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.ReactTestingLibrary
open HubComponents
open SignalRApp
open SignalRApp.SignalRHub

Jest.describe("Elmish SignalR", fun () ->
    let asserter msg (oldModel: Elmish.Model) (newModel: Elmish.Model) =
        match msg with
        | Elmish.RegisterHub hub -> 
            Jest.expect(hub).toEqual(newModel.Hub)
            Jest.expect(newModel.Hub).toBeDefined()
        | Elmish.SignalRMsg rsp ->
            match rsp with
            | Response.Howdy _ -> Jest.expect(newModel).toEqual(oldModel)
            | Response.RandomCharacter c -> Jest.expect(newModel.Text).toEqual(c)
            | Response.NewCount i ->
                Jest.expect(oldModel.Count).not.toBe(newModel.Count)
                Jest.expect(oldModel.Count).toBe(i)
        | Elmish.SignalRStreamMsg (StreamFrom.Response.GetInts i) ->
            Jest.expect(oldModel.SFCount).not.toBe(newModel.SFCount)
            Jest.expect(newModel.SFCount).toBe(i)
            Jest.expect(oldModel.SFCount).not.toBe(i)
        | Elmish.Subscription sub -> 
            Jest.expect(newModel.StreamSubscription).toBeDefined()
            Jest.expect(newModel.StreamSubscription).toBe(sub)
        | Elmish.StreamStatus ss -> Jest.expect(newModel.StreamStatus).toBe(ss)
        | Elmish.ClientStreamStatus ss -> Jest.expect(newModel.ClientStreamStatus).toBe(ss)
        | Elmish.IncrementCount
        | Elmish.DecrementCount
        | Elmish.RandomCharacter
        | Elmish.SayHello
        | Elmish.StartClientStream
        | Elmish.StartServerStream ->
            Jest.expect(newModel).toBe(expect.anything())

    Jest.test.elmish("Updates work properly", Elmish.init, Elmish.update, asserter)

)
