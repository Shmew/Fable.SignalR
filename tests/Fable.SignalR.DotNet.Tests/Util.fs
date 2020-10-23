namespace Fable.SignalR.DotNet.Tests

[<AutoOpen>]
module Util =
    open Expecto

    [<RequireQualifiedAccess>]
    module Async =
        let lift x = async { return x }