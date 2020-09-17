# External Calls

When you have external services that need to be able to 
dispatch messages to the hub clients you can use dependency 
injection to get a hub context in the form of a [FableHubCaller](api#fablehubcaller).

## Setup

There is no setup from the `Fable.SignalR` side of things.

The only thing you will need to do is require the service:

```fsharp
let getMyHubContext (services: IServiceProvider) =
    services.GetRequiredService<FableHubCaller<Action,Response>>()
```

## In Action

As a more concrete example:

```fsharp
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
```

All you need to do is add this to your `IServiceCollection` and
when the application starts and a user connects they will
automatically be dispatched messages on a five second interval.
