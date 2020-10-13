module SignalRFeliz

open Fable.FastCheck
open Fable.FastCheck.Jest
open Fable.Jester
open Fable.ReactTestingLibrary

Jest.describe("SignalR works with Feliz", fun () ->
    Jest.test("Can send messages to the hub", async {
        let render = RTL.render(Components.Hook.render())

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
        let render = RTL.render(Components.InvokeHook.render())

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
        let render = RTL.render(Components.InvokeHook.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("text")).toHaveTextContent("")

        render.getByTestId("increment").click()
        render.getByTestId("random").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("text")).not.toHaveTextContent(""))
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("1"))
    })

    Jest.test("Can stream from the server hub", async {
        let render = RTL.render(Components.StreamingHook.render())

        Jest.expect(render.getByTestId("count")).toHaveTextContent("0")
        Jest.expect(render.getByTestId("server-complete")).toHaveTextContent("false")

        render.getByTestId("start-server-stream").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("count")).toHaveTextContent("100"))
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("server-complete")).toHaveTextContent("true"))
    })

    Jest.test("Can stream from the client hub", async {
        let render = RTL.render(Components.StreamingHook.render())

        Jest.expect(render.getByTestId("client-complete")).toHaveTextContent("false")

        render.getByTestId("start-client-stream").click()
        
        do! RTL.waitFor(fun () -> Jest.expect(render.getByTestId("client-complete")).toHaveTextContent("true"))
    })

    Jest.test("Can stream twice at once", async {
        let render = RTL.render(Components.DoubleStream.render())

        do! RTL.waitFor((fun () -> Jest.expect(render.getByTestId("server-complete")).toHaveTextContent("true")), [ waitForOption.timeout 10000 ])
        do! RTL.waitFor((fun () -> Jest.expect(render.getByTestId("server-complete2")).toHaveTextContent("true")), [ waitForOption.timeout 10000 ])

        Jest.expect(render.getByTestId("count")).toHaveTextContent("100")
        Jest.expect(render.getByTestId("count2")).toHaveTextContent("100")
    })
)
