# Creating Connections

The first step to getting started with SignalR and Fable on 
the client is to create your connection.

The first thing you need to do is create a *shared* project
that models the messages you want to pass to and from the 
server.

For example:
```fsharp
[<RequireQualifiedAccess>]
type Action =
    | IncrementCount of int
    | DecrementCount of int
    | RandomCharacter
    | SayHello

[<RequireQualifiedAccess>]
type Response =
    | Howdy
    | NewCount of int
    | RandomCharacter of string

module Endpoints =
    let [<Literal>] Root = "/SignalR"
```

## Elmish

When using the `Fable.SignalR.Elmish` package you can add
a SignalR connection into your model like this:

```fsharp
type Model =
    { Count: int
      Text: string
      Hub: Elmish.Hub<Action,Response> option }

    // This only works if you're using Feliz.UseElmish or Feliz.ElmishComponents!
    //
    // If you're using the traditional elmish model you will need to clean this up yourself.
    interface System.IDisposable with
        member this.Dispose () =
            this.Hub |> Option.iter (fun hub -> hub.Dispose())

type Msg =
    | SignalRMsg of Response
    | IncrementCount
    | DecrementCount
    | RandomCharacter
    | SayHello
    | RegisterHub of Elmish.Hub<Action,Response>

let init =
    { Count = 0
        Text = ""
        Hub = None }
    , Cmd.SignalR.connect RegisterHub (fun hub -> 
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug)
            .onMessage SignalRMsg)
```

## Feliz

The `Fable.SignalR.Feliz` package exposes SignalR as a hook, and has the
least ceremony of the three options:

```fsharp
let render = React.functionComponent(fun () ->
    let count,setCount = React.useState 0
    let text,setText = React.useState ""

    let hub =
        React.useSignalR<Action,Response>(fun hub -> 
            hub.withUrl(Endpoints.Root)
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Debug)
                .onMessage <|
                    function
                    | Response.Howdy -> JS.console.log("Howdy!")
                    | Response.NewCount i -> setCount i
                    | Response.RandomCharacter str -> setText str
        )
            
    Html.div [
        prop.children [
            textDisplay {| count = count; text = text |}
            buttons {| count = count; hub = hub |}
        ]
    ])
```

## Native

If you're not using either of the extension packages then you
will simply create the connection like this:

```fsharp
let hub =
    SignalR.connect<Action,_,_,Response,_>(fun hub ->
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Debug)
            .onMessage <|
                function
                | Response.Howdy -> JS.console.log("Howdy!")
                | Response.NewCount i -> JS.console.log(i)
                | Response.RandomCharacter str -> JS.console.log(str))
```

Then you will need to start the connection: 

```fsharp
hub.startNow()
```

Managing the life-cycle of the connection, as well as
message handling will be up to you after this point.
