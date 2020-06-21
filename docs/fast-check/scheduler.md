# Scheduler

The scheduler arbitrary is a tool to test for the 
presence of race-conditions in your code.

This feature is likely to be largely unused, as
these types of issues are hard to create in the 
wonderful world of functional-programming.

The way this works is that the scheduler allows
you to wrap all async/promises used in your code which
then will be resolved in an arbitrary order. For 
example, if you have two async/promises which one should
always resolve first (not by definition, but by
nature: like one sleeping for 1 second and the other
for 20 seconds) the scheduler may decide to 
resolve the longer promise first.

There are two types of schedulers:

Shared between them:

```fsharp
type SchedulerReturnTask =
    isDone: bool
    
    isFaulty: bool
```

## AsyncScheduler

```fsharp
type AsyncSchedulerReturn =
    isDone: bool
    
    isFaulty: bool
    
    task: Async<SchedulerReturnTask>

type AsyncScheduler =
    /// Adds an async to the scheduler, returns the same 
    /// async that now runs in the context of the scheduler.
    schedule (a: Async<'T>) -> Async<'T>

    /// Adds a functions that generates asyncs to the scheduler, returns the same 
    /// async that now runs in the context of the scheduler.
    scheduleFunction (f: 'Args -> Async<'T>) -> ('Args -> Async<'T>)

    /// Adds a sequence of asyncs to the scheduler, returns the same 
    /// asyncs that now runs in the context of the scheduler.
    scheduleSequence (funcs: seq<unit -> Async<obj>>) -> AsyncSchedulerReturn
    scheduleSequence (funcs: seq<(unit -> Async<obj>) * string>) -> AsyncSchedulerReturn

    /// Number of pending tasks waiting to be scheduled by the scheduler.
    count: unit -> int

    /// Wait for one promise to resolve in the scheduler.
    ///
    /// Throws if there is no more pending tasks.
    waitOne: unit -> Async<unit>

    /// Tries to wait for one promise to resolve in the scheduler.
    ///
    /// Returns None if there is no more pending tasks.
    tryWaitOne: unit -> Async<unit> option

    /// Wait all scheduled tasks, including the ones that might be created by one of 
    /// the resolved task.
    ///
    /// Do not use if waitAll call has to be wrapped into an helper function such as act that can 
    /// relaunch new tasks afterwards.
    waitAll: unit -> Async<unit> 
```

Usage would look like this:

```fsharp
// If you're using Fable.FastCheck.Jest
Jest.test.prop("Scheduler runs async", Arbitrary.Defaults.asyncScheduler, fun s ->
    async {
        let one = s.schedule(async { return 1 })
        let two = s.schedule(async { return 2 })

        do! s.waitAll()

        do! Jest.expect(one).toBe(1)
        do! Jest.expect(two).toBe(2)
    }
)

Jest.test("Scheduler runs async", fun () -> 
    FastCheck.assert'(FastCheck.asyncProperty(Arbitrary.Defaults.asyncScheduler, fun s ->
        async {
            let one = s.schedule(async { return 1 })
            let two = s.schedule(async { return 2 })

            do! s.waitAll()

            do! Jest.expect(one).toBe(1)
            do! Jest.expect(two).toBe(2)
        }
    ))
)
```

## PromiseScheduler

```fsharp
type PromiseSchedulerReturn =
    isDone: bool
    
    isFaulty: bool
    
    task: JS.Promise<SchedulerReturnTask>

type PromiseScheduler =
    /// Adds a promise to the scheduler, returns the same 
    /// promise that now runs in the context of the scheduler.
    schedule: (prom: JS.Promise<'T>) -> JS.Promise<'T>

    /// Adds a functions that generates promises to the scheduler, returns the same 
    /// promise that now runs in the context of the scheduler.
    scheduleFunction: (f: 'Args -> JS.Promise<'T>) -> 'Args -> JS.Promise<'T>

    /// Adds a sequence of promises to the scheduler, returns the same 
    /// promises that now runs in the context of the scheduler.
    scheduleSequence: (funcs: seq<unit -> JS.Promise<obj>>) -> PromiseSchedulerReturn
    scheduleSequence: (funcs: seq<(unit -> JS.Promise<obj>) * string>) -> PromiseSchedulerReturn

    /// Number of pending tasks waiting to be scheduled by the scheduler.
    count: unit -> int

    /// Wait for one promise to resolve in the scheduler.
    ///
    /// Throws if there is no more pending tasks.
    waitOne: unit -> JS.Promise<unit>

    /// Tries to wait for one promise to resolve in the scheduler.
    ///
    /// Returns None if there is no more pending tasks.
    tryWaitOne: unit -> JS.Promise<unit> option

    /// Wait all scheduled tasks, including the ones that might be created by one of the 
    /// resolved task.
    ///
    /// Do not use if waitAll call has to be wrapped into an helper function such as act 
    /// that can relaunch new tasks afterwards.
    waitAll: unit -> JS.Promise<unit> 
```

Usage would look like this:

```fsharp
// If you're using Fable.FastCheck.Jest
Jest.test.prop("Scheduler runs promises", Arbitrary.Defaults.promiseScheduler, fun s ->
    promise {
        let one = s.schedule(promise { return 1 })
        let two = s.schedule(promise { return 2 })

        do! s.waitAll()

        do! Jest.expect(one).resolves.toBe(1)
        do! Jest.expect(two).resolves.toBe(2)
    }
)

Jest.test("Scheduler runs promises", fun () -> 
    FastCheck.assert'(FastCheck.promiseProperty(Arbitrary.Defaults.promiseScheduler, fun s ->
        promise {
            let one = s.schedule(promise { return 1 })
            let two = s.schedule(promise { return 2 })

            do! s.waitAll()

            do! Jest.expect(one).resolves.toBe(1)
            do! Jest.expect(two).resolves.toBe(2)
        }
    ))
)
```
