# Expect

Jest and its ecosystem has many matchers available
to make writing tests as easy as possible.

To get started you need to start your expect:

```fs
Jest.expect(someValue)
```

From this point you can "dot" into all the matchers
that are valid for the given input.

## not

Inverts the pass/fail status of a matcher.

Usage:
```fsharp
Jest.expect(1).not.toBe(2)
```

## rejects

<Note>This is only available when the assertion is a promise</Note>

Unwrap the reason of a rejected promise so any other 
matcher can be chained. If the promise is fulfilled 
the assertion fails.

Usage:
```fsharp
Jest.expect(myPromise).rejects.toThrow()
```

## resolves

<Note>This is only available when the assertion is a promise</Note>

Unwrap the value of a fulfilled promise so any other 
matcher can be chained. If the promise is rejected 
the assertion fails.

This is automatically applied for `Async<'T>` values.

Usage:
```fsharp
Jest.expect(myPromise).resolves.toBe(1)
```

## toBe

Compare primitive values or to check referential identity 
of object instances. It calls Object.is to compare values, 
which is even better for testing than === strict equality 
operator.

Signature:
```fsharp
(value: 'T)
```

Usage:
```fsharp
Jest.expect(1).toBe(2)
```

## toBeCloseTo

<Note>This is only available when the assertion is an `int`, `float`, or `decimal`</Note>

Compare floats or decimals for approximate equality.

Signature:
```fsharp
(number: decimal, ?numDigits: int)
(number: float, ?numDigits: int)
```

Usage:
```fsharp
Jest.expect(System.Math.PI).toBeCloseTo(3.14, 2)
```

## toBeChecked

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given element is checked. 

It accepts an input of type checkbox or radio and elements with 
a role of checkbox, radio, or switch with a valid aria-checked 
attribute of "true" or "false".

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeChecked()
```

## toBeDefined

Check that a variable is not undefined.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect("hi").toBeDefined()
```

## toBeDisabled

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element is disabled from the 
user's perspective.

It matches if the element is a form control and the disabled 
attribute is specified on this element or the element is a 
descendant of a form element with a disabled attribute.

According to the specification, the following elements can be 
actually disabled: button, input, select, textarea, optgroup, 
option, and fieldset.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeDisabled()
```

## toBeEnabled

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element is not disabled from the user's perspective.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeEnabled()
```

## toBeEmpty

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element has content or not.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeEmpty()
```

## toBeFalsy

Matcher for when you don't care what a value is and you want to 
ensure a value is false in a boolean context.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(()).toBeFalsy()
```

## toBeGreaterThan

<Note>This is only available when the assertion is a number primative such as `int` or `float`</Note>

To compare received > expected.

Signature:
```fsharp
(number: decimal)
(number: float)
(number: int)
(number: int64)
```

Usage:
```fsharp
Jest.expect(3).toBeGreaterThan(2)
```

## toBeGreaterThanOrEqual

<Note>This is only available when the assertion is a number primative such as `int` or `float`</Note>

To compare received >= expected.

Signature:
```fsharp
(number: decimal)
(number: float)
(number: int)
(number: int64)
```

Usage:
```fsharp
Jest.expect(3).toBeGreaterThanOrEqual(3)
```

## toBeInTheDocument

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element is present in the document or not.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeInTheDocument()
```

## toBeInvalid

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check if a form element, or the entire form, is currently invalid.

An input, select, textarea, or form element is invalid if it has an 
aria-invalid attribute with no value or a value of "true", or if the 
result of checkValidity() is false.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeInvalid()
```

## toBeLessThan

<Note>This is only available when the assertion is a number primative such as `int` or `float`</Note>

To compare received < expected.

Signature:
```fsharp
(number: decimal)
(number: float)
(number: int)
(number: int64)
```

Usage:
```fsharp
Jest.expect(2).toBeLessThan(3)
```

## toBeLessThanOrEqual

<Note>This is only available when the assertion is a number primative such as `int` or `float`</Note>

To compare received <= expected.

Signature:
```fsharp
(number: decimal)
(number: float)
(number: int)
(number: int64)
```

Usage:
```fsharp
Jest.expect(3).toBeLessThanOrEqual(3)
```

## toBeNaN

Check that a value is NaN.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(Fable.Core.JS.NaN).toBeNaN()
```

## toBeNull

Check that something is null.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myString).toBeNull()
```

## toBeRequired

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check if a form element is currently required.

An element is required if it is having a required or 
aria-required="true" attribute.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeRequired()
```

## toBeTruthy

Matcher for when you don't care what a value is and you want to 
ensure a value is true in a boolean context.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect([|1;2;3|]).toBeTruthy()
```

## toBeUndefined

Check that a variable is undefined.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myObj).toBeUndefined()
```

## toBeValid

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check if the value of a form element, or the entire form, is currently valid.

An input, select, textarea, or form element is valid if it has no aria-invalid 
attribute or an attribute value of "false". The result of checkValidity() must 
also be true.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeValid()
```

## toBeVisible

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

This allows you to check if an element is currently visible to the user.

An element is visible if all the following conditions are met:

* Does not have its css property display set to none.
* Does not have its css property visibility set to either hidden or collapse.
* Does not have its css property opacity set to 0.
* The parent element is also visible (and so on up to the top of the DOM tree).
* Does not have the hidden attribute.
* If `<details />` it has the open attribute.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toBeVisible()
```

## toContain

Check that an item is in a collection.

Note that this matcher will check *both* the 
index and values if given a int or float. 

These will *both* pass:

```fsharp
expect([1;2;5]).toContain(5) 

expect([1;2;5]).toContain(3)
```

If you do not want this, see [toContainEqual](#tocontainequal).

Signature:
```fsharp
(item: 'T)
```

Usage:
```fsharp
Jest.expect([1;2;3]).toContain(1)
Jest.expect([1;2;8]).toContain(8)
Jest.expect(["I";"Like";"Pie"]).toContain("Pie")
```

## toContainElement

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element contains another element as a descendant or not.

Signature:
```fsharp
(element: HTMLElement)
(element: Node)
```

Usage:
```fsharp
Jest.expect(myElement).toContainElement(myExpectedElement)
```

## toContainHTML

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether a string representing a HTML element is contained in another element

Signature:
```fsharp
(htmlText: string)
```

Usage:
```fsharp
Jest.expect(myElement).toContainHTML("<div></div>")
```

## toContainEqual

Check that an item with a specific structure and 
values is contained in a collection.

Signature:
```fsharp
(item: 'T)
```

Usage:
```fsharp
let testList = [
    {| One = 1
        Two = 2
        Three = 3 |}
    {| One = 4
        Two = 5
        Three = 6 |}
]

Jest.expect(testList).toContainEqual({| One = 1; Two = 2; Three = 3 |})
```

## toEqual

Compare recursively all properties of object instances 
(also known as "deep" equality). It calls Object.is to 
compare primitive values, which is even better for 
testing than === strict equality operator.

Signature:
```fsharp
(value: 'T)
```

Usage:
```fsharp
Jest.expect(true).toEqual(true)
```

## toHaveAttribute

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given element has an attribute or not. 

You can also optionally check that the attribute has a specific expected value 
or partial match using [expect.stringContaining] or [expect.stringMatching].

Signature:
```fsharp
(attr: string, ?value: obj)
```

Usage:
```fsharp
Jest.expect(myElement).toHaveAttribute("style")
```

## toHaveClass

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given element has certain classes within its class attribute.

You must provide at least one class, unless you are asserting that an element does 
not have any classes.

Signature:
```fsharp
([<ParamArray>] classNames: string [])
```

Usage:
```fsharp
Jest.expect(myElement).toHaveClass("myDiv")
```

## toHaveDescription

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given element has a description or not.

An element gets its description via the aria-describedby attribute. Set this to 
the id of one or more other elements. These elements may be nested inside, be 
outside, or a sibling of the passed in element.

Whitespace is normalized. Using multiple ids will join the referenced elements’ 
text content separated by a space.

Signature:
```fsharp
(value: Regex)
(value: string)
```

Usage:
```fsharp
Jest.expect(myElement).toHaveDescription("Hello!")
```

## toHaveDisplayValue

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given form element has the specified displayed value (the 
one the end user will see). 

It accepts input, select and textarea elements with the exception of 
`<input type="checkbox">` and `<input type="radio">`, which can be meaningfully 
matched only using [toBeChecked] or [toHaveFormValues].

Signature:
```fsharp
(value: Regex)
(value: string)
(values: ResizeArray<Regex>)
(values: ResizeArray<string>)
(values: Regex [])
(values: string [])
(values: Regex list)
(values: string list)
(values: Regex seq)
(values: string seq)
```

Usage:
```fsharp
Jest.expect(myElement).toHaveDisplayValue("Hello!")
```

## toHaveFocus

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether an element has focus or not.

Signature:
```fsharp
unit
```

Usage:
```fsharp
Jest.expect(myElement).toHaveClass()
```

## toHaveFormValues

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check if a form or fieldset contains form controls for each given name, and having the specified value.

Note that this matcher can *only* be invoked on a form or fieldset element.

Signature:
```fsharp
(expectedValues: obj)
(expectedValues: ResizeArray<string * obj>)
(expectedValues: (string * obj) [])
(expectedValues: (string * obj) list)
(expectedValues: (string * obj) seq)
```

Usage:
```fsharp
let formValues = [
    "username", box "Shmew"
    "remember me", box true
]

Jest.expect(myElement).toHaveFormValues(formValues)
```

## toHaveLength

Check that an object has a .length property and it is set 
to a certain numeric value.

Signature:
```fsharp
(length: int)
```

Usage:
```fsharp
Jest.expect("hi").toHaveLength(2)
```

## toHaveProperty

You can provide an optional value argument to compare the received 
property value (recursively for all properties of object instances, 
also known as deep equality, like the toEqual matcher).

Signature:
```fsharp
(keyPath: string, ?value: 'T)
(keyPath: ResizeArray<string>)
(keyPath: ResizeArray<string>, value: 'T)
(keyPath: string [])
(keyPath: string [], value: 'T)
(keyPath: string list)
(keyPath: string list, value: 'T)
(keyPath: string seq)
(keyPath: string seq, value: 'T)
```

Usage:
```fsharp
Jest.expect({| test = "testValue" |} |> toPlainJsObj).toHaveProperty("test")
```

## toHaveStyle

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check if a certain element has some specific css properties with specific values applied. 

It matches only if the element has all the expected properties applied, not just some of them.

Signature:
```fsharp
(css: obj)
(css: string)
(css: IStyleAttribute)
(css: IStyleAttribute list)
```

Usage:
```fsharp
let divStyle = style.backgroundColor (color.red)

Jest.expect(myElement).toHaveStyle(divStyle)
```

## toHaveTextContent

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given element has a text content or not.

When a string argument is passed through, it will perform a partial case-sensitive match to 
the element content.

To perform a case-insensitive match, you can use a RegExp with the /i modifier.

If you want to match the whole content, you can use a RegExp to do it.

Signature:
```fsharp
(text: string)
(text: string, normalizeWhitespace: bool)
(text: Regex)
(text: Regex, normalizeWhitespace: bool)
```

Usage:
```fsharp
Jest.expect(myElement).toHaveTextContent(Regex("\\w+?"))
```

## toHaveValue

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Check whether the given form element has the specified value. 

It accepts input, select and textarea elements with the exception of of `<input type="checkbox">`
and `<input type="radio">`, which can be meaningfully matched only using [toBeChecked] or [toHaveFormValues].

Signature:
```fsharp
(value: bool)
(value: float)
(value: System.Guid)
(value: int)
(value: string)
(value: ResizeArray<string>)
(value: string [])
(value: string list)
(value: string seq)
```

Usage:
```fsharp
Jest.expect(myElement).toHaveValue("hunter2")
```

## toMatch

<Note>This is only available when the assertion is a `string`</Note>

Check that a string matches a string or regular expression.

When using a string it is the same as doing "mystring".Contains(value)

Signature:
```fsharp
(value: string)
(value: Regex)
```

Usage:
```fsharp
Jest.expect("test").toMatch("test")
```

## toMatchObject

Check that a JavaScript object matches a subset of the properties of an object.

Signature:
```fsharp
(object: 'T)
```

Usage:
```fsharp
let actual = {| test = "hi" |}
let expected = {| test = "hi" |}

Jest.expect(actual).toMatchObject(expected)
```

## toMatchSnapshot

<Note>This is only available when the assertion is an `HTMLElement` or `Node`</Note>

Ensures that a value matches the most recent snapshot.

Signature:
```fsharp
(?propertyMatchers, ?hint)
```

Usage:
```fsharp
Jest.expect(myElement).toMatchSnapshot()
```

## toStrictEqual

Check that an object has the same types as well as structure.

Differences from .toEqual:

Keys with undefined properties are checked. e.g. {a: undefined, b: 2} does 
not match {b: 2} when using .toStrictEqual.

Array sparseness is checked. e.g. [, 1] does not match [undefined, 1] when 
using .toStrictEqual.

Object types are checked to be equal. e.g. A class instance with fields a 
and b will not equal a literal object with fields a and b.

Signature:
```fsharp
(value: 'T)
```

Usage:
```fsharp
let actual = {| test = "hi" |}
let expected = {| test = "hiya" |}

Jest.expect(actual).not.toStrictEqual(expected)
```

## toThrow

Check that a function throws when called.

Signature:
```fsharp
unit
(err: exn)
(err: Regex)
(err: string)
```

Usage:
```fsharp
Jest.expect(myFailingFunction).toThrow(System.Exception("uh oh!"))
```

## Where are the other expect matchers?

Some matchers were chosen not to be included in this
library. 

The reason for this being three-fold: 

* The missing matchers are things like testing that a mocked call
was called N number of times. Well desgined F# code *shouldn't need 
mocks*. It's my opinion that if you need a mock for a test, your 
test is *already failing*. If you find that you *really* need this 
functionality, create an [issue] and we can review/discuss it.
* The F# language invalidates the need for the assertion.
* It's additional work to maintain. ;)

[issue]: https://github.com/Shmew/Fable.Jester/issues/new/choose
[expect.stringContaining]: /jest/expect-helpers#stringcontaining
[expect.stringMatching]: /jest/expect-helpers#stringmatching
