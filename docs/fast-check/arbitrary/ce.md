# Arbitrary Computation Expression

<Note>If you're unfamiliar with computation expressions
I recommend you read this [blog series]</Note>

A computation expression for constructing
and composing Arbitraries is exposed via 
`arbitrary`

The following methods are defined in the builder:
 - Bind
 - Combine
 - Delay
 - For
 - Return
 - ReturnFrom
 - Run
 - TryFinally
 - TryWith
 - Using
 - While
 - Zero

Which can be used like this:

```fsharp
let intTupleArb =
    arbitrary {
        let! i = Arbitrary.Defaults.integer
        let! i2 = Arbitrary.Defaults.integer
        return i,i2
    }
```

[blog series]:https://fsharpforfunandprofit.com/series/computation-expressions.html