# Sending Messages

Sending messages to the server is quite painless with 
SignalR, and there are couple methods of doing so:

## Send

When you `send` a message to the SignalR server it does not
return any value. The response from the server is dispatched
after the server processes the mesage. The server may never
actually respond directly to your specific client, the hub 
could send them to a group of other clients, for example.

When you do get a message back, it is handled by the `onMessage`
handler is defined during initial configuration of your SignalR
connection.

### Elmish

The `Cmd` to send messages is `Cmd.SignalR.send`.

Here's how that would look:

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

let update msg model =
    match msg with
    | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
    | SignalRMsg rsp ->
        match rsp with
        | Response.Howdy -> model, Cmd.none
        | Response.RandomCharacter str ->
            { model with Text = str }, Cmd.none
        | Response.NewCount i ->
            { model with Count = i }, Cmd.none
    | IncrementCount ->
        model, Cmd.SignalR.send model.Hub (Action.IncrementCount model.Count)
    | DecrementCount ->
        model, Cmd.SignalR.send model.Hub (Action.DecrementCount model.Count)
    | RandomCharacter ->
        model, Cmd.SignalR.send model.Hub Action.RandomCharacter
    | SayHello ->
        model, Cmd.SignalR.send model.Hub Action.SayHello
```

### Feliz

Sending messages is as simple as calling `send` or `sendNow` from your hub:

```fsharp
let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
    React.fragment [
        Html.div input.count
        Html.div input.text
    ])

let buttons = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
    React.fragment [
        Html.button [
            prop.text "Increment"
            prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
        ]
        Html.button [
            prop.text "Decrement"
            prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
        ]
        Html.button [
            prop.text "Get Random Character"
            prop.onClick <| fun _ -> input.hub.current.sendNow Action.RandomCharacter
        ]
    ])

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

### Native

Same as the Feliz example, they both expose the same methods
for calling the SignalR hub.

## Invoke

Invoking with SignalR behaves like a traditional request, where it 
will return an async expression that resolves to the response type.

### Elmish

There are a few different `Cmd`s to invoke a hub method.
 * `Cmd.SignalR.attempt` - Maps only the error
 * `Cmd.SignalR.either` - Maps the success or error.
 * `Cmd.SignalR.perform` - Maps only the success.

Here's how that would look:

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
    | InvokeIncrementCount
    | OnInvokeError of exn
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

let update msg model =
    match msg with
    | RegisterHub hub -> { model with Hub = Some hub }, Cmd.none
    | SignalRMsg rsp ->
        match rsp with
        | Response.Howdy -> model, Cmd.none
        | Response.RandomCharacter str ->
            { model with Text = str }, Cmd.none
        | Response.NewCount i ->
            { model with Count = i }, Cmd.none
    | OnInvokeError e -> model,Cmd.none
    | InvokeIncrementCount ->
        model, Cmd.SignalR.either model.Hub (Action.IncrementCount model.Count) 
                SignalRMsg OnInvokeError
```

### Feliz

Sending messages is as simple as calling `invoke` from your hub:

```fsharp
let textDisplay = React.functionComponent(fun (input: {| count: int; text: string |}) ->
    React.fragment [
        Html.div input.count
        Html.div input.text
    ])

let buttons = React.functionComponent(fun (input: {| count: int; hub: Hub<Action,Response> |}) ->
    React.fragment [
        Html.button [
            prop.text "Increment"
            prop.onClick <| fun _ -> input.hub.current.sendNow (Action.IncrementCount input.count)
        ]
        Html.button [
            prop.text "Decrement"
            prop.onClick <| fun _ -> input.hub.current.sendNow (Action.DecrementCount input.count)
        ]
        Html.button [
            prop.text "Get Random Character"
            prop.onClick <| fun _ -> input.hub.current.sendNow Action.RandomCharacter
        ]
    ])

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

### Native

Same as the Feliz example, they both expose the same methods
for calling the SignalR hub.