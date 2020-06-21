# Elmish Model Testing

This library has some helpers to make a using 
model-based testing compatible with [Elmish] by
testing your update function and resulting model.

<Note>If you want to learn more about Elmish
I highly recommend you have a gander at 
[Zaid-Ajaj]'s [Book] on the subject.</Note>

To start you need to have the usual suspects:

```fsharp
type Model = { Count: int }

let init () =
    { Count = 0 }, Cmd.none

type Msg =
    | Decrement
    | Increment

let update msg (model: Model) =
    match msg with
    | Decrement -> { model with Count = model.Count - 1 }, Cmd.none
    | Increment -> { model with Count = model.Count + 1 }, Cmd.none
```

Then we define the assertions that will be made 
against the model during the test:

```fsharp
let asserter msg oldModel newModel =
    match msg with
    | Decrement -> Jest.expect(oldModel.Count).toBeGreaterThan(newModel.Count)
    | Increment -> Jest.expect(oldModel.Count).toBeLessThan(newModel.Count)
```

Then it's time to run our tests:

```fsharp
// If you're using Fable.FastCheck.Jest
Jest.test.elmish("test my elmish", init(), update, asserter)

Jest.test("test my elmish", fun () ->
    FastCheck.assert'(
        FastCheck.property(
            Arbitrary.elmish(init, update, asserter), 
            fun (model, real, cmds) ->
                FastCheck.modelRun(model, real, cmds)
        )
    )
)
```

Wait, what is this `test.elmish`/`Arbitrary.elmish` doing?

Great question! If you do not provide your own Arbitrary
like this:

```fsharp
let msgs = Arbitrary.constant [
    Decrement
    Increment
]

// If you're using Fable.FastCheck.Jest
Jest.test.elmish("test my elmish", init(), update, asserter, msgs)

Jest.test("test my elmish", fun () ->
    FastCheck.assert'(
        FastCheck.property(
            Arbitrary.elmish(init, update, asserter, msgs), 
            fun (model, real, cmds) ->
                FastCheck.modelRun(model, real, cmds)
        )
    )
)
```

Then [Arbitrary.auto] is called on the `'Msg` type provided.
This function will attempt to generate an arbitrary for the
entire discriminated union. It supports nested structures, 
functions, etc. As long as you don't need custom behavior 
this is a way to quickly get tests up and running without
constructing some potentially very large arbitraries.

[Arbitrary.auto]:/fast-check/arbitrary/arbitrary#auto
[Book]:https://zaid-ajaj.github.io/the-elmish-book
[Elmish]:https://github.com/elmish/elmish
[Zaid-Ajaj]:https://github.com/Zaid-Ajaj
