namespace Fable.SignalR.DotNet.Tests

[<AutoOpen>]
module TestProperty =
    open Expecto
    open FsCheck
    open System

    let private propertyTest methodName name (property: Property) =
        let runner (config: FsCheckConfig) =
            { new IRunner with
                member __.OnStartFixture _ = ()

                member __.OnArguments(testNumber, args, formatOnEvery) =
                    config.receivedArgs config name testNumber args
                    |> Async.RunSynchronously

                member __.OnShrink(values, formatValues) =
                    config.successfulShrink config name values
                    |> Async.RunSynchronously

                member __.OnFinished(fsCheckTestName, testResult) =
                    config.finishedTest config fsCheckTestName
                    |> Async.RunSynchronously

                    let numTests i =
                        if i = 1 then "1 test" else sprintf "%i tests" i

                    let stampsToString s =
                        let entry (p, xs) =
                            sprintf "%A%s %s" p "%" (String.concat ", " xs)

                        match Seq.map entry s |> Seq.toList with
                        | [] -> ""
                        | [ x ] -> sprintf " (%s)\n" x
                        | xs -> sprintf "%s\n" (String.concat "\n" xs)

                    match testResult with
                    | TestResult.True (_testData, _b) -> ()
                    | TestResult.False (_, _, _, Outcome.Exception (:? IgnoreException as e), _) -> raise e
                    | TestResult.False (data, original, shrunk, outcome, Random.StdGen (std, gen)) ->
                        let parameters =
                            original
                            |> List.map (sprintf "%A")
                            |> String.concat " "
                            |> sprintf "Parameters:\n\t%s"

                        let shrunk =
                            if data.NumberOfShrinks > 0 then
                                shrunk
                                |> List.map (sprintf "%A")
                                |> String.concat " "
                                |> sprintf "\nShrunk %i times to:\n\t%s" data.NumberOfShrinks
                            else
                                ""

                        let labels =
                            match data.Labels.Count with
                            | 0 -> String.Empty
                            | 1 -> sprintf "Label of failing property: %s\n" (Set.toSeq data.Labels |> Seq.head)
                            | _ ->
                                sprintf "Labels of failing property (one or more is failing): %s\n"
                                    (String.concat " " data.Labels)

                        let focus =
                            sprintf "Focus on error:\n\t%s (%i, %i) \"%s\"" methodName std gen name

                        sprintf "Failed after %s. %s%s\nResult:\n\t%A\n%s%s%s" (numTests data.NumberOfTests) parameters
                            shrunk outcome labels (stampsToString data.Stamps) focus
                        |> FailedException
                        |> raise

                    | TestResult.Exhausted data ->
                        sprintf "Exhausted after %s%s" (numTests data.NumberOfTests) (stampsToString data.Stamps)
                        |> FailedException
                        |> raise }

        let fsConfig = { FsCheckConfig.defaultConfig with maxTest = 100 }

        let config =
            { MaxTest = 100
              MaxFail = 1000
              Replay = Option.map Random.StdGen fsConfig.replay
              Name = name
              StartSize = fsConfig.startSize
              EndSize = fsConfig.endSize
              QuietOnSuccess = true
              Every = fun _ _ -> String.Empty
              EveryShrink = fun _ -> String.Empty
              Arbitrary = fsConfig.arbitrary
              Runner = runner fsConfig }

        Check.One(config, property) |> async.Return
    
    let checkProp name = propertyTest "etestProperty" name

    let testPropertyP name property = 
        propertyTest "etestProperty" name property
        |> fun test -> TestLabel(name, TestCase(Async test, Normal), Normal)