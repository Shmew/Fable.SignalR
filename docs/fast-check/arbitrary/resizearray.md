# ResizeArray

These are functions to help compose Arbitrary ResizeArrays.

This is accessed via:
```fsharp
Arbitrary.ResizeArray
```


## ofLength

Signature:
```fsharp
(size: int) (arb: Arbitrary<'T>) -> Arbitrary<ResizeArray<<'T>>
```

## ofRange

Signature:
```fsharp
(min: int) (max: int) (arb: Arbitrary<'T>) -> Arbitrary<ResizeArray<'T>>
```

## piles

Creates an arbitrary of a collection of a given length 
such that all elements have the given sum.

Signature:
```fsharp
(length: int) (sum: int) -> Arbitrary<ResizeArray<'T>>
```

## sequence

Signature:
```fsharp
(arbs: ResizeArray<Arbitrary<'T>>) -> Arbitrary<ResizeArray<'T>>
```

## shuffle

Creates an arbitrary of a collection that is shuffled.

Signature:
```fsharp
(xs: ResizeArray<'T>) -> Arbitrary<ResizeArray<<'T>>
```

## shuffledSub

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(originalArray: ResizeArray<'T>) -> Arbitrary<ResizeArray<'T>>
```

## shuffledSubOfSize

Creates an arbitrary that is shuffled and a sub-section of the given collection.

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: ResizeArray<'T>) -> Arbitrary<ResizeArray<'T>>
```

## sub

Creates an arbitrary that is a sub-section of the given collection.

Signature:
```fsharp
(xs: ResizeArray<'T>) -> Arbitrary<ResizeArray<'T>>
```

## subOfSize

Signature:
```fsharp
(minLength: int) (maxLength: int) (xs: ResizeArray<'T>) -> Arbitrary<ResizeArray<'T>>
```

## traverse

Signature:
```fsharp
(f: 'T -> Arbitrary<'U>) (arbs: ResizeArray<Arbitrary<'T>>) -> Arbitrary<ResizeArray<'U>>
```

## twoDimOf

Creates a ResizeArray of ResizeArrays arbitrary from a given arbitrary.

Signature:
```fsharp
(arb: Arbitrary<'T>) -> Arbitrary<ResizeArray<ResizeArray<'T>>>
```

## twoDimOfDim

Creates an ResizeArray of ResizeArrays arbitrary from a given arbitrary.

Signature:
```fsharp
(rows: int) (cols: int) (arb: Arbitrary<'T>) -> Arbitrary<ResizeArray<ResizeArray<'T>>>
```
