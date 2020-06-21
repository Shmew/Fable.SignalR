# Running Tests

Once you've created your project and done the necessary 
configuration, all that's left is to run it!

The steps are relatively simple:

## package.json

In your `package.json` you will want to create some script
tasks to compile your tests. Then you will want a task to 
run your scripts. 

Here is an example:

```json
...
"scripts": {
    "pretestJest": "fable-splitter -c tests/Fable.Jester.Tests/splitter.config.js",
    "pretestRTL": "fable-splitter -c tests/Fable.ReactTestingLibrary.Tests/splitter.config.js",
    "test": "yarn pretestJest && yarn pretestRTL && yarn jest",
    "testJest": "yarn pretestJest && yarn jest",
    "testRTL": "yarn pretestRTL && yarn jest"
  },
...
```

When you want to run your tests you can simply do `yarn test`.

<Note>I do recommend you use a [FAKE] script to clean your tests directory</Note>

[FAKE]: https://github.com/fsharp/FAKE

### Snapshot Testing

If you are doing snapshot testing you will want to add 
a jest configuration to point to your test directory.

The reason for this is that when doing your snapshot
testing, jest will see the `.js.snap` file in your project
directory, and warn you about having an obsolete snapshot.

Here is an example:

```json
...
  "jest": {
    "roots": [
      "./dist/tests"
    ]
  }
}
```

## Test Output

When it's time to finally run your tests you should see output like this:

<resolved-image source='/images/test.gif' />
