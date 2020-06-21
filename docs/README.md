# Fable.Jester [![Nuget](https://img.shields.io/nuget/v/Fable.Jester.svg?maxAge=0&colorB=brightgreen&label=Fable.Jester)](https://www.nuget.org/packages/Fable.Jester) [![Nuget](https://img.shields.io/nuget/v/Fable.ReactTestingLibrary.svg?maxAge=0&colorB=brightgreen&label=Fable.ReactTestingLibrary)](https://www.nuget.org/packages/Fable.ReactTestingLibrary) [![Nuget](https://img.shields.io/nuget/v/Fable.FastCheck.svg?maxAge=0&colorB=brightgreen&label=Fable.FastCheck)](https://www.nuget.org/packages/Fable.FastCheck) [![Nuget](https://img.shields.io/nuget/v/Fable.FastCheck.Jest.svg?maxAge=0&colorB=brightgreen&label=Fable.FastCheck.Jest)](https://www.nuget.org/packages/Fable.FastCheck.Jest)

Fable bindings for [jest](https://github.com/facebook/jest) and friends for delightful Fable testing:
 * [fast-check](https://github.com/dubzzz/fast-check)
 * [jest-dom](https://github.com/testing-library/jest-dom)
 * [react-testing-library](https://github.com/testing-library/react-testing-library)
 * [user-event](https://github.com/testing-library/user-event)

A quick look:

```fsharp
Jest.describe("my tests", fun () ->
    Jest.test("water is wet", fun () ->
        Jest.expect("test").toBe("test")
        Jest.expect("test").not.toBe("somethingElse")
        Jest.expect("hi").toHaveLength(2)
        Jest.expect("hi").not.toHaveLength(3)
    )
    Jest.test.prop("Is positive", Arbitrary.ConstrainedDefaults.integer(1,100), fun i ->
        Jest.expect(i).toBeGreaterThan(0)
    )
)
```
