# Test

The `test` is the core functionality of Jest, and where you 
make all of your assertions.

There are four tests available:
* [test](#test-2)
* [test.only](#testonly)
* [test.skip](#testskip)
* [test.todo](#testtodo)

## test

Runs all tests within the test block and groups them together 
in the results.

Signatures: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
(name: string, fn: unit -> JS.Promise<unit>) -> unit
(name: string, fn: unit -> Async<unit>) -> unit
(name: string, fn: JS.Promise<unit>) -> unit
(name: string, fn: Async<unit>) -> unit
```

You can use this like so:

```fsharp
Jest.test("my test", (fun () ->
    // assertions go here
))

Jest.test("my test", promise {
    do! Jest.expect(myPromise).resolves.toBe(1)
})

Jest.test("my test", async {
    do! Jest.expect(myAsync).toBe(1)
})
```

## test.only

The difference between this and [test](#test-2) is that *only* 
this test will execute within a given describe block. __All 
other test blocks will be skipped!__

Signatures: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
(name: string, fn: unit -> JS.Promise<unit>) -> unit
(name: string, fn: unit -> Async<unit>) -> unit
(name: string, fn: JS.Promise<unit>) -> unit
(name: string, fn: Async<unit>) -> unit
```

You can use this like so:

```fsharp
Jest.test.only("my test", (fun () ->
    // assertions go here
))

Jest.test.only("my test", promise {
    do! Jest.expect(myPromise).resolves.toBe(1)
})

Jest.test.only("my test", async {
    do! Jest.expect(myAsync).toBe(1)
})
```

## test.skip

__Runs no assertions within the test block__.

Signatures: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
(name: string, fn: unit -> JS.Promise<unit>) -> unit
(name: string, fn: unit -> Async<unit>) -> unit
(name: string, fn: JS.Promise<unit>) -> unit
(name: string, fn: Async<unit>) -> unit
```

You can use this like so:

```fsharp
Jest.test.skip("my test", (fun () ->
    // assertions go here, but won't be executed
))

Jest.test.skip("my test", promise {
    // will not assert
    do! Jest.expect(myPromise).resolves.toBe(1)
})

Jest.test.skip("my test", async {
    // will not assert
    do! Jest.expect(myAsync).toBe(1)
})
```

What is the point of this function?

Good question! It is mostly when you need to control which
tests are executed based on some external factor.

For example if you have tests that will always fail in a CI
environment, you could match on an environment variable and
skip some tests based on the value.

## test.todo

This is a placeholder so you can remember to implement a test
at a later point in time.

Signature: 
```fsharp 
(name: string) -> unit
```

You can use this like so:

```fsharp
Jest.test.todo "Do this!"
```

When you run your tests the summary will include a count of
tests that still need to be done.

<resolved-image source='/images/jest/test-summary.png' />

## Multiple assertions

You can make multiple assertions for a test by simply running
them in your test block.

One thing to note is that if your test is making multiple 
assertions that include __promises__ or __async__ you __must
use the promise or async computation expressions!__ If you 
do not do this the assertions can either not get run, or cause
your entire test suite to have wildly different results each
run.

The `Async` computation expression is overloaded to resolve
expected promises.


```fsharp
Jest.test("my test", promise {
    do! Jest.expect(myPromise).resolves.not.toBe(2)
    do! Jest.expect(myPromise).resolves.toBe(1)
})

Jest.test("my test", async {
    do! Jest.expect(myPromise).resolves.toBe(1)
    do! Jest.expect(myAsync).toBe(1)
})
```

## Where are the other tests?

This is the same logic as the 
[above section on describes](#where-are-the-other-describes)

The reason for this is that it is really not useful for us.

Let's look at how it is used in the [Jest docs]:

```js
test.each([
  [1, 1, 2],
  [1, 2, 3],
  [2, 1, 3],
])('.add(%i, %i)', (a, b, expected) => {
  expect(a + b).toBe(expected);
});
```

If you want this type of functionality, it can be implemented 
like this:

```fsharp
Jest.test("same functionality as test.each", (fun () ->
    for (input, output) in [|(1, 2);(2, 3);(3, 4)|] do 
        Jest.expect(input + 1).toEqual(output)
))
```

[Jest docs]: https://jestjs.io/docs/en/api#testeachtablename-fn-timeout
