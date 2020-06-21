# Fable.Jester

Fable.Jester are bindings to use [jest] and [jest-dom] to
test Fable applications.

The library has been developed to follow the native API as 
closely as possible, while making some changes to allow for 
better discoverablity.

Fable.Jester as a whole is exposed as two main types: `Jest`
and `expect`.

As long as you know of those two items you can find anything 
that is available in this library. See those linked sections 
for more details.
 
## Jest

The `Jest` type exposes almost every piece of functionality 
in the library:

 * [Describe blocks](/jest/describe)
 * [Test blocks](/jest/test)
 * [Global functions](/jest/globals)
 * [Expect](/jest/expect)

## Expect

The [expect](/jest/expectHelpers) type is not the actual
method of making assertions, which can be confusing. The 
purpose of this type is a collection of helper methods to 
aid you when *actually* making your assertions.

[jest]: https://www.npmjs.com/package/jest
[jest-dom]: https://www.npmjs.com/package/@testing-library/jest-dom