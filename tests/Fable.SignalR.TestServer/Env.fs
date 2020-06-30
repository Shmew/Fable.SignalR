namespace SignalRApp

module Env =
    open System

    let getEnvFromAllOrNone (s: string) =
        let envOpt (envVar: string) =
            if envVar = "" || isNull envVar then None
            else Some(envVar)

        let procVar = Environment.GetEnvironmentVariable(s) |> envOpt
        let userVar = Environment.GetEnvironmentVariable(s, EnvironmentVariableTarget.User) |> envOpt
        let machVar = Environment.GetEnvironmentVariable(s, EnvironmentVariableTarget.Machine) |> envOpt

        match procVar, userVar, machVar with
        | Some(v), _, _
        | _, Some(v), _
        | _, _, Some(v) -> Some(v)
        | _ -> None

    let getPortsOrDefault defaultVal =
        match getEnvFromAllOrNone "GIRAFFE_FABLE_PORT" with
        | Some value -> value |> uint16
        | None -> defaultVal
