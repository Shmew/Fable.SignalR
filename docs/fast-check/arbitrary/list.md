# List

These are functions to help compose Arbitrary lists.

This is accessed via:
```fsharp
Arbitrary.List
```

## ofLength

Signature:
```fsharp
(size: int) (arb: Arbitrary<'T>) -> Arbitrary<'T list>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (arb: Arbitrary<'T>) -> Arbitrary<'T list>
```

## piles

Creates an arbitrary of a collection of a given length 
such that all elements have the given sum.

Signature:
```fsharp
(length: int) (sum: int) -> Arbitrary<'T list>
```

## sequence

Signature:
```fsharp
(arbs: Arbitrary<'T> list) -> Arbitrary<'T list>
```

## shuffle

Creates an arbitrary of a collection that is shuffled.

Signature:
```fsharp
(xs: 'T list) -> Arbitrary<'T list>
```

## shuffledSub

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(originalArray: 'T list) -> Arbitrary<'T list>
```

## shuffledSubOfSize

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T list) -> Arbitrary<'T list>
```

## sub

Creates an arbitrary that is a sub-section of the given collection.

Signature:
```fsharp
(xs: 'T list) -> Arbitrary<'T list>
```

## subOfSize

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T list) -> Arbitrary<'T list>
```

## traverse

Signature:
```fsharp
(f: 'T -> Arbitrary<'U>) (arbs: Arbitrary<'T> list) -> Arbitrary<'U list>
```

## twoDimOf

Creates a list of lists arbitrary from a given arbitrary.

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<'T list list>
```

## twoDimOfDim

Creates an list of lists arbitrary from a given arbitrary.

Signature:
```fsharp
(rows: int) (cols: int) (arb: Arbitrary<'T>) -> Arbitrary<'T list list>
```
