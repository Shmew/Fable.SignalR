namespace SignalRApp

open Fable.SignalR
open FSharp.Control
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open SignalRApp
open System.Threading

type Ticker private (hub: FableHubCaller<SignalRHub.Action,SignalRHub.Response>) =
    let mutable state = 0
    let cts = new CancellationTokenSource()

    let ticking =
        AsyncSeq.intervalMs 5000
        |> AsyncSeq.map (fun _ -> 
            state <- state + 1
            state)
        |> AsyncSeq.iterAsync (fun i ->
            SignalRHub.Response.TickerCount i
            |> hub.Clients.All.Send
            |> Async.AwaitTask)

    interface IHostedService with
        member _.StartAsync ct =
            async { do Async.Start(ticking, cts.Token) }
            |> fun a -> upcast Async.StartAsTask(a, cancellationToken = ct)
        member _.StopAsync ct =
            async { do cts.Cancel() }
            |> fun a -> upcast Async.StartAsTask(a, cancellationToken = ct)

    static member Create (services: IServiceCollection) =
        services.AddHostedService<Ticker>(fun s -> 
            s.GetRequiredService<FableHubCaller<SignalRHub.Action,SignalRHub.Response>>() 
            |> Ticker)
