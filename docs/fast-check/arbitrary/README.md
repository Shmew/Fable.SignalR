# Arbitrary

Arbitrary is the interface that holds all the details needed
to generate the values for your tests:

```fsharp
type Random =
    /// Clone the random number generator
    clone: unit -> Random

    /// Generate an integer having `bits` random bits.
    next: bits: int -> int

    /// Generate a random boolean.
    nextBoolean: unit -> bool

    /// Generate a random integer (32 bits).
    nextInt: unit -> int

    /// Generate a random integer between min (included) and max (included).
    nextInt: min: int * max: int -> int

    /// Generate a random any between min (included) and max (included).
    nextBigInt: min: bigint * max: bigint -> bigint

    /// Generate a random floating point number between 0.0 (included) and 1.0 (excluded).
    nextDouble: unit -> float

/// A Shrinkable<'T> holds an internal value of type `'T`
/// and can shrink it to smaller values.
type Shrinkable<'T> =
    value_ : 'T

    shrink: unit -> seq<Shrinkable<'T>>

    /// State storing the result of hasCloneMethod.
    ///
    /// If true the value will be cloned each time it gets accessed.
    hasToBeCloned : bool

    /// Safe value of the shrinkable.
    ///
    /// Depending on hasToBeCloned it will either be value_ or a clone of it.
    value : 'T

    /// Create another shrinkable by mapping all values using the provided `mapper`
    ///
    /// Both the original value and the shrunk ones are impacted.
    map: mapper: ('T -> 'U) -> Shrinkable<'U>

    /// Create another shrinkable by filtering its shrunk values against a predicate.
    /// 
    /// Return true to keep the element, false otherwise.
    filter: predicate: ('T -> bool) -> Shrinkable<'T>

type Arbitrary<'T> =
    /// Generate a value of type `'T` along with its shrink method
    /// based on the provided random number generator.
    generate: mrng: Random -> Shrinkable<'T>

    /// Create another arbitrary by filtering values against a predicate.
    /// 
    /// Return true to keep the element, false otherwise.
    filter: predicate: ('T -> bool) -> Arbitrary<'T>

    /// Create another arbitrary by mapping all produced values using the provided mapper function.
    map: mapper: ('T -> 'U) -> Arbitrary<'U>

    /// Create another arbitrary by mapping a value from a base Arbirary using the fmapper function.
    [<Emit("$0.chain($1)")>]
    bind: fmapper: ('T -> Arbitrary<'U>) -> Arbitrary<'U>

    /// Create another Arbitrary with no shrink values.
    noShrink: unit -> Arbitrary<'T>

    /// Create another Arbitrary having bias - by default returns itself.
    withBias: freq: float -> Arbitrary<'T>

    /// Create another Arbitrary that cannot be biased.
    noBias: unit -> Arbitrary<'T>

type ArbitraryWithShrink<'T> =
    inherit Arbitrary<'T>

    /// Produce a stream of shrinks of value.
    shrink: value: 'T * ?shrunkOnce: bool -> seq<'T>

    /// Build the Shrinkable associated to value.
    shrinkableFor: value: 'T * ?shrunkOnce: bool -> Shrinkable<'T>
```

The functions outlined below are located in the `Arbitrary` module.

## apply

Signature:
```fsharp
(arbF: Arbitrary<'T -> 'U>) (arb: Arbitrary<'T>) -> Arbitrary<'U>
```

## asyncCommands

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IAsyncCommand to be executed by asyncModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IAsyncCommand.

Signature:
```fsharp
(commandArbs: Arbitrary<IAsyncCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IAsyncCommand<'Model,'Real>>>
```

## asyncCommandsOfMax

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IAsyncCommand to be executed by asyncModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IAsyncCommand.

Signature:
```fsharp
(maxCommands: int) (commandArbs: Arbitrary<IAsyncCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IAsyncCommand<'Model,'Real>>>
```

## asyncCommandsOfSettings

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IAsyncCommand to be executed by asyncModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IAsyncCommand.

Signature:
```fsharp
(settings: ICommandConstraintProperty list) 
(commandArbs: Arbitrary<IAsyncCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IAsyncCommand<'Model,'Real>>>
```

## auto

Attempts to auto generate arbitraries for a given type.

This is mostly intended for complex types that
would be very cumbersome to write an Arbitrary for.

All types generated from this will use the default 
Arbitrary for each primitive.

Classes are currently [not supported](https://github.com/fable-compiler/Fable/issues/2027).

Signature:
```fsharp
unit -> Arbitrary<'T>
```

Usage:
```fsharp
type MyDU =
    | Empty
    | SingleValue of int
    | Record of RecordTest
    | Function of (int -> string)

Arbitrary.auto<MyDU>()
```

## bind

Signature:
```fsharp
(f: 'A -> Arbitrary<'B>) (arb: Arbitrary<'A>) -> Arbitrary<'B>
```

## bind2

Signature:
```fsharp
(f: 'A -> 'B -> Arbitrary<'C>) (a: Arbitrary<'A>) (b: Arbitrary<'B>) -> Arbitrary<'B>
```

## choose

Applies the given function to the arbitrary. 
Returns an arbitrary comprised of the results 
x for each generated value where the function 
returns Some(x).

Signature:
```fsharp
(chooser: 'T -> 'U option) (arb: Arbitrary<'T>) -> Arbitrary<'U>
```

## clonedConstant

Clones a constant, useful when generating an arbitrary from a mutable value.

Signature:
```fsharp
(value: 'T) -> Arbitrary<'T>
```

## constant

Creates an arbitrary that returns a constant value.

Signature:
```fsharp
(value: 'T) -> Arbitrary<'T>
```

## commands

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of Command to be executed by modelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of Commands.

Signature:
```fsharp
(commandArbs: Arbitrary<ICommand<'Model,'Real>> list) 
    -> Arbitrary<seq<ICommand<'Model,'Real>>>
```

## commandsOfMax

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of Command to be executed by modelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of Commands.

Signature:
```fsharp
(maxCommands: int) (commandArbs: Arbitrary<ICommand<'Model,'Real>> list) 
    -> Arbitrary<seq<ICommand<'Model,'Real>>>
```

## commandsOfSettings

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of Command to be executed by modelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of Commands.

Signature:
```fsharp
(settings: ICommandConstraintProperty list) (commandArbs: Arbitrary<ICommand<'Model,'Real>> list)
    -> Arbitrary<seq<ICommand<'Model,'Real>>>
```

## elements

Build an arbitrary that randomly generates one of the values in the given non-empty seq.

Signature:
```fsharp
(xs: 'T seq) -> Arbitrary<'T>
```

## elmish

<Note>See [Elmish Model Testing](/elmish-model-testing) for usage.</Note>

Creates an arbitrary of elmish commands to use with runModel.

Signature:
```fsharp
// Uses auto<'Msg>() to generate Msgs
(init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: ('Msg -> 'Model -> 'Model -> unit))
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>

(init: 'Model * Elmish.Cmd<'Msg>, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: ('Msg -> 'Model -> 'Model -> unit), 
 msgs: Arbitrary<'Msg list>)
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>

// Uses auto<'Msg>() to generate Msgs
(init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: ('Msg -> 'Model -> 'Model -> unit))
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>

(init: 'Model, 
 update: 'Msg -> 'Model -> 'Model * Elmish.Cmd<'Msg>, 
 asserter: ('Msg -> 'Model -> 'Model -> unit), 
 msgs: Arbitrary<'Msg list>)
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>

// Uses auto<'Msg>() to generate Msgs
(init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: ('Msg -> 'Model -> 'Model -> unit))
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>

(init: 'Model, 
 update: 'Msg -> 'Model -> 'Model, 
 asserter: ('Msg -> 'Model -> 'Model -> unit), 
 msgs: Arbitrary<'Msg list>)
    -> Arbitrary<Model<'Model,'Msg> * 
                 Model<'Model,'Msg> * 
                 seq<ICommand<Model<'Model,'Msg>,Model<'Model,'Msg>>>>
```

## filter

Create another arbitrary by filtering values against a predicate.

Return true to keep the element, false otherwise.

Signature:
```fsharp
(a: Arbitrary<'T>) -> Arbitrary<'T>
```

## func

Creates an arbitrary function that returns the given arbitrary value.

Signature:
```fsharp
(arb: Arbitrary<'TOut>) -> Arbitrary<'T -> 'TOut>
```

## infiniteStream

Produce an infinite stream of values.

<Note type="warning">Requires Object.assign.</Note>

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<seq<'T>>
```

## map

Includes map through to map6.

Signature:
```fsharp
(f: 'A -> 'B) (a: Arbitrary<'A>) -> Arbitrary<'B>
(f: 'A -> 'B -> 'C) (a: Arbitrary<'A>) (b: Arbitrary<'B>) -> Arbitrary<'C>
(f: 'A -> 'B -> 'C -> 'D) (a: Arbitrary<'A>) (b: Arbitrary<'B>) (c: Arbitrary<'C>) -> Arbitrary<'D>
...
```

## mixedCase

Randomly switch the case of characters generated by `Arbitrary<string>` (upper/lower).

Signature:
```fsharp
(stringArb: Arbitrary<string>) -> Arbitrary<string>
```

## mixedCaseWithToggle

Randomly switch the case of characters generated by `Arbitrary<string>` (upper/lower).

Signature:
```fsharp
(toggleCase: bool) (stringArb: Arbitrary<string>) -> Arbitrary<string>
```

## option

Generates an option of a given arbitrary.

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<'T option>
```

## optionOfFreq

Generates an option of a given arbitrary.

The probability of None is `1. / freq`.

Signature:
```fsharp
(freq: float) (arb: Arbitrary<'T>) -> Arbitrary<'T option>
```

## promiseCommands

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IPromiseCommand to be executed by promiseModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IPromiseCommand.

Signature:
```fsharp
(commandArbs: Arbitrary<IPromiseCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IPromiseCommand<'Model,'Real>>>
```

## promiseCommandsOfMax

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IPromiseCommand to be executed by promiseModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IPromiseCommand.

Signature:
```fsharp
(maxCommands: int) (commandArbs: Arbitrary<IPromiseCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IPromiseCommand<'Model,'Real>>>
```

## promiseCommandsOfSettings

<Note>See [Model Testing](/model-testing) for usage.</Note>

Sequence of IPromiseCommand to be executed by promiseModelRun.

This implementation comes with a shrinker adapted for commands.

It should shrink more efficiently than a normal sequence of IPromiseCommand.

Signature:
```fsharp
(settings: ICommandConstraintProperty list) 
(commandArbs: Arbitrary<IPromiseCommand<'Model,'Real>> list) 
    -> Arbitrary<seq<IPromiseCommand<'Model,'Real>>>
```


## record

<Note type="warning">This does not produce F# records.</Note>

Records following the `recordModel` schema.

Signature:
```fsharp
(recordModel: Map<string,obj>) -> Arbitrary<obj>
```

## recordWithDeletedKeys

Signature:
```fsharp
(recordModel: Map<string,obj>) -> Arbitrary<obj>
```

## result

Generates a result of the given arbitraries.

Signature:
```fsharp
(ok: Arbitrary<'Success>) (err: Arbitrary<'Failure>) -> Arbitrary<Result<'Success,'Failure>>
```

## resultOfFreq

Generates a result of the given arbitraries.

The probability of Error is `1. / freq`.

Signature:
```fsharp
(freq: float) (ok: Arbitrary<'Success>) (err: Arbitrary<'Failure>) 
    -> Arbitrary<Result<'Success,'Failure>>
```

## stringOf

Creates a string arbitrary using the characters produced by a char arbitrary.

Signature:
```fsharp
(charArb: Arbitrary<char>) -> Arbitrary<string>
```

## stringOfMaxSize

Creates a string arbitrary using the characters produced by a char arbitrary.

Signature:
```fsharp
(maxLength: int) (charArb: Arbitrary<char>) -> Arbitrary<string>
```

## stringOfSize

Creates a string arbitrary using the characters produced by a char arbitrary.

Signature:
```fsharp
(minLength: int) (maxLength: int) (charArb: Arbitrary<char>)  -> Arbitrary<string>
```

## unzip

Includes unzip through to unzip6.

Signature:
```fsharp
(a: Arbitrary<'A * 'B>) -> Arbitrary<'A> * Arbitrary<'B>
(a: Arbitrary<'A * 'B * 'C>) -> Arbitrary<'A> * Arbitrary<'B> * Arbitrary<'C>
...
```

## zip

Includes zip through to zip6.

Signature:
```fsharp
(a: Arbitrary<'A>) (b: Arbitrary<'B>) -> Arbitrary<'A * 'B>
(a: Arbitrary<'A>) (b: Arbitrary<'B>) (c: Arbitrary<'C>) -> Arbitrary<'A * 'B * 'C>
```
