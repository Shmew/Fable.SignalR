namespace Fable.SignalR.DotNet.Tests

module Client =
    open Expecto
    open Microsoft.AspNetCore.TestHost
    open Microsoft.Extensions.Hosting
    open SignalRApp

    let host =
        App.app
            .ConfigureWebHost(fun webBuilder -> webBuilder.UseTestServer() |> ignore)
            .StartAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let testServer = host.GetTestServer()

    [<Tests>]
    let tests = 
        testList "Client" [ 
            //testPropertyP "DotNet SignalR client works" <| Generation.Commands.commandGen testServer
            //testPropertyP "DotNet SignalR client works with MsgPack" <| Generation.Commands.msgPackCommandGen testServer
            testPropertyP "DotNet SignalR client works with Akka" <| Generation.Commands.akkaCommandGen testServer
            testPropertyP "DotNet SignalR client works with Akka and MsgPack" <| Generation.Commands.akkaMsgPackCommandGen testServer
        ]
        //|> testSequenced
