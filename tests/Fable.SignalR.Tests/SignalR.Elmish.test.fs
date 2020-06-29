module SignalRElmish

open Browser.Types
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.ReactTestingLibrary
open Fable.SignalR
open SignalRApp
open SignalRApp.SignalRHub

Jest.describe("SignalR works with Elmish", fun () ->
    Jest.test("Can send messages to the hub", async {
        let render = RTL.render(Components.Elmish.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("text")).toHaveTextContent("")

        render.getByTestId("increment").click()
        render.getByTestId("random").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("1"))
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("text")).not.toHaveTextContent(""))

        render.getByTestId("decrement").click()

        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("0"))
    })

    Jest.test("Can invoke messages on the hub", async {
        let render = RTL.render(Components.InvokeElmish.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("text")).toHaveTextContent("")

        render.getByTestId("increment").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("1"))
        
        render.getByTestId("random").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("text")).not.toHaveTextContent(""))
        
        render.getByTestId("decrement").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("1"))
    })
)
