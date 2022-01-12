namespace Fable.SignalR.DotNet.Tests

open Microsoft.AspNetCore.SignalR.Client

module Client =
    open BlackFox.CommandLine
    open Expecto
    open Fable.SignalR
    open Fake.Core
    open Fake.IO.FileSystemOperators
    open Fake.JavaScript
    open Microsoft.AspNetCore.TestHost
    open Microsoft.Extensions.Hosting
    open SignalRApp
    open System.Diagnostics

    let clientTests =
        testAsync "Client" {
            let! host =
                App.app
                    .ConfigureWebHost(fun webBuilder -> webBuilder.UseTestServer() |> ignore)
                    .StartAsync()
                |> Async.AwaitTask

            try
                let testServer = host.GetTestServer()

                do! checkProp "DotNet SignalR client works" (Generation.Commands.commandGen testServer)
                do! checkProp "DotNet SignalR client works with MsgPack" (Generation.Commands.msgPackCommandGen testServer)
            finally host.Dispose()
        }

    let jsTests =
        let rec checkHeartbeat (hub: HubConnection<_,_,_,_,_>) retries =
            async {
                printfn "Checking hub state"

                match hub.State with
                | HubConnectionState.Connected -> return ()
                | _ -> 
                    try
                        printfn "Attempting to start hub"
                        do! hub.Start()
                    with e ->
                        if retries > 4 then
                            printfn "%s" e.Message
                    
                    printfn "Moving on"

                    do! Async.Sleep 5000

                    return! checkHeartbeat hub (retries + 1)
            }
            
        testAsync "JS Tests" {
            let heartbeatHub =
                SignalR.Connect<unit,unit,unit,unit,unit>(fun hub -> hub.WithUrl("http://localhost:8085" + Endpoints.Root))
            
            let mutable ps = None : Process option

            try
                CmdLine.empty
                |> CmdLine.appendRaw "run"
                #if NET6_0
                |> CmdLine.appendRaw "-f net6.0"
                #endif
                #if NET5_0
                |> CmdLine.appendRaw "-f net5.0"
                #endif
                #if NETCOREAPP3_1
                |> CmdLine.appendRaw "-f netcoreapp3.1"
                #endif
                |> CmdLine.toString
                |> CreateProcess.fromRawCommandLine "dotnet"
                |> CreateProcess.withWorkingDirectory (__SOURCE_DIRECTORY__ @@ ".." @@ "Fable.SignalR.TestServer")
                |> CreateProcess.ensureExitCodeWithMessage "TestServer bootstrap failed."
                |> CreateProcess.appendSimpleFuncs ignore (fun state proc -> ps <- Some proc) (fun _ _ _ -> async.Return ()) ignore
                |> Proc.startAndAwait
                |> Async.Ignore
                |> Async.Start

                do! checkHeartbeat heartbeatHub 0

                do! heartbeatHub.Stop()

                try
                    Yarn.exec "test" <| fun p ->
                        { p with WorkingDirectory = __SOURCE_DIRECTORY__ @@ ".." @@ ".." }
                    Ok ()
                with _ -> Error "Jest tests failed"
                |> Expect.isOk 
                <| ""
            finally 
                (heartbeatHub :> System.IDisposable).Dispose()
                Option.iter (fun (proc: Process) -> proc.Kill true) ps
                Process.killAllCreatedProcesses()
        }

    [<Tests>]
    let tests =
        testList "SignalR" [
            clientTests
            //jsTests
        ]
        |> testSequenced
