# Map

These are functions to help compose Arbitrary Maps.

This is accessed via:
```fsharp
Arbitrary.Map
```

## ofLength

Signature:
```fsharp
(length: int) (key: Arbitrary<'Key>) (value: Arbitrary<'Value>) -> Arbitrary<Map<'Key,'Value>>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (key: Arbitrary<'Key>) (value: Arbitrary<'Value>) 
    -> Arbitrary<Map<'Key,'Value>>
```
