# Jest Extension

If you're using `Fable.Jest` then you can
install the `Fable.FastCheck.Jest` which
allows using `Fable.FastCheck` quite a bit
more convenient.

All of the extensions are for `Jest.test`.

## elmish

<Note>See [Elmish Model Testing](/elmish-model-testing) for usage.</Note>

Executes a model-based test using the init and 
update functions of an elmish application, 
performs checks based on the given message via 
the paired assertion.

The assertions list is where your Jest.expect
assertion(s) should be located.

Signature:
```fsharp
(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit
```

## elmish.only

<Note>See [Elmish Model Testing](/elmish-model-testing) for usage.</Note>

Executes only this model-based test using the init and 
update functions of an elmish application, 
performs checks based on the given message via 
the paired assertion.

The assertions list is where your Jest.expect
assertion(s) should be located.

Signature:
```fsharp
(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit
```

## elmish.skip

<Note>See [Elmish Model Testing](/elmish-model-testing) for usage.</Note>

Skips this model-based test using the init and 
update functions of an elmish application, 
performs checks based on the given message via 
the paired assertion.

The assertions list is where your Jest.expect
assertion(s) should be located.

Signature:
```fsharp
(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: 'Msg -> 'Model -> 'Model -> unit, 
 msgs: Arbitrary<'Msg list>, 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit
```

## prop

Runs a test using the provided arbitraries and predicate function.

Signature:
```fsharp
(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

...
```

Usage:
```fsharp
Jest.test.prop("My arb test", Arbitrary.Defaults.integer, fun i ->
    Jest.expect(i+1).toEqual(i+1)
)
```

## prop.only

Runs only this test using the provided arbitraries and predicate function.

Signature:
```fsharp
(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list, 
 ?timeout: int)
    -> unit

...
```

Usage:
```fsharp
Jest.test.prop.only("My arb test", Arbitrary.Defaults.integer, fun i ->
    Jest.expect(i+1).toEqual(i+1)
)
```

## prop.skip

Skips this test using the provided arbitraries and predicate function.

Signature:
```fsharp
(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 predicate: ('T0 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> Async<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<bool>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> JS.Promise<unit>), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> bool), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit

(name: string, 
 arb0: Arbitrary<'T0>, 
 arb1: Arbitrary<'T1>, 
 predicate: ('T0 -> 'T1 -> unit), 
 ?fastCheckOptions: IFastCheckOptionsProperty list)
    -> unit
...
```

Usage:
```fsharp
Jest.test.prop.skip("My arb test", Arbitrary.Defaults.integer, fun i ->
    Jest.expect(i+1).toEqual(i+1)
)
```
