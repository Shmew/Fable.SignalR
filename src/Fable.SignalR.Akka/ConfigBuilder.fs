namespace Fable.SignalR.Akka

open Microsoft.Extensions.Logging

module ConfigBuilder =
    type AkkaConfig =
        { Extensions: string list
          Loggers: string list
          Provider: string
          DownUnreachable: int<s>
          Roles: string list
          SeedNodes: string list
          Hostname: string
          PublicHostname: string
          Port: uint16
          LogLevelPredicate: LogLevel -> bool }

    let create (config: AkkaConfig) =
        let logLevelList = [ 0 .. 6 ] |> List.map enum<LogLevel>

        let logLevel =
            logLevelList
            |> List.tryFind config.LogLevelPredicate
            |> function
            | Some LogLevel.None
            | None -> Hocon.LogLevel.OFF
            | Some logLevel ->
                match logLevel with
                | LogLevel.Trace 
                | LogLevel.Debug -> Hocon.LogLevel.DEBUG
                | LogLevel.Information -> Hocon.LogLevel.INFO
                | LogLevel.Warning -> Hocon.LogLevel.WARNING
                | LogLevel.Error 
                | LogLevel.Critical -> Hocon.LogLevel.ERROR
                | _ -> Hocon.LogLevel.OFF

        akka {
            extensions [ "Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider, Akka.Cluster.Tools" ]

            log_config_on_start true
            loggers config.Loggers
            stdout_loglevel logLevel
            loglevel logLevel

            actor {
                provider config.Provider
                
                if logLevel = Hocon.LogLevel.DEBUG then
                    debug {
                        receive true
                        autoreceive true
                        lifecycle true
                        event_stream true
                        unhandled true
                    }
            }

            remote.dot_netty.tcp {
                hostname config.Hostname
                public_hostname config.PublicHostname
                port config.Port
            }

            cluster {
                auto_down_unreachable_after config.DownUnreachable
                roles config.Roles

                seed_nodes config.SeedNodes

                pub_sub {
                    routing_logic Hocon.Router.Logic.Broadcast
                }
            }
        }
