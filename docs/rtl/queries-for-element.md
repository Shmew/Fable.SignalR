# queriesForElement

The `queriesForElement` type exposes many methods of querying
the DOM for elements to test/compare against.

There are six different categories of queries:
 * [getBy](#getby)
 * [getAll](#getall)
 * [queryBy](#queryby)
 * [queryAll](#queryall)
 * [findBy](#findby)
 * [findAll](#findall)

Each of the query types have different targets for the attribute 
you want to query for:
 * [AltText](#alttext)
 * [DisplayValue](#displayvalue)
 * [LabelText](#labeltext)
 * [PlaceholderText](#placeholdertext)
 * [Role](#role)
 * [Text](#text)
 * [TestId](#testid)
 * [Title](#title)

## Query Types

### getBy

The getBy query type returns the first matching node for a
query. 
 
This will throw an error if no elements match, or
if more than one match is found.

### getAll

The getAll query type returns a list of all matching nodes
of the query. 
 
This will throw an error if no elements match.

### queryBy

The queryBy query type returns the first matching node for a
query. 
 
This will return an `Option` for the element.

### queryAll

The queryAll query type returns a list of all matching nodes
of the query. 
 
This will return an empty list if there are no matches.

### findBy

The findBy query type returns the first matching node for a
query. 
 
This returns a `JS.Promise` for the element which will reject
if no matches are found after the default timeout of 4500ms.

### findAll

The findAll query type returns a list of all matching nodes
of the query. 
 
This returns a `JS.Promise` for the matches which will reject
if no matches are found after the default timeout of 4500ms.

## Query Targets

### AltText

Signature:
```fsharp
(matcher: string, ?selector : string, ?ignore: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?selector : string, ?ignore: string, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?selector : string, ?ignore: string, ?exact: bool, 
    ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.getByAltText("howdy!")
```

### DisplayValue

Signature:
```fsharp
(matcher: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?exact: bool, ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.getAllByDisplayValue("howdy!")
```

### LabelText

Signature:
```fsharp
(matcher: string, ?selector : string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?selector : string, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?selector : string, ?exact: bool, 
    ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.queryByLabelText("howdy!")
```

### PlaceholderText

Signature:
```fsharp
(matcher: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?exact: bool, ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.queryAllByPlaceholderText("howdy!")
```

### Role

Signature:
```fsharp
(matcher: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?exact: bool, ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.findByRole("howdy!")
```

### Text

Signature:
```fsharp
(matcher: string, ?selector : string, ?ignore: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?selector : string, ?ignore: string, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?selector : string, ?ignore: string, ?exact: bool, 
    ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.findAllByText("howdy!")
```

### TestId

Signature:
```fsharp
(matcher: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?exact: bool, ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.getByTestId("howdy!")
```

### Title

Signature:
```fsharp
(matcher: string, ?exact: bool, ?normalizer: string -> string)
(matcher: Regex, ?exact: bool, ?normalizer: string -> string)
(matcher: string * HTMLElement -> bool, ?exact: bool, ?normalizer: string -> string)
```

Usage:
```fsharp
RTL.screen.getAllByTitle("howdy!")
```

