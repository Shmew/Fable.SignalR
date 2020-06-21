# render

The `render` type returned after calling `RTL.render` is one of
the most used aspects of react-testing-library. The main reason
for this being that the `ReactElement` has now been rendered, and
what you have now is a type that inherits [queriesForElement] as
well as exposes a few additional functions and properties that 
will make testing your React application easier.

## Render Options
When creating your render object you can pass in options to
modify the behavior:

```fsharp
RTL.render(myReactElement, [
    renderOption.hydrate true
])
```

### container

By default, React Testing Library will create a div and append that 
div to the document.body and this is where your React component will 
be rendered. If you provide your own HTMLElement container via this 
option, it will not be appended to the document.body automatically.

Signature:
```fsharp
(value: HTMLElement)
```

### baseElement

By default, React Testing Library will create a div and append that 
div to the document.body and this is where your React component will 
be rendered. If you provide your own HTMLElement container via this 
option, it will not be appended to the document.body automatically.

Signature:
```fsharp
(value: HTMLElement)
```

### hydrate

By default, React Testing Library will create a div and append that 
div to the document.body and this is where your React component will 
be rendered. If you provide your own HTMLElement container via this 
option, it will not be appended to the document.body automatically.

Signature:
```fsharp
(value: bool)
```

### wrapper

By default, React Testing Library will create a div and append that 
div to the document.body and this is where your React component will 
be rendered. If you provide your own HTMLElement container via this 
option, it will not be appended to the document.body automatically.

Signature:
```fsharp
(value: ReactElement)
```

## baseElement

The containing DOM node where your React Element is rendered in 
the container. If you don't specify the baseElement in the options 
of render, it will default to document.body.

This is useful when the component you want to test renders something 
outside the container div, e.g. when you want to snapshot test your 
portal component which renders its HTML directly in the body.

Signature:
```fsharp
property: HTMLElement
```

Usage:
```fsharp
renderer.baseElement
```

## container

The containing DOM node of your rendered React Element (rendered 
using ReactDOM.render). It's a div. This is a regular DOM node, 
so you can call container.querySelector etc. to inspect the children.

Signature:
```fsharp
property: HTMLElement
```

Usage:
```fsharp
renderer.container
```

## asFragment

Returns a DocumentFragment of your rendered component. This can be 
useful if you need to avoid live bindings and see how your component 
reacts to events.

Signature:
```fsharp
unit -> DocumentFragment
```

Usage:
```fsharp
renderer.asFragment()
```

## debug

This method is a shortcut for console.log(prettyDOM(baseElement))

Signature:
```fsharp
unit -> unit
```

Usage:
```fsharp
renderer.debug()
```

## rerender

It'd probably be better if you test the component that's doing the 
prop updating to ensure that the props are being updated correctly 
(see the Guiding Principles section). That said, if you'd prefer to 
update the props of a rendered component in your test, this function 
can be used to update props of the rendered component.

Signature:
```fsharp
(reactElement: ReactElement) -> unit
```

Usage:
```fsharp
renderer.rerender(myReactElement)
```

## unmount

This will cause the rendered component to be unmounted. This is useful 
for testing what happens when your component is removed from the page 
(like testing that you don't leave event handlers hanging around 
causing memory leaks).

Signature:
```fsharp
unit -> unit
```

Usage:
```fsharp
renderer.unmount()
```

[queriesForElement]: /rtl/queries-for-element
