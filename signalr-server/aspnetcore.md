# SignalR with ASP.NET Core and Giraffe

The use of ASP.NET Core and Giraffe is a bit
more involved than Saturn, but is only a couple extra
steps of configuration.

## Setting up a basic hub

To get started with the core functionality of SignalR
you only need to take a few steps.

### Define your domain

Firstly you will want to created a *shared* project that will
contain your shared message data structure.

For example:

```fsharp
namespace SignalRApp

module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int

    [<RequireQualifiedAccess>]
    type Response =
        | NewCount of int

module Endpoints =   
    let [<Literal>] Root = "/SignalR"
```

### Handler functions

Once you have a shared model, it's fine to define how your
hub will behave.

There are two functions you will always need to provide when
creating a hub:
* invoke - A function that takes a client message (`Action`) and outputs the server response.
* send - A function that given a client message (`Action`) and hub context and (maybe) responds 
(with a `Response`).

Following our example these would look like this:

```fsharp
module SignalRHub =
    open Fable.SignalR
    open FSharp.Control.Tasks.V2
    open SignalRHub

    let update (msg: Action) =
        match msg with
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)

    let invoke (msg: Action) (services: System.IServiceProvider) =
        task { return update msg }

    let send (msg: Action) (hubContext: FableHub<Action,Response>) =
        update msg
        |> hubContext.Clients.Caller.Send
```

### Adding it to the application

Now that you have a shared model and defined the behavior of your hub, all
that's left is to add it to the application.

It's easiest if you go ahead and define your configuration instead of inline it
in the fluent builders (but they support all of the overloads should you want to do so):

```fsharp
let mySignalRConfig =
    { EndpointPattern = Endpoints.Root
      Send = SignalRHub.send
      Invoke = SignalRHub.invoke 
      Config = None }
```

#### IServiceCollection

You will need to add SignalR to your `IServiceCollection`:

```fsharp
let myConfig serviceCollection =
    serviceCollection.AddSignalR(mySignalRConfig)
```

#### IApplicationBuilder

Lastly you will want to also add it to the `IApplicationBuilder`:

```fsharp
let myApp appBuilder =
    appBuilder.UseSignalR(mySignalRConfig)
```

That's it! You can now call your hub from the Fable client.

## Adding streaming

Similar to above, adding streaming is as easy as extending the steps we've
already done.

### Extend your domain

We need to add the new behavior in our shared model:

```fsharp
module SignalRHub =
    [<RequireQualifiedAccess>]
    type Action =
        | IncrementCount of int
        | DecrementCount of int

    [<RequireQualifiedAccess>]
    type Response =
        | NewCount of int

    // Streaming from the server
    module StreamFrom =
        [<RequireQualifiedAccess>]
        type Action =
            | GenInts
        
        [<RequireQualifiedAccess>]
        type Response =
            | GetInts of int

    // Streaming to the server
    module StreamTo =
        [<RequireQualifiedAccess>]
        type Action =
            | GiveInt of int

module Endpoints =   
    let [<Literal>] Root = "/SignalR"
```

### Add stream handler functions

If you want to support streaming either from the client and/or the
server you need to define the behavior you want:
* Streaming from - A function that takes a streaming message (`StreamFrom.Action`) 
and hub context that then returns an `IAsyncEnumerable<StreamFrom.Response>`.
* Streaming to - A function that takes a `IAsyncEnumerable<StreamTo.Action>` and a hub context
and then (maybe) responds (with a `Response`).

Following our example the module would now look like this:

<Note type="tip">This is using the [FSharp.Control.AsyncSeq](https://github.com/fsprojects/FSharp.Control.AsyncSeq) library</Note>

```fsharp
module SignalRHub =
    open Fable.SignalR
    open FSharp.Control
    open FSharp.Control.Tasks.V2
    open SignalRHub
    open System.Collections.Generic

    let update (msg: Action) =
        match msg with
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)

    let invoke (msg: Action) (services: System.IServiceProvider) =
        task { return update msg }

    let send (msg: Action) (hubContext: FableHub<Action,Response>) =
        update msg
        |> hubContext.Clients.Caller.Send

    [<RequireQualifiedAccess>]
    module Stream =
        let sendToClient (msg: StreamFrom.Action) (hubContext: FableHub<Action,Response>) =
            match msg with
            | StreamFrom.Action.GenInts ->
                asyncSeq {
                    for i in [ 1 .. 100 ] do
                        yield StreamFrom.Response.GetInts i
                }
                |> AsyncSeq.toAsyncEnum

        let getFromClient (clientStream: IAsyncEnumerable<StreamTo.Action>) 
            (hubContext: FableHub<Action,Response>) =
            
            AsyncSeq.ofAsyncEnum clientStream
            |> AsyncSeq.iterAsync (function 
                | StreamTo.Action.GiveInt i -> 
                    hubContext.Clients.Caller.Send(Response.NewCount i) 
                    |> Async.AwaitTask)
            |> Async.StartAsTask
```

### Adding it to the application

Now that we've extended our model and defined our behavior we just
modify our configurations a bit and we're good to go!

#### IServiceCollection

Adjusting our `IServiceCollection` config to:

```fsharp
let myConfig serviceCollection =
    serviceCollection.AddSignalR (
        mySignalRConfig, 
        SignalRHub.Stream.sendToClient, 
        SignalRHub.Stream.getFromClient
    )
```

#### IApplicationBuilder

Adjusting our `IApplicationBuilder` config to:

```fsharp
let myApp appBuilder =
    appBuilder.UseSignalR (
        mySignalRConfig, 
        SignalRHub.Stream.sendToClient, 
        SignalRHub.Stream.getFromClient
    )
```

That's it! You can now call your hub from the Fable client.