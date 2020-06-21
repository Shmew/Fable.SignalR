# Defaults

The Arbitrary module has a collection of default Arbitraries
for you to use or compose into new ones.

This is accessible via:
```fsharp
Arbitrary.Defaults
```

## anything

Any type of values.

Returns:
```fsharp
Arbitrary<obj>
```

## ascii

Single ascii characters - char code between 0x00 (included) and 0x7f (included).

Returns:
```fsharp
Arbitrary<char>
```

## asciiString

An [ascii](#ascii) string.

Returns:
```fsharp
Arbitrary<string>
```

## asyncScheduler

A scheduler instance.

See [scheduler](/scheduler) for more details.

Returns:
```fsharp
Arbitrary<AsyncScheduler>
```

## base64

Single base64 characters - A-Z, a-z, 0-9, + or /

Returns:
```fsharp
Arbitrary<char>
```

## base64String

A base64 string will always have a length multiple of 4 (padded with =).

Returns:
```fsharp
Arbitrary<string>
```

## bigInt

Uniformly distributed bigint values.

Returns:
```fsharp
Arbitrary<bigint>
```

## bigUint

Uniformly distributed bigint positive values.

Returns:
```fsharp
Arbitrary<bigint>
```

## boolean

true or false.

Returns:
```fsharp
Arbitrary<bool>
```

## byte

Any byte value.

Returns:
```fsharp
Arbitrary<byte>
```

## char

Single printable ascii characters - char code between 
0x20 (included) and 0x7e (included)

Returns:
```fsharp
Arbitrary<char>
```

## char16bits

Single characters - all values in 0x0000-0xffff can be generated

<Note type="warning">Some generated characters might appear invalid regarding UCS-2 and UTF-16 encoding.</Note>

Indeed values within 0xd800 and 0xdfff constitute surrogate pair 
characters and are illegal without their paired character.

Returns:
```fsharp
Arbitrary<char>
```

## compareBooleanFunc

A comparison boolean function returns:
 - true whenever a < b
 - false otherwise (ie. a = b or a > b)

Returns:
```fsharp
Arbitrary<obj -> obj -> bool>
```

## compareFunc

 A comparison function returns:
 - Negative value whenever a < b.
 - Positive value whenever a > b
 - Zero whenever a and b are equivalent

Comparison functions are transitive: `a < b and b < c => a < c`

They also satisfy: `a < b <=> b > a` and `a = b <=> b = a`

Returns:
```fsharp
Arbitrary<obj -> obj -> int>
```

## dateTime

Any DateTime value.

Returns:
```fsharp
Arbitrary<DateTime>
```

## dateTimeOffset

Any DateTimeOffset value.

Returns:
```fsharp
Arbitrary<DateTimeOffset>
```

## domain

 Having an extension with at least two lowercase characters.

According to [RFC 1034](https://www.ietf.org/rfc/rfc1034.txt),
[RFC 1123](https://www.ietf.org/rfc/rfc1123.txt) and 
[WHATWG URL Standard](https://url.spec.whatwg.org/).

Returns:
```fsharp
Arbitrary<string>
```

## double

Floating point numbers between 0.0 (included) and 1.0 (excluded) - accuracy of `1 / 2**53`.

Returns:
```fsharp
Arbitrary<float>
```

## emailAddress

According to [RFC 5322](https://www.ietf.org/rfc/rfc5322.txt)

Returns:
```fsharp
Arbitrary<string>
```

## exn

A System.Exception with random `message` string.

Returns:
```fsharp
Arbitrary<exn>
```

## float

Floating point numbers between 0.0 (included) and 
1.0 (excluded) - accuracy of `1 / 2**24`.

Returns:
```fsharp
Arbitrary<float>
```

## float32

Any float32 value.

Returns:
```fsharp
Arbitrary<float32>
```

## fullUnicode

Single unicode characters - any of the code points defined in the unicode standard.

<Note type="warning">Generated values can have a length greater than 1, so the generated 
type is a string.</Note>

Returns:
```fsharp
Arbitrary<string>
```

## fullUnicodeString

A [fullUnicode](#fullUnicode) string.

Returns:
```fsharp
Arbitrary<string>
```

## guid

Any guid value.

Returns:
```fsharp
Arbitrary<Guid>
```

## hexa

Single hexadecimal characters - 0-9 or a-f

Returns:
```fsharp
Arbitrary<char>
```

## hexaString

A [hexa](#hexa) string.

Returns:
```fsharp
Arbitrary<string>
```

## int16

Any int16 value.

Returns:
```fsharp
Arbitrary<int16>
```

## integer

Any integer value.

Returns:
```fsharp
Arbitrary<int>
```

## int64

Integers between Number.MIN_SAFE_INTEGER (included) 
and Number.MAX_SAFE_INTEGER (included).

Returns:
```fsharp
Arbitrary<int64>
```

## ipV4

Valid IP v4.

Following [RFC 3986](https://tools.ietf.org/html/rfc3986#section-3.2.2).

Returns:
```fsharp
Arbitrary<string>
```

## ipV4Extended

Valid IP v4 according to WhatWG.

Following the WhatWG [specification for web-browsers](https://url.spec.whatwg.org/)

There is no equivalent for IP v6 according to the [IP v6 parser](https://url.spec.whatwg.org/#concept-ipv6-parser)

Returns:
```fsharp
Arbitrary<string>
```

## ipV6

Valid IP v6.

Following [RFC 3986](https://tools.ietf.org/html/rfc3986#section-3.2.2)

Returns:
```fsharp
Arbitrary<string>
```

## json

JSON compliant string.

Returns:
```fsharp
Arbitrary<string>
```

## jsonObject

JSON compliant values.

Returns:
```fsharp
Arbitrary<obj>
```

## lorem

Lorem ipsum strings of words.

Returns:
```fsharp
Arbitrary<string>
```

## maxSafeInteger

Integers between Number.MIN_SAFE_INTEGER 
(included) and Number.MAX_SAFE_INTEGER (included).

Returns:
```fsharp
Arbitrary<int64>
```

## maxSafeNat

positive integers between 0 (included) and 
Number.MAX_SAFE_INTEGER (included).

Returns:
```fsharp
Arbitrary<int64>
```

## object

Any object.

Returns:
```fsharp
Arbitrary<obj>
```

## promiseScheduler

A scheduler instance.

See [scheduler](/scheduler) for more details.

Returns:
```fsharp
Arbitrary<PromiseScheduler>
```

## regex

Any valid `Regex`.

Returns:
```fsharp
Arbitrary<Regex>
```

## sbyte

Any sbyte value.

Returns:
```fsharp
Arbitrary<sbyte>
```

## string

Any string value.

Returns:
```fsharp
Arbitrary<string>
```

## string16bits

Any 16-bit string.

Returns:
```fsharp
Arbitrary<string>
```

## timeSpan

Any TimeSpan.

Returns:
```fsharp
Arbitrary<TimeSpan>
```

## unicode

Single unicode characters defined in the BMP plan - 
char code between 0x0000 (included) and 0xffff 
(included) and without the range 0xd800 to 0xdfff 
(surrogate pair characters).

Returns:
```fsharp
Arbitrary<char>
```

## unicodeJson

JSON strings with unicode support.

Returns:
```fsharp
Arbitrary<string>
```

## unicodeJsonObject

JSON compliant values with unicode support.

Returns:
```fsharp
Arbitrary<obj>
```

## unicodeString

Any unicode compliant string.

Returns:
```fsharp
Arbitrary<string>
```

## uint16

Any uint16 value.

Returns:
```fsharp
Arbitrary<uint16>
```

## uint32

Any uint32 value.

Returns:
```fsharp
Arbitrary<uint32>
```

## uint64

Some unint64 values.

<Note>Due to number limitations this is just an alias and cast from maxSafeNat.</Note>

Returns:
```fsharp
Arbitrary<uint64>
```

## uuid

UUID from v1 to v5.

According to [RFC 4122](https://tools.ietf.org/html/rfc4122)

No mixed case, only lower case digits (0-9a-f).

Returns:
```fsharp
Arbitrary<string>
```

## webAuthority

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)

`authority = [ userinfo "@" ] host [ ":" port ]`

Returns:
```fsharp
Arbitrary<string>
```

## webFragments

Fragments of an URI (web included).

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)

eg.: In the url `https://domain/plop?page=1#hello=1&world=2`, 
`?hello=1&world=2` are query parameters.

Returns:
```fsharp
Arbitrary<string>
```

## webQueryParameters

Query parameters of an URI (web included).

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)

eg.: In the url `https://domain/plop/?hello=1&world=2`, 
`?hello=1&world=2` are query parameters.

Returns:
```fsharp
Arbitrary<string>
```

## webSegment

Internal segment of an URI (web included).

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)

eg.: In the url `https://github.com/dubzzz/fast-check/`, 
`dubzzz` and `fast-check` are segments.

Returns:
```fsharp
Arbitrary<string>
```

## webUrl

According to [RFC 3986](https://www.ietf.org/rfc/rfc3986.txt)
and [WHATWG URL Standard](https://url.spec.whatwg.org/).

Returns:
```fsharp
Arbitrary<string>
```
