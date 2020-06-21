# Seq

These are functions to help compose Arbitrary sequences.

This is accessed via:
```fsharp
Arbitrary.Seq
```

## ofLength

Signature:
```fsharp
(size: int) (arb: Arbitrary<'T>) -> Arbitrary<'T seq>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (arb: Arbitrary<'T>) -> Arbitrary<'T seq>
```

## piles

Creates an arbitrary of a collection of a given length 
such that all elements have the given sum.

Signature:
```fsharp
(length: int) (sum: int) -> Arbitrary<'T seq>
```

## sequence

Signature:
```fsharp
(arbs: Arbitrary<'T> seq) -> Arbitrary<'T seq>
```

## shuffle

Creates an arbitrary of a collection that is shuffled.

Signature:
```fsharp
(xs: 'T seq) -> Arbitrary<'T seq>
```

## shuffledSub

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(originalArray: 'T seq) -> Arbitrary<'T seq>
```

## shuffledSubOfSize

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T seq) -> Arbitrary<'T seq>
```

## sub

Creates an arbitrary that is a sub-section of the given collection.

Signature:
```fsharp
(xs: 'T seq) -> Arbitrary<'T seq>
```

## subOfSize

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: 'T seq) -> Arbitrary<'T seq>
```

## traverse

Signature:
```fsharp
(f: 'T -> Arbitrary<'U>) (arbs: Arbitrary<'T> seq) -> Arbitrary<'U seq>
```

## twoDimOf

Creates a seq of seqs arbitrary from a given arbitrary.

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<'T seq seq>
```

## twoDimOfDim

Creates an seq of seqs arbitrary from a given arbitrary.

Signature:
```fsharp
(rows: int) (cols: int) (arb: Arbitrary<'T>) -> Arbitrary<'T seq seq>
```
