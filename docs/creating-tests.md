# Creating Tests

For the most part creating tests is the same as with .NET 
applications, except in this case it must be run via node.js.

You will want to create a library project (I usually put mine 
in tests/projectName.Tests/) You then will want to create your
tests with a specific name synax of `YourFileName.test.fs`, 
this allows Jest to pick up all your tests easily without telling
it which tests it needs to run and where, everything within a 
directory will be searched to find all `.test.js` files.

Almost every aspect of `Fable.Jester` and `Fable.ReactTestingLibrary`
have tests written for them, should you need the reference.

## Splitter Config

This is the main caveat when building your jest project. You will
want to make sure that you include a `splitter.config.js` file and
configure it in two main ways:

There are two main things to note here for both types: 
 * `allFiles: true` __Fable will not compile all of your tests 
   if this is not set__.
 * `sourceMaps: "inline"` this enables Jest to display the FSharp 
   source code when a test fails rather than the transpiled Javascript.

   <resolved-image source='/images/jest/sourcemap.png' />

### Without snapshot testing

When not doing snapshot testing you can forgo the config file and
use the cli, but I don't recommend it.

```js
const path = require("path");

module.exports = {
    allFiles: true,
    entry: path.join(__dirname, "./Fable.ReactTestingLibrary.Tests.fsproj"),
    outDir: path.join(__dirname, "../../dist/tests/RTL"),
    babel: {
        plugins: ["@babel/plugin-transform-modules-commonjs"],
        sourceMaps: "inline"
    }
};
```

### With snapshot testing

Enabling snapshot testing is pretty easy, the main thing is copy 
and pasting the below `onCompiled()` function. You shouldn't need 
to modify anything, the node module is included in the project 
and will automatically load.

```js
const path = require("path");
const testsDir = path.join(__dirname, "../../dist/tests");

module.exports = {
    allFiles: true,
    entry: path.join(__dirname, "./Fable.Jester.Tests.fsproj"),
    outDir: testsDir,
    babel: {
        plugins: [
            "@babel/plugin-transform-modules-commonjs"
        ],
        sourceMaps: "inline"
    },
    onCompiled() {
        const fs = require('fs')
        const findSnapshotLoader = () => {
            const jesterDir =
                fs
                    .readdirSync(testsDir)
                    .sort()
                    .reverse()
                    .find(item => { return item.startsWith("Fable.Jester") })

            return require(path.join(testsDir, jesterDir, "SnapshotLoader"))
        }

        findSnapshotLoader().copySnaps(__dirname, this.outDir)
    }
};
```

## Test Structure

It is important to note that due to how Fable handles 
namespaces, __you cannot use namespaces for your tests__.

Each file you will write your describe blocks (they are 
optional, but it's recommended) and then place your tests 
within them. That's all that's required to start testing!

Here is an example:

```fsharp
module Tests

open Fable.Jester

[<RequireQualifiedAccess>]
module Async =
    let map f computation =
        async {
            let! res = computation
            return f res
        }

let myPromise = promise { return 1 + 1 }
    
let myAsync = async { return 1 + 1 }

Jest.describe("can run basic tests", (fun () ->
    Jest.test("running a test", (fun () ->
        Jest.expect(1+1).toEqual(2)
    ))

    Jest.test("running a promise test", (fun () ->
        Jest.expect(myPromise).resolves.toEqual(2)
    ))
    Jest.test("running a promise test", promise {
        do! Jest.expect(myPromise).resolves.toEqual(2)
        do! Jest.expect(myPromise |> Promise.map ((+) 1)).resolves.toEqual(3)
    })

    Jest.test("running an async test", (fun () ->
        Jest.expect(myAsync).toEqual(2)
    ))
    Jest.test("running an async test", async {
        do! Jest.expect(myAsync).toEqual(2)
        do! Jest.expect(myAsync |> Async.map ((+) 1)).toEqual(3)
    })
))

Jest.describe("how to run a test like test.each", (fun () ->
    Jest.test("same functionality as test.each", (fun () ->
        for (input, output) in [|(1, 2);(2, 3);(3, 4)|] do 
            Jest.expect(input + 1).toEqual(output)
    ))
))

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

Jest.describe("tests with the skip modifier don't get run", (fun () ->
    Jest.test.skip("adds", (fun () ->
        Jest.expect(true).toEqual(false)
    ))
    Jest.test("this should execute", (fun () ->
        Jest.expect(true).toEqual(true)
    ))
))

Jest.describe("todo tests give us our todo", (fun () ->
    Jest.test.todo "Do this!"
))

Jest.describe.skip("these shouldn't run", (fun () ->
    Jest.test("this shouldn't run", (fun () ->
        Jest.expect(true).toEqual(false)
    ))
    Jest.test("this shouldn't run either", (fun () ->
        Jest.expect(true).toEqual(false)
    ))
))
```

## Snapshot Testing

Doing snapshots is very simple, you will run a test
that calls `toMatchSnapshot()`. If the file does not
exist, it will get generated in your project directory.

From that point forward when you run your tests it will
confirm the DOM structure matches that of your snapshot.

See [jest documentation] for more information.

[jest documentation]: https://jestjs.io/docs/en/snapshot-testing
