# Fable.SignalR [![Nuget](https://img.shields.io/nuget/v/Fable.SignalR.svg?maxAge=0&colorB=brightgreen&label=Fable.SignalR)](https://www.nuget.org/packages/Fable.SignalR)

Fable bindings for the SignalR client, and ASP.NET Core/Giraffe/Saturn wrappers for SignalR server hubs.

A quick look:

On the client:
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

On the server:

```fsharp
module SignalRHub =
    let update (msg: Action) =
        match msg with
        | Action.IncrementCount i -> Response.NewCount(i + 1)
        | Action.DecrementCount i -> Response.NewCount(i - 1)
        | Action.RandomCharacter ->
            let characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
            
            System.Random().Next(0,characters.Length-1)
            |> fun i -> characters.ToCharArray().[i]
            |> string
            |> Response.RandomCharacter

    let invoke (msg: Action) _ = update msg

    let send (msg: Action) (hubContext: FableHub<Action,Response>) =
        update msg
        |> hubContext.Clients.Caller.Send

application {
    use_signalr (
        configure_signalr {
            endpoint Endpoints.Root
            send SignalRHub.send
            invoke SignalRHub.invoke
        }
    )
    ...
}
```

The shared file:

```fsharp
[<RequireQualifiedAccess>]
type Action =
    | IncrementCount of int
    | DecrementCount of int
    | RandomCharacter

[<RequireQualifiedAccess>]
type Response =
    | NewCount of int
    | RandomCharacter of string

module Endpoints =
    let [<Literal>] Root = "/SignalR"
```
