module SignalRElmish

open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.ReactTestingLibrary

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

    Jest.test("Invocation order does not matter", async {
        let render = RTL.render(Components.InvokeElmish.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("text")).toHaveTextContent("")

        render.getByTestId("increment").click()
        render.getByTestId("random").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("text")).not.toHaveTextContent(""))
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("1"))
    })

    Jest.test("Can stream from the server hub", async {
        let render = RTL.render(Components.StreamingElmish.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("server-stream-status")).toHaveTextContent("NotStarted")

        render.getByTestId("start-server-stream").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("10"))
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("server-stream-status")).toHaveTextContent("Finished"))
    })

    Jest.test("Can stream from the client hub", async {
        let render = RTL.render(Components.StreamingElmish.render())

        Jest.expect(render.getByTestId("client-stream-status")).toHaveTextContent("NotStarted")

        render.getByTestId("start-client-stream").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("client-stream-status")).toHaveTextContent("Finished"))
    })
)
