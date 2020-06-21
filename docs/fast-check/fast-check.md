# FastCheck

FastCheck is the entry point to using most of the 
functionality of Fable.FastCheck. 

## FastCheck Options

Many of the functions below have overloads for `IFastCheckOptionsProperty list`, 
these are built using the `property` type:

```fsharp
type FastCheckOptions =
    /// Stop run on failure
    /// 
    /// It makes the run stop at the first encountered failure without shrinking.
    /// 
    /// When used in complement to `seed` and `path`,
    /// it replays only the minimal counterexample.
    endOnFailure: (value: bool)
        
    /// Custom values added at the beginning of generated ones
    /// 
    /// It enables users to come with examples they want to test at every run
    examples: (value: 'T list)
    
    /// Interrupt test execution after a given time limit: disabled by default
    /// 
    /// NOTE: Relies on `Date.now()`.
    /// 
    /// NOTE:
    /// Useful to avoid having too long running processes in your CI.
    ///
    /// Replay capability (see seed, path) can still be used if needed.
    /// 
    /// WARNING:
    /// If the test got interrupted before any failure occured
    /// and before it reached the requested number of runs specified by numRuns
    /// it will be marked as success. Except if markInterruptAsFailure as been 
    /// set to `true`
    interruptAfterTimeLimit: (value: int)

    /// Logger (see statistics): `console.log` by default
    logger: (value: string -> unit)

    /// Mark interrupted runs as failed runs: disabled by default
    markInterruptAsFailure: (value: bool)

    /// Maximal number of skipped values per run
    /// 
    /// Skipped is considered globally, so this value is used to compute maxSkips = 
    /// maxSkipsPerRun * numRuns.
    ///
    /// Runner will consider a run to have failed if it skipped maxSkips+1 times 
    /// before having generated numRuns valid entries.
    /// 
    /// See pre for more details on pre-conditions
    maxSkipsPerRun: (value: int)

    /// Number of runs before success: 100 by default
    numRuns: (value: int)

    /// Way to replay a failing property directly with the counterexample.
    ///
    /// It can be fed with the counterexamplePath returned by the failing test 
    /// (requires `seed` too).
    path: (value: string)

    /// Initial seed of the generator: `Date.now()` by default
    /// 
    /// It can be forced to replay a failed run.
    /// 
    /// In theory, seeds are supposed to be 32 bits integers.
    ///
    /// In case of double value, the seed will be rescaled into a 
    /// valid 32 bits integer (eg.: values between 0 and 1 will be evenly spread 
    /// into the range of possible seeds).
    seed: (value: float)

    /// Skip all runs after a given time limit: disabled by default
    /// 
    /// NOTE: Relies on `Date.now()`.
    /// 
    /// NOTE:
    /// Useful to stop too long shrinking processes.
    /// Replay capability (see seed, path) can resume the shrinking.
    /// 
    /// WARNING:
    /// It skips runs. Thus test might be marked as failed.
    /// Indeed, it might not reached the requested number of successful runs.
    skipAllAfterTimeLimit: (value: int)

    /// Maximum time in milliseconds for the predicate to answer: disabled by 
    /// default
    /// 
    /// WARNING: Only works for async code (see asyncProperty), will not 
    /// interrupt a synchronous code.
    timeout: (value: int)

    /// Force the use of unbiased arbitraries: biased by default
    unbiased: (value: bool)

module FastCheckOptions =
    /// Random generator is the core element behind the generation of random values 
    /// - changing it might directly impact the quality and performances of the 
    /// generation of random values.
    ///
    /// Default: xorshift128plus
    type randomType =
        congruential
        congruential32
        mersenne
        xorshift128plus
        xoroshiro128plus

    /// Set verbosity level.
    type verbose =
        none
        verbose
        veryVerbose
```

## assert'

Run the property, throw in case of failure.

It can be called directly from describe/it blocks of Mocha and Jest.

Signature: 
```fsharp 
(prop: AsyncProperty<'T>, ?fastCheckOptions: IFastCheckOptionsProperty list) -> JS.Promise<unit>
(prop: Property<'T>, ?fastCheckOptions: IFastCheckOptionsProperty list) -> unit
```

You can use this like so:

```fsharp
FastCheck.assert'(FastCheck.property(Arbitrary.Defaults.integer, fun i -> 
    Jest.expect(add i).toEqual(i + 1))
)
```

## asyncCheck

Run the property, does not throw contrary to [assert](#assert).

Signature: 
```fsharp 
type ExecutionStatus =
    | Success = 0
    | Skipped = -1
    | Failure = 1

/// Summary of the execution process.
type ExecutionTree<'T> =
    /// Status of the property.
    status: ExecutionStatus

    /// Generated value.
    value: 'T

    /// Values derived from this value.
    children: ExecutionTree<'T> list

type VerbosityLevel =
    | None = 0
    | Verbose = 1
    | VeryVerbose = 2

/// Post-run details produced by check.
/// 
/// A failing property can easily detected by checking the `failed` flag of this structure.
type RunDetails<'T> =
    /// If the test failed.
    failed: bool

    /// If the execution was interrupted.
    interrupted: bool

    /// Number of runs.
    /// 
    /// - In case of failed property: Number of runs up to the first failure 
    /// (including the failure run).
    ///
    /// - Otherwise: Number of successful executions.
    numRuns: float

    /// Number of skipped entries due to failed pre-condition.
    /// 
    /// As `numRuns` it only takes into account the skipped values that occured before the 
    /// first failure.
    numSkips: float

    /// Number of shrinks required to get to the minimal failing case (aka counterexample).
    numShrinks: float

    /// Seed that have been used by the run.
    /// 
    /// It can be forced in assert', check, sample and statistics using parameters.
    seed: float

    /// In case of failure: the counterexample contains the minimal failing case 
    /// (first failure after shrinking).
    counterexample: 'T option

    /// In case of failure: it contains the reason of the failure.
    error: string option

    /// In case of failure: path to the counterexample.
    /// 
    /// For replay purposes, it can be forced in assert', check, sample and statistics using 
    /// parameters.
    counterexamplePath: string option

    /// List all failures that have occurred during the run.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values present.
    failures: 'T list

    /// Execution summary of the run.
    /// 
    /// Traces the origin of each value encountered during the test and its execution status.
    ///
    /// Can help to diagnose shrinking issues.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values in it:
    ///
    /// - Verbose: Only failures.
    ///
    /// - VeryVerbose: Failures, Successes and Skipped.
    executionSummary: ExecutionTree<'T> list

    /// Verbosity level required by the user.
    verbose: VerbosityLevel

(prop: AsyncProperty<'T>, ?fastCheckOptions: IFastCheckOptionsProperty list) 
    -> Async<RunDetails<'T>>
```

You can use this like so:

```fsharp
FastCheck.asyncCheck(FastCheck.asyncProperty(Arbitrary.Defaults.integer, fun i -> 
    async { return add i = i + 1 }
) |> Async.map(fun res ->  Jest.expect(res.failed).toEqual(false))
```

## asyncModelRun

Run asynchronous commands over a Model and the Real system.

Throw in case of inconsistency.

Signature: 
```fsharp 
(initialModel: 'Model, real: 'Real, commandIter: seq<IAsyncCommand<'Model,'Real>>) 
    -> JS.Promise<unit>
(initialModel: 'Model, real: 'Real, commandIter: IAsyncCommandSeq<'Model,'Real>) 
    -> JS.Promise<unit>
```

See [Model Testing](/fast-check/model-testing) for usage.

## asyncProperty

Instantiate a new AsyncProperty.

Properties are the type used to make FastCheck assertions via [assert'](#assert) and [check](#check).

Signature: 
```fsharp 
(arb0: Arbitrary<'T0>, predicate: ('T0 -> Async<bool>)) -> AsyncProperty<'T0>
(arb0: Arbitrary<'T0>, predicate: ('T0 -> Async<unit>)) -> AsyncProperty<'T0>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> Async<bool>)) 
    -> AsyncProperty<'T0 * 'T1>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> Async<unit>)) 
    -> AsyncProperty<'T0 & 'T1>
...
```

You can use this like so:

```fsharp
FastCheck.property(Arbitrary.Defaults.integer, fun i -> 
    promise { return Jest.expect(add i).toEqual(i + 1) }
)
```

## check

Run the property, does not throw contrary to [assert](#assert).

Signature: 
```fsharp 
type ExecutionStatus =
    | Success = 0
    | Skipped = -1
    | Failure = 1

/// Summary of the execution process.
type ExecutionTree<'T> =
    /// Status of the property.
    status: ExecutionStatus

    /// Generated value.
    value: 'T

    /// Values derived from this value.
    children: ExecutionTree<'T> list

type VerbosityLevel =
    | None = 0
    | Verbose = 1
    | VeryVerbose = 2

/// Post-run details produced by check.
/// 
/// A failing property can easily detected by checking the `failed` flag of this structure.
type RunDetails<'T> =
    /// If the test failed.
    failed: bool

    /// If the execution was interrupted.
    interrupted: bool

    /// Number of runs.
    /// 
    /// - In case of failed property: Number of runs up to the first failure 
    /// (including the failure run).
    ///
    /// - Otherwise: Number of successful executions.
    numRuns: float

    /// Number of skipped entries due to failed pre-condition.
    /// 
    /// As `numRuns` it only takes into account the skipped values that occured before the 
    /// first failure.
    numSkips: float

    /// Number of shrinks required to get to the minimal failing case (aka counterexample).
    numShrinks: float

    /// Seed that have been used by the run.
    /// 
    /// It can be forced in assert', check, sample and statistics using parameters.
    seed: float

    /// In case of failure: the counterexample contains the minimal failing case 
    /// (first failure after shrinking).
    counterexample: 'T option

    /// In case of failure: it contains the reason of the failure.
    error: string option

    /// In case of failure: path to the counterexample.
    /// 
    /// For replay purposes, it can be forced in assert', check, sample and statistics using 
    /// parameters.
    counterexamplePath: string option

    /// List all failures that have occurred during the run.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values present.
    failures: 'T list

    /// Execution summary of the run.
    /// 
    /// Traces the origin of each value encountered during the test and its execution status.
    ///
    /// Can help to diagnose shrinking issues.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values in it:
    ///
    /// - Verbose: Only failures.
    ///
    /// - VeryVerbose: Failures, Successes and Skipped.
    executionSummary: ExecutionTree<'T> list

    /// Verbosity level required by the user.
    verbose: VerbosityLevel

(prop: Property<'T>, ?fastCheckOptions: IFastCheckOptionsProperty list) -> RunDetails<'T>
```

You can use this like so:

```fsharp
FastCheck.check(FastCheck.property(Arbitrary.Defaults.integer, fun i -> 
    add i = i + 1
) |> fun res -> Jest.expect(res.failed).toEqual(false)
```

## modelRun

Fires a DOM event.

Signature: 
```fsharp 
(initialModel: 'Model, real: 'Real, commandIter: seq<ICommand<'Model,'Real>>) -> unit
(initialModel: 'Model, real: 'Real, commandIter: ICommandSeq<'Model,'Real>) -> unit
```

See [Model Testing](/fast-check/model-testing) for usage.

## promiseCheck

Run the property, does not throw contrary to [assert](#assert).

Signature: 
```fsharp 
type ExecutionStatus =
    | Success = 0
    | Skipped = -1
    | Failure = 1

/// Summary of the execution process.
type ExecutionTree<'T> =
    /// Status of the property.
    status: ExecutionStatus

    /// Generated value.
    value: 'T

    /// Values derived from this value.
    children: ExecutionTree<'T> list

type VerbosityLevel =
    | None = 0
    | Verbose = 1
    | VeryVerbose = 2

/// Post-run details produced by check.
/// 
/// A failing property can easily detected by checking the `failed` flag of this structure.
type RunDetails<'T> =
    /// If the test failed.
    failed: bool

    /// If the execution was interrupted.
    interrupted: bool

    /// Number of runs.
    /// 
    /// - In case of failed property: Number of runs up to the first failure 
    /// (including the failure run).
    ///
    /// - Otherwise: Number of successful executions.
    numRuns: float

    /// Number of skipped entries due to failed pre-condition.
    /// 
    /// As `numRuns` it only takes into account the skipped values that occured before the 
    /// first failure.
    numSkips: float

    /// Number of shrinks required to get to the minimal failing case (aka counterexample).
    numShrinks: float

    /// Seed that have been used by the run.
    /// 
    /// It can be forced in assert', check, sample and statistics using parameters.
    seed: float

    /// In case of failure: the counterexample contains the minimal failing case 
    /// (first failure after shrinking).
    counterexample: 'T option

    /// In case of failure: it contains the reason of the failure.
    error: string option

    /// In case of failure: path to the counterexample.
    /// 
    /// For replay purposes, it can be forced in assert', check, sample and statistics using 
    /// parameters.
    counterexamplePath: string option

    /// List all failures that have occurred during the run.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values present.
    failures: 'T list

    /// Execution summary of the run.
    /// 
    /// Traces the origin of each value encountered during the test and its execution status.
    ///
    /// Can help to diagnose shrinking issues.
    /// 
    /// You must enable verbose with at least Verbosity.Verbose in Parameters
    /// in order to have values in it:
    ///
    /// - Verbose: Only failures.
    ///
    /// - VeryVerbose: Failures, Successes and Skipped.
    executionSummary: ExecutionTree<'T> list

    /// Verbosity level required by the user.
    verbose: VerbosityLevel

(prop: AsyncProperty<'T>, ?fastCheckOptions: IFastCheckOptionsProperty list) 
    -> Async<RunDetails<'T>>
```

You can use this like so:

```fsharp
FastCheck.promiseCheck(FastCheck.promiseProperty(Arbitrary.Defaults.integer, fun i -> 
    promise { return add i = i + 1 }
) |> Promise.map(fun res ->  Jest.expect(res.failed).toEqual(false))
```

## promiseProperty

Instantiate a new AsyncProperty.

Properties are the type used to make FastCheck assertions via [assert'](#assert) and [check](#check).

Signature: 
```fsharp 
(arb0: Arbitrary<'T0>, predicate: ('T0 -> JS.Promise<bool>)) -> AsyncProperty<'T0>
(arb0: Arbitrary<'T0>, predicate: ('T0 -> JS.Promise<unit>)) -> AsyncProperty<'T0>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> JS.Promise<bool>)) 
    -> AsyncProperty<'T0 * 'T1>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> JS.Promise<unit>)) 
    -> AsyncProperty<'T0 & 'T1>
...
```

## property

Instantiate a new Property.

Properties are the type used to make FastCheck assertions via [assert'](#assert) 
and [check](#check).

Signature: 
```fsharp 
(arb0: Arbitrary<'T0>, predicate: ('T0 ->bool)) -> Property<'T0>
(arb0: Arbitrary<'T0>, predicate: ('T0 ->unit)) -> Property<'T0>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> bool)) -> Property<'T0 * 'T1>
(arb0: Arbitrary<'T0>, arb1: Arbitrary<'T1>, predicate: ('T0 -> 'T1 -> unit)) -> Property<'T0 & 'T1>
...
```

You can use this like so:

```fsharp
FastCheck.property(Arbitrary.Defaults.integer, fun i -> 
    Jest.expect(add i).toEqual(i + 1)
)
```

## sample

Generate a list containing all the values that would have been generated during 
[assert'](#assert) and [check](#check).

Signature: 
```fsharp 
(arb: Arbitrary<'T>) -> 'T list
(arb: Arbitrary<'T>, fastCheckOptions: IFastCheckOptionsProperty list) -> 'T list
(arb: Arbitrary<'T>, numValues: int) -> 'T list
(arb: IProperty<'T,'Return>, fastCheckOptions: IFastCheckOptionsProperty list) -> 'T list
(arb: IProperty<'T,'Return>, numValues: int) -> 'T list
```

You can use this like so:

```fsharp
FastCheck.sample Arbitrary.Defaults.integer |> List.iter (printfn "%i")
```

## scheduledModelRun

Run asynchronous and scheduled commands over a Model and the Real system.

Throw in case of inconsistency.

Signature: 
```fsharp 
(scheduler: AsyncScheduler, 
 initialModel: 'Model, 
 real: 'Real, 
 commandIter: seq<ICommand<'Model,'Real>>) 
    -> JS.Promise<unit>

(scheduler: AsyncScheduler, 
 initialModel: 'Model, 
 real: 'Real, 
 commandIter: ICommandSeq<'Model,'Real>) 
    -> JS.Promise<unit>

(scheduler: PromiseScheduler, 
 initialModel: 'Model, 
 real: 'Real, 
 commandIter: seq<ICommand<'Model,'Real>>) 
    -> JS.Promise<unit>

(scheduler: PromiseScheduler, 
 initialModel: 'Model, 
 real: 'Real, 
 commandIter: ICommandSeq<'Model,'Real>) 
    -> JS.Promise<unit>
```

See [Model Testing](/fast-check/model-testing) and [Scheduler](/fast-check/scheduler) for usage.

## statistics

Gather useful statistics concerning generated values.

Prints the result in `console.log` or `params.logger` (if defined).

Classifier function that can classify the generated value in zero, one, or more categories (with free labels).

Signature: 
```fsharp 
(arb: Arbitrary<'T>, classify: 'T -> string) -> unit
(arb: Arbitrary<'T>, classify: 'T -> string, 
    fastCheckOptions: IFastCheckOptionsProperty list) -> unit
(arb: Arbitrary<'T>, classify: 'T -> string, numValues: int) -> unit
(arb: Arbitrary<'T>, classify: 'T -> seq<string>) -> unit
(arb: Arbitrary<'T>, classify: 'T -> seq<string>, 
    fastCheckOptions: IFastCheckOptionsProperty list) -> unit
(arb: Arbitrary<'T>, classify: 'T -> seq<string>, numValues: int) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> string) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> string, 
    fastCheckOptions: IFastCheckOptionsProperty list) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> string, numValues: int) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> seq<string>) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> seq<string>, 
    fastCheckOptions: IFastCheckOptionsProperty list) -> unit
(prop: IProperty<'T,'Return>, classify: 'T -> seq<string>, numValues: int) -> unit
```

You can use this like so:

```fsharp
FastCheck.statistics(Arbitrary.Defaults.integer, id)
```

## stringify

Convert any value to its fast-check string representation.

Signature: 
```fsharp 
(value: 'T) -> string
```

You can use this like so:

```fsharp
FastCheck.stringify 1
```
