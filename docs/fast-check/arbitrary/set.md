# Set

These are functions to help compose Arbitrary Sets.

This is accessed via:
```fsharp
Arbitrary.Set
```

## ofLength

Signature:
```fsharp
(length: int) (arb: Arbitrary<'T>) -> Arbitrary<Set<'T>>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (arb: Arbitrary<'T>) -> Arbitrary<Set<'T>>
```
