# Constrained Defaults

Provides functions to easily customize behavior of many of the [Defaults](#Defaults).

This is accessible via:
```fsharp
Arbitrary.ConstrainedDefaults
```

Parameters that take a `I_ConstraintProperty` list are accessible via:
```fsharp
Constraints
```

```fsharp
type Command =
    /// Maximum number of commands to execute on the model.
    maxCommands: (value: int)

    /// Disable replaying of the model test.
    disableReplayLog: (value: bool)
        
    /// Set the reply path.
    replayPath: (value: string)
    
type Date =
    /// Minimum value for date (inclusive).
    min: (value: DateTime)

    /// Maximum value for date (inclusive).
    max: (value: DateTime)

type Obj<'T> =
    /// Maximal depth allowed.
    maxDepth: (value: int)

    /// Maximal number of keys.
    maxKeys: (value: int)

    /// Arbitrary for keys.
    /// 
    /// Default for `key` is: `Arbitrary.Defaults.string`
    key: (value: Arbitrary<string>)

    /// Arbitrary for values.
    values: (value: Arbitrary<'T> [])
    /// Arbitrary for values.
    values: (value: Arbitrary<'T> list)
    /// Arbitrary for values.
    values: (value: Arbitrary<'T> seq)
    /// Arbitrary for values.
    values: (value: ResizeArray<Arbitrary<'T>>)

    /// Also generate boxed versions of values.
    withBoxedValues: (value: bool)

    /// Also generate Set.
    withSet: (value: bool)

    /// Also generate Map.
    withMap: (value: bool)

    /// Also generate string representations of object instances.
    withObjectString: (value: bool)

    /// Also generate object with null prototype.
    withNullPrototype: (value: bool)

module Uuid =
    type VersionNumber =
        N1
        N2
        N3
        N4
        N5

type WebAuthority =
    /// Enable IPv4 in host.
    withIPv4: (value: bool)

    /// Enable extended IPv4 format.
    withIPv4Extended: (value: bool)
        
    /// Enable IPv6 in host.
    withIPv6: (value: bool)
        
    /// Enable port suffix.
    withPort: (value: bool)
        
    /// Enable user information prefix.
    withUserInfo: (value: bool)

type WebUrl =
    /// Enforce specific schemes, eg.: http, https.
    validSchemes: (value: string list)

    /// Settings: for webAuthority.
    authoritySettings: (properties: IWebAuthorityConstraintProperty list)

    /// Enable query parameters in the generated url.
    withQueryParameters: (value: bool)

    /// Enable fragments in the generated url.
    withFragments: (value: bool)
```

## anything

Any type of values.

Signature:
```fsharp
(constraints: IObjConstraintProperty list)
```

Returns:
```fsharp
Arbitrary<obj>
```

## asciiString

An [ascii](#ascii) string.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## asyncScheduler

Creates a scheduler with a wrapped act function.

See [scheduler](/scheduler) for more details.

Signature:
```fsharp
(act: ((unit -> Async<unit>) -> Async<unit>))
```

Returns:
```fsharp
Arbitrary<AsyncScheduler>
```

## base64String

A base64 string will always have a length multiple of 4 (padded with =)

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## bigInt

All possible bigint between min (included) and max (included).

Signature:
```fsharp
(min: bigint, max: bigint)
```

Returns:
```fsharp
Arbitrary<bigint>
```

## bigIntN

All possible bigint between -2^(n-1) (included) and 2^(n-1)-1 (included).

Signature:
```fsharp
(min: bigint, max: bigint)
```

Returns:
```fsharp
Arbitrary<bigint>
```

## bigUint

All possible bigint between 0 (included) and max (included).

Signature:
```fsharp
(max: bigint)
```

Returns:
```fsharp
Arbitrary<bigint>
```

## bigUintN

All possible bigint between 0 (included) and 2^n -1 (included).

Signature:
```fsharp
(n: int)
```

Returns:
```fsharp
Arbitrary<bigint>
```

## date

Any DateTime value.

Signature:
```fsharp
(constraints: IDateConstraintProperty list)
```

Returns:
```fsharp
Arbitrary<DateTime>
```

## double

Floating point numbers between 0.0 (included) and max (excluded) - accuracy of `max / 2**53`.

and

Floating point numbers between min (included) and max (excluded) - accuracy of `(max - min) / 2**53`.


Signature:
```fsharp
 (max: float)
 (min: float, max: float)
```

Returns:
```fsharp
Arbitrary<float>
```

## float

Floating point numbers between 0.0 (included) and max (excluded) - accuracy of `max / 2**24`.

and

Floating point numbers between min (included) and max (excluded) - accuracy of `(max - min) / 2**24`.

Signature:
```fsharp
(max: float)
(min: float, max: float)
```

Returns:
```fsharp
Arbitrary<float>
```

## fullUnicodeString

A [fullUnicode](#fullUnicode) string.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## hexaString

A [hexa](#hexa) string.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## integer

Any integer value.

Signature:
```fsharp
(max: int)
(min: int, max: int)
```

Returns:
```fsharp
Arbitrary<int>
```

## json

JSON strings with a maximal depth.

Signature:
```fsharp
(maxDepth: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## jsonObject

JSON compliant values with a maximal depth.

Signature:
```fsharp
(maxDepth: int)
```

Returns:
```fsharp
Arbitrary<obj>
```

## lorem

Lorem ipsum string of words with maximal number of words.

and

Lorem ipsum string of words or sentences with maximal number of words or sentences.

Signature:
```fsharp
(maxWordsCount: float)
(maxWordsCount: float, sentencesMode: bool)
```

Returns:
```fsharp
Arbitrary<string>
```

## object

Any object.

Signature:
```fsharp
(constraints: IObjConstraintProperty list)
```

Returns:
```fsharp
Arbitrary<obj>
```

## promiseScheduler

Creates a scheduler with a wrapped act function.

See [scheduler](/scheduler) for more details.

Signature:
```fsharp
(act: ((unit -> JS.Promise<unit>) -> JS.Promise<unit>))
```

Returns:
```fsharp
Arbitrary<PromiseScheduler>
```

## string

Any string value.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## string16bits

Any 16-bit string.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## unicodeJson

JSON strings with unicode support and a maximal depth.

Signature:
```fsharp
(maxDepth: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## unicodeJsonObject

JSON compliant values with unicode support and a maximal depth.

Signature:
```fsharp
(maxDepth: int)
```

Returns:
```fsharp
Arbitrary<obj>
```

## unicodeString

Any unicode compliant string.

Signature:
```fsharp
(maxLength: int)
(minLength: int, maxLength: int)
```

Returns:
```fsharp
Arbitrary<string>
```

## uuidV

UUID of a given version (in v1 to v5).

According to [RFC 4122](https://tools.ietf.org/html/rfc4122).

No mixed case, only lower case digits (0-9a-f).

Signature:
```fsharp
(versionNumber: IUuidVersionConstraintProperty)
```

Returns:
```fsharp
Arbitrary<string>
```

## webAuthority

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)

`authority = [ userinfo "@" ] host [ ":" port ]`

Signature:
```fsharp
(constraints: IWebAuthorityConstraintProperty list)
```

Returns:
```fsharp
Arbitrary<string>
```

## webUrl

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)
and [WHATWG URL Standard](https://url.spec.whatwg.org/).

Signature:
```fsharp
(constraints: IWebUrlConstraintProperty list)
```

Returns:
```fsharp
Arbitrary<string>
```
