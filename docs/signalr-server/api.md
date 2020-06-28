# Expect

Jest exposes an `expect` object that is used to make 
creating assertions easier.

## addSnapshotSerializer

Add a module that formats application-specific data structures.

Signature:
```fsharp
(serializer: obj) -> unit
```

Usage:
```fsharp
expect.addSnapshotSerializer(import "serializer" "my-serializer-module")
```

## any

Matches anything that was created with the given constructor.

Signature:
```fsharp
(value: 'Constructor)
```

Usage:
```fsharp
Jest.expect(myConstructedObj).toBe(expect.any(myConstructor)
```

## anything

Matches anything but null or undefined.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(1).toBe(expect.anything())
```

## arrayContaining

Matches a received collection which contains all of the elements 
in the expected array. That is, the expected collection is a 
subset of the received collection. Therefore, it matches a 
received collection which contains elements that are not in the 
expected collection.

Signature:
```fsharp
(values: ResizeArray<'T>)
(values: 'T [])
(values: 'T list)
(values: 'T seq)
```

Usage:
```fsharp
let arraySample = [| 1;2;3;4;5;6;7 |] 

Jest.expect(arraySample).toEqual(expect.arrayContaining([| 2;3;4 |]))
```

## assertions

Verifies that a certain number of assertions are called 
during a test.

Signature:
```fsharp
(number: int) -> unit
```

Usage:
```fsharp
expect.assertions(2)
```

## extend

Adds custom matchers to Jest.

See the [jest documentation](https://jestjs.io/docs/en/expect) for list 
of `this` properties and methods 

Signature:
```fsharp
(matchers: unit -> MatcherResponse) -> unit
(matchers: 'a -> MatcherResponse) -> unit
(matchers: 'a -> 'b -> MatcherResponse) -> unit
...

/// The response structure of matcher extensions.
type MatcherResponse =
    abstract pass: bool
    abstract message: unit -> string
```

Usage:
```fsharp
expect.extend(myExtension)
```

## hasAssertions

Verifies that at least one assertion is called during a test.

Signature:
```fsharp
unit -> unit
```

Usage:
```fsharp
expect.hasAssertions()
```

## not

Inverts the pass/fail status of a matcher.

Usage:
```fsharp
Jest.expect("test").toEqual(expect.not.stringContaining("whoa"))
```

## objectContaining

Matches any received object that recursively matches the 
expected properties. That is, the expected object is a 
subset of the received object. Therefore, it matches a 
received object which contains properties that are 
present in the expected object.

Signature:
```fsharp
(value: obj)
```

Usage:
```fsharp
let actual = 
    {| someValue = "test"
       someOtherValue = "testValue" |} 
    |> Fable.Core.JsInterop.toPlainJsObj

let expected = 
    {| someValue = "test" |} 
    |> Fable.Core.JsInterop.toPlainJsObj

Jest.expect(actual).toEqual(expect.objectContaining(expected))
```

## stringContaining

Matches the received value if it is a string that 
contains the exact expected string.

Signature:
```fsharp
(value: string)
```

Usage:
```fsharp
Jest.expect("test").toEqual(expect.stringContaining("te"))
```

## stringMatching

Matches the received value if it is a string that matches 
the expected string or regular expression.

Signature:
```fsharp
(value: string)
(value: Regex)
```

Usage:
```fsharp
Jest.expect("test").toEqual(expect.stringMatching(Regex("test")))
```
