# RTL

RTL is the entry point to using all functionality of
Fable.ReactTestingLibrary. 

## act

This is a light wrapper around the react-dom/test-utils act function. 

All it does is forward all arguments to the act function if your 
version of react supports act.

Signature: 
```fsharp 
(callback: unit -> unit) -> unit
```

You can use this like so:

```fsharp
RTL.act(fun () ->
    ReactDOM.render(myReactElement, myContainer)
)
```

## cleanup

Unmounts React trees that were mounted with render.

<Note>This is done automatically in Jest after each test.</Note>

Signature: 
```fsharp 
unit -> unit
```

You can use this like so:

```fsharp
RTL.cleanup()
```

## configure

Set the configuration options.

Signature: 
```fsharp 
(options: IConfigureOption list) -> unit

// ConfigureOptions
type configureOption:
    /// The default value for the hidden option used by getByRole. 
    ///
    /// Defaults to false.
    defaultHidden: (value: bool)

    /// A function that returns the error used when getBy* or getAllBy* fail. 
    /// Takes the error message and container object as arguments.
    getElementError (handler: string * HTMLElement -> exn)

    /// The attribute used by getByTestId and related queries. 
    ///
    /// Defaults to data-testid.
    testIdAttribute (value: string)
```

You can use this like so:

```fsharp
RTL.configure [
    configureOption.defaultHidden true
    configureOption.testIdAttribute "myAttribute"
]
```

## createEvent.`event`

Convenience methods for creating DOM events that can then be fired by 
fireEvent, allowing you to have a reference to the event created: this 
might be useful if you need to access event properties that cannot be 
initiated programmatically (such as timeStamp).

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
/// The event property type is based on the type of event being created.
(element: HTMLElement, ?eventProperties: <EventProperty> list) -> Event
```

You can use this like so:

```fsharp
RTL.createEvent.change(elem, ([ 
    event.target [ 
        prop.value newTime 
    ] 
]))

elem.createEvent.change([ 
    event.target [ 
        prop.value newTime 
    ] 
])
```

## fireEvent

Fires a DOM event.

Signature: 
```fsharp 
(element: HTMLElement, event: #Browser.Types.Event) -> unit
```

You can use this like so:

```fsharp
RTL.fireEvent(elem, myEvent)
```

## fireEvent.`event`

Convenience methods for firing DOM events.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
/// The event property type is based on the type of event being created.
(element: HTMLElement, ?eventProperties: <EventProperty> list) -> Event
```

You can use this like so:

```fsharp
RTL.fireEvent.change(elem, [ 
    event.target [ 
        prop.value "Hello world" 
    ] 
])

elem.fireEvent.change([
    event.target [ 
        prop.value "Hello world" 
    ]
])
```

## getNodeText

Gets the text of the element.

Signature: 
```fsharp 
(element: HTMLElement) -> string
```

You can use this like so:

```fsharp
RTL.getNodeText(myElem)
```

## getRoles

Allows iteration over the implicit ARIA roles represented 
in a given tree of DOM nodes.

It returns an object, indexed by role name, with each value being an 
array of elements which have that implicit ARIA role.

Signature: 
```fsharp 
(element: HTMLElement) -> obj
```

You can use this like so:

```fsharp
RTL.getRoles(myElem)
```

## isInaccessible

Compute if the given element should be excluded from the accessibility 
API by the browser. 

It implements every MUST criteria from the Excluding Elements from the 
Accessibility Tree section in WAI-ARIA 1.2 with the exception of 
checking the role attribute.

Signature: 
```fsharp 
(element: HTMLElement) -> bool
```

You can use this like so:

```fsharp
RTL.isInaccessible(myElem)
```

## logRoles

Print out a list of all the implicit ARIA roles within a tree of DOM 
nodes, each role containing a list of all of the nodes which match 
that role.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.logRoles(myElem)
```

## prettyDOM

Returns a readable representation of the DOM tree of a node.

Signature: 
```fsharp 
(element: HTMLElement) -> string
(node: Node) -> string
(element: HTMLElement, maxLength: int) -> string
(element: HTMLElement, options: IPrettyDOMOption list) -> string
(element: HTMLElement, maxLength: int, options: IPrettyDOMOption list) -> string

// prettyDOM options
type prettyDOMOption:
    /// Call toJSON method (if it exists) on objects.
    callToJSON (value: bool)

    /// Escape special characters in regular expressions.
    escapeRegex (value: bool)

    /// Escape special characters in strings.
    escapeString (value: bool)

    /// Highlight syntax with colors in terminal (some plugins).
    highlight (value: bool)

    /// Spaces in each level of indentation.
    indent (value: int) 

    /// Levels to print in arrays, objects, elements, .. etc.
    maxDepth (value: int)

    /// Minimize added space: no indentation nor line breaks.
    min (value: bool)

    /// Plugins to serialize application-specific data types.
    plugins (value: seq<string>) 

    /// Include or omit the name of a function.
    printFunctionName (value: bool)

    /// Colors to highlight syntax in terminal.
    theme (properties: IPrettyDOMThemeOption list)

// prettyDOM theme options
// Only accessible from prettyDOMOption module
type theme:
    /// Default: "gray"
    comment (value: string)

    /// Default: "reset"
    content (value: string)

    /// Default: "yellow"
    prop (value: string)

    /// Default: "cyan"
    tag (value: string)

    /// Default: "green"
    value (value: string)
```

You can use this like so:

```fsharp
RTL.prettyDOM(myElem, [
    prettyDOMOption.callToJSON true
    prettyDOMOption.highlight true
    prettyDOMOption.theme [
        prettyDOMOption.theme.comment (color.red)
    ]
])
```

## render

Render into a container which is appended to document.body.

See [render](/rtl/render) for more details.

Signature: 
```fsharp 
(reactElement: ReactElement) -> render
(reactElement: ReactElement, options: IRenderOption list) -> render

// Render options
type renderOption:
    /// By default, React Testing Library will create a div and 
    /// append that div to the document.body and this is where 
    /// your React component will be rendered. If you provide 
    /// your own HTMLElement container via this option, it will 
    /// not be appended to the document.body automatically.
    container (value: HTMLElement) 

    /// If the container is specified, then this defaults to that, 
    /// otherwise this defaults to document.documentElement. This 
    /// is used as the base element for the queries as well as what 
    /// is printed when you use debug().
    baseElement (value: HTMLElement)

    /// If hydrate is set to true, then it will render with 
    /// ReactDOM.hydrate. This may be useful if you are using 
    /// server-side rendering and use ReactDOM.hydrate to mount 
    /// your components.
    hydrate (value: bool)

    /// Pass a React Component as the wrapper option to have it 
    /// rendered around the inner element. This is most useful for 
    /// creating reusable custom render functions for common data 
    /// providers.
    wrapper (value: ReactElement)
```

You can use this like so:

```fsharp
RTL.render(myReactElement)

RTL.render(myReactElement, [
    renderOption.hydrate true
])
```

## screen

Queries bound to the document.body

See [queries](/rtl/queries-for-element) for more details.

You can use this like so:

```fsharp
RTL.screen.getByTestId("my-test-id")
```

## userEvent.clear

Convenience method for using [fireEvent](#fireevent).

Selects the text inside an input or textarea and deletes it.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.clear(myElement)

myElement.userEvent.clear()
```

## userEvent.click

Convenience method for using [fireEvent](#fireevent).

Clicks element, depending on what element is it can have different side effects.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.click(myElement)

myElement.userEvent.click()
```

## userEvent.ctrlClick

Convenience method for using [fireEvent](#fireevent).

Clicks element while holding the control key, depending on what 
element is it can have different side effects.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.ctrlClick(myElement)

myElement.userEvent.ctrlClick()
```

## userEvent.dblClick

Convenience method for using [fireEvent](#fireevent).

Clicks element twice, depending on what element is it can have different side effects.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.dblClick(myElement)

myElement.userEvent.dblClick()
```

## userEvent.selectOptions

Convenience method for using [fireEvent](#fireevent).

Selects the specified option(s) of a `<select>` or a `<select multiple>` element.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement, values: 'T []) -> unit
(element: HTMLElement, values: 'T list) -> unit
(element: HTMLElement, values: ResizeArray<'T>) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.selectOptions(myElement, ["Peach"])

myElement.userEvent.selectOptions(["Peach"])
```

## userEvent.shiftClick

Convenience method for using [fireEvent](#fireevent).

Clicks element while holding the shift key, depending on what 
element is it can have different side effects.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.shiftClick(myElement)

myElement.userEvent.shiftClick()
```

## userEvent.shiftCtrlClick

Convenience method for using [fireEvent](#fireevent).

Clicks element while holding the shift and control keys, depending on what 
element is it can have different side effects.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.shiftCtrlClick(myElement)

myElement.userEvent.shiftCtrlClick()
```

## userEvent.tab

Convenience method for using [fireEvent](#fireevent).

Fires a tab event changing the document.activeElement in the same way the browser does.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(shift: bool, focusTrap: HTMLElement) -> unit
```

You can use this like so:

```fsharp
RTL.userEvent.tab(false, myElement)

myElement.userEvent.tab(false)
```

## userEvent.type'

Convenience method for using [fireEvent](#fireevent).

Writes text inside an `<input>` or a `<textarea>`.

`HTMLElement` is extended to also support these methods.

Signature: 
```fsharp 
(element: HTMLElement, text: string) -> JS.Promise<unit>
(element: HTMLElement, text: string, allAtOnce: bool) -> JS.Promise<unit>
(element: HTMLElement, text: string, delay: int) -> JS.Promise<unit>
(element: HTMLElement, text: string, allAtOnce: bool, delay: int) -> JS.Promise<unit>
```

You can use this like so:

```fsharp
RTL.userEvent.type'(myElement, "oh, hello there!")

myElement.userEvent.type'("oh, hello there!")
```

## waitFor

When in need to wait for any period of time you can use waitFor, to wait for your expectations to pass.

Signature: 
```fsharp 
(callback: unit -> unit) -> JS.Promise<unit>
(callback: unit -> unit, waitForOptions: IWaitOption list) -> JS.Promise<unit>

// waitFor options
type waitForOption:
    /// The default container is the global document. 
    ///
    /// Make sure the elements you wait for are descendants of container.
    container (element: HTMLElement)

    /// The default interval is 50ms. 
    ///
    /// However it will run your callback immediately before starting the intervals.
    interval (value: int)

    /// Sets the configuration of the mutation observer.
    mutationObserver (options: IMutationObserverOption list)

    /// The default timeout is 1000ms.
    timeout (value: int)

// waitFor mutationObserver Options
// Only accessible from waitForOption module
type mutationObserver
    /// An array of specific attribute names to be monitored. 
    ///
    /// If this property isn't included, changes to all attributes cause mutation 
    /// notifications.
    attributeFiler (filter: string)
    attributeFiler (filters: string list)

    /// An array of specific attribute names to be monitored. 
    ///
    /// If this property isn't included, changes to all attributes cause mutation 
    /// notifications.
    attributeOldValue (value: bool)

    /// Set to true to watch for changes to the value of attributes on the node 
    /// or nodes being monitored. 
    ///
    /// The default value is false.
    attributes (value: bool)
    
    /// Set to true to monitor the specified target node or subtree for changes 
    /// to the character data contained within the node or nodes. 
    ///
    /// The default value is `false` via omission.
    characterData (value: bool)
    
    /// Set to true to record the previous value of a node's text whenever the 
    /// text changes on nodes being monitored.
    ///
    /// The default value is `false` via omission.
    characterDataOldValue (value: bool)
    
    /// Set to true to monitor the target node (and, if subtree is true, its 
    /// descendants) for the addition of 
    /// new child nodes or removal of existing child nodes. 
    ///
    /// The default is false.
    childList (value: bool)
    
    /// Set to true to extend monitoring to the entire subtree of nodes rooted 
    /// at target. All of the other MutationObserverInit properties are then 
    /// extended to all of the nodes in the subtree instead of applying 
    /// solely to the target node. 
    ///
    /// The default value is false.
    subtree (value: bool)
```

You can use this like so:

```fsharp
promise {
    ...
    return! RTL.waitFor(fun () -> Jest.expect(myHtmlElement).toHaveFocus())
}
```

## waitForElementToBeRemoved

Wait for the removal of element(s) from the DOM.

See [waitFor](#waitfor) for details on options.

Signature: 
```fsharp 
(callback: unit -> #HTMLElement option) -> JS.Promise<unit>
(callback: unit -> #HTMLElement list) -> JS.Promise<unit>
(callback: unit -> #HTMLElement option, waitForOptions: IWaitOption list) -> JS.Promise<unit>
(callback: unit -> #HTMLElement list, waitForOptions: IWaitOption list) -> JS.Promise<unit>
```

You can use this like so:

```fsharp
promise {
    ...
    return! RTL.waitForElementToBeRemoved(fun () -> RTL.screen.queryByTestId("myElementTestId"))
}
```

## within

Takes a DOM element and binds it to the raw query functions, 
allowing them to be used without specifying a container. 

See [queries](/rtl/queries-for-element) for more details.

Signature: 
```fsharp 
(element: HTMLElement) -> queriesForElement
```

You can use this like so:

```fsharp
RTL.within(myElement)
```
