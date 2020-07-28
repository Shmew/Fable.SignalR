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

All hub functions have a `FableHub<_,_>` or `FableHub` as a parameter. This
interface has a property `Services` which exposes the `System.IServiceProvider`.

So for example your send function would then look like this:
```fsharp
let send (msg: Action) (hubContext: FableHub<Action,Response>) =
    hubContext.Services.GetService<MyAdder>()
    |> update msg
    |> hubContext.Clients.Caller.Send
```
