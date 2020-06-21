# Array

These are functions to help compose Arbitrary arrays.

This is accessed via:
```fsharp
Arbitrary.Array
```

## ofLength

Signature:
```fsharp
(size: int) (arb: Arbitrary<'T>) -> Arbitrary<'T []>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (arb: Arbitrary<'T>) -> Arbitrary<'T []>
```

## piles

Creates an arbitrary of a collection of a given length 
such that all elements have the given sum.

Signature:
```fsharp
(length: int) (sum: int) -> Arbitrary<'T []>
```

## sequence

Signature:
```fsharp
(arbs: Arbitrary<'T> []) -> Arbitrary<'T []>
```

## shuffle

Creates an arbitrary of a collection that is shuffled.

Signature:
```fsharp
(xs: 'T []) -> Arbitrary<'T []>
```

## shuffledSub

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(originalArray: 'T []) -> Arbitrary<'T []>
```

## shuffledSubOfSize

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T []) -> Arbitrary<'T []>
```

## sub

Creates an arbitrary that is a sub-section of the given collection.

Signature:
```fsharp
(xs: 'T []) -> Arbitrary<'T []>
```

## subOfSize

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T []) -> Arbitrary<'T []>
```

## traverse

Signature:
```fsharp
(f: 'T -> Arbitrary<'U>) (arbs: Arbitrary<'T> []) -> Arbitrary<'U []>
```

## twoDimOf

Creates a array of arrays arbitrary from a given arbitrary.

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<'T [][]>
```

## twoDimOfDim

Creates an array of arrays arbitrary from a given arbitrary.

Signature:
```fsharp
(rows: int) (cols: int) (arb: Arbitrary<'T>) -> Arbitrary<'T [][]>
```
