# Describe

Tests in Jest are defined by module level `describe` blocks. 
All tests should be wrapped in these based on some commonality. 
You can think of this as your `testList` if you're famlilar 
with [Expecto] or [Fable.Mocha].

There are three describes available:
* [describe](#describe-2)
* [describe.only](#describeonly)
* [describe.skip](#describeskip)

## describe

Runs all tests within the describe block and groups them 
together in the results.

Signature: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
```

You can use this like so:

```fsharp
Jest.describe("my test suite", (fun () ->
    // tests go here
))
```

## describe.only

Only runs all tests within the describe block and groups 
them together in the results.

The difference between this and [describe](#describe-2) is 
hat *only* this block will
run within a given file. __All other describe blocks will be 
skipped!__

Signature: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
```

You can use this like so:

```fsharp
Jest.describe.only("my test suite", (fun () ->
    // tests go here
))
```

## describe.skip

Runs __no tests within the describe block__.

Signature: 
```fsharp 
(name: string, fn: unit -> unit) -> unit
```

You can use this like so:

```fsharp
Jest.describe.skip("my test suite", (fun () ->
    // tests go here, but they won't be executed.
))
```

What is the point of this function?

Good question! It is mostly when you need to control which
tests are executed based on some external factor.

For example if you have tests that will always fail in a CI
environment, you could match on an environment variable and
skip some tests based on the value.

## Where are the other describes?

If you're familiar with Jest, you may have noticed that 
the `describe.each` family of `describe` is missing.

The reason for this is that it is really not useful for us.

Let's look at how it is used in the [Jest docs]:

```js
describe.each([
  [1, 1, 2],
  [1, 2, 3],
  [2, 1, 3],
])('.add(%i, %i)', (a, b, expected) => {
  test(`returns ${expected}`, () => {
    expect(a + b).toBe(expected);
  });
});
```

If you want this type of functionality, it can be implemented like this:

```fsharp
Jest.describe("how to run a describe like describe.each", (fun () ->
    let myTestCases = [
        (1, 1, 2)
        (1, 2, 3)
        (2, 1, 3)
    ]

    for (a, b, expected) in myTestCases do
        Jest.test(sprintf "%i + %i returns %i" a b expected, (fun () ->
            Jest.expect(a + b).toBe(expected)    
        ))
))
```

[Expecto]: https://github.com/haf/expecto
[Fable.Mocha]: https://github.com/Zaid-Ajaj/Fable.Mocha
[Jest docs]: https://jestjs.io/docs/en/api#describeeachtablename-fn-timeout
