# Dependency Injection

Let's say we have this class we want to inject:

```fsharp
type MyAdder () =
    member _.AddOne (x: int) = x + 1
    member _.SubtractOne (x: int) = x - 1
```

## Setup

Just like with any DI in ASP.NET Core, you will need to 
add it to your service collection during your application configuration first.

### Giraffe

```fsharp
let configServices (services: IServiceCollection) =
    services.AddSingleton<MyAdder>()
```

### Saturn

```fsharp
application {
    ...
    service_config (fun s -> s.AddSingleton<MyAdder>())
    ...
}
```

## Hub Functions

Getting registered services inside your hub functions
can be done in two ways:

### Invoke

The invoke function already injects a `System.IServiceProvider`
which is why in previous examples you might have noticed this:

```fsharp
let update (msg: Action) =
    match msg with
    | Action.IncrementCount i -> Response.NewCount(i + 1)
    | Action.DecrementCount i -> Response.NewCount(i - 1)

let invoke (msg: Action) _ = update msg
```

The second parameter for invoke is a `System.IServiceProvider`, so if
you need a service you can do this:

```fsharp
// Exposes the type extensions for System.IServiceProvider
open Microsoft.Extensions.DependencyInjection

let update (msg: Action) (adder: MyAdder) =
    match msg with
    | Action.IncrementCount i -> Response.NewCount(adder.AddOne i)
    | Action.DecrementCount i -> Response.NewCount(adder.SubtractOne i)

let invoke (msg: Action) (services: System.IServiceProvider) =
    services.GetService<MyAdder>()
    |> update msg
```

### FableHub

All other hub functions have a `FableHub<_,_>` as a parameter. This
interface has a property `Services` which exposes the `System.IServiceProvider`.

So for example your send function would then look like this:
```fsharp
let send (msg: Action) (hubContext: FableHub<Action,Response>) =
    hubContext.Services.GetService<MyAdder>()
    |> update msg
    |> hubContext.Clients.Caller.Send
```

Or if your invoke function is simple like above, you can simply do:
```fsharp
let send (msg: Action) (hubContext: FableHub<Action,Response>) =
    invoke msg hubContext.Services
    |> hubContext.Clients.Caller.Send
```
