namespace Fable.SignalR.DotNet.Tests

[<AutoOpen>]
module Util =
    [<RequireQualifiedAccess>]
    module Async =
        let lift x = async { return x }