namespace Fable.SignalR.DotNet.Tests

[<AutoOpen>]
module Config =
    open Expecto

    let config =
        { FsCheckConfig.defaultConfig with maxTest = 100 }
    
    let inline testProp name f = testPropertyWithConfig config name f
