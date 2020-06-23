namespace Fable.SignalR

open Fable.Core
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module Parser =
    type RArray<'T> = System.Collections.Generic.List<'T>

    open Fable.SimpleJson
    open Messages

    [<RequireQualifiedAccess>]
    module HubRecords =
        type InvocationMessage<'T> = 
            { ``type``: MessageType
              target: string
              headers: Map<string,string> option
              invocationId: string option
              arguments: RArray<'T option> 
              streamIds: RArray<string> option }

            static member inline Validate<'T> (msg: InvocationMessage<'T>) =
                if msg.target = "" then
                    failwith "Invalid payload for Invocation message."
                if msg.invocationId.IsSome then
                    if msg.invocationId.Value = "" then
                        failwith "Invalid payload for Invocation message."
                msg

        type StreamInvocationMessage<'T> =
            { ``type``: MessageType
              invocationId: string
              target: string
              arguments: RArray<'T option> 
              streamIds: RArray<string> option }

        type StreamItemMessage<'T> =
            { ``type``: MessageType
              invocationId: string
              item: 'T option }

            static member inline Validate<'T> (msg: StreamItemMessage<'T>) =
                if msg.invocationId = "" || msg.item.IsNone then
                    failwith "Invalid payload for StreamItem message."
                msg

        type CompletionMessage<'T> =
            { ``type``: MessageType
              invocationId: string
              error: string option
              result: 'T option }

            static member inline Validate<'T> (msg: CompletionMessage<'T>) =
                let fail () = failwith "Invalid payload for Completion message."

                match msg.result, msg.error with
                | Some _, Some _ -> fail()
                | None, Some err -> if err = "" then fail()
                | _ -> if msg.invocationId = "" then fail()

                msg

        type PingMessage =
            { ``type``: MessageType }

        type CloseMessage =
            { ``type``: MessageType
              error: string option
              allowReconnect: bool option }

        type CancelInvocationMessage =
            { ``type``: MessageType
              invocationId: string }

    [<RequireQualifiedAccess>]
    module TextMessageFormat =
        let recordSeparatorChar = char 0x1e
        let recordSeparator = unbox<string> recordSeparatorChar

        let write (output: string) =
            sprintf "%s%s" output recordSeparator

        let parse (input: string) =
            if input.EndsWith(recordSeparator) then
                let arr = input.Split(recordSeparatorChar)
                arr.[0..(arr.Length-2)]
            else failwith "Message is incomplete."

    type JsonProtocol () =
        member _.name = "json"
        member _.version = 1.0
        member _.transferFormat = TransferFormat.Text

        member _.writeMessage (message: HubMessage<'ClientStreamApi,'ServerApi,'ServerStreamApi>) =
            TextMessageFormat.write(Json.stringify message)
            |> U2.Case1

        #if FABLE_COMPILER
        member inline this.parseMessages<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #else
        member this.parseMessages<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #endif
            this.parseMsgs<'ClientStreamApi,'ServerApi,'ServerStreamApi>(input, ?logger = logger)

        member inline _.processMsg<'ClientStreamApi,'ServerApi,'ServerStreamApi> (parsedRaw: Json, msgType: MessageType) =
            match msgType with
            | MessageType.Invocation ->
                Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<'ServerApi>> parsedRaw
                |> Result.map (HubRecords.InvocationMessage.Validate >> unbox<InvocationMessage<'ServerApi>> >> U7.Case1)
            | MessageType.StreamItem -> 
                Json.tryConvertFromJsonAs<HubRecords.StreamItemMessage<'ServerStreamApi>> parsedRaw
                |> Result.map (HubRecords.StreamItemMessage.Validate >> unbox<StreamItemMessage<'ServerStreamApi>> >> U7.Case2)
            | MessageType.Completion -> 
                Json.tryConvertFromJsonAs<HubRecords.CompletionMessage<'ServerApi>> parsedRaw
                |> Result.map (HubRecords.CompletionMessage.Validate >> unbox<CompletionMessage<'ServerApi>> >> U7.Case3)
            | MessageType.StreamInvocation -> 
                Json.tryConvertFromJsonAs<HubRecords.StreamInvocationMessage<'ClientStreamApi>> parsedRaw
                |> Result.map (unbox<StreamInvocationMessage<'ClientStreamApi>> >> U7.Case4)
            | MessageType.CancelInvocation -> 
                Json.tryConvertFromJsonAs<HubRecords.CancelInvocationMessage> parsedRaw
                |> Result.map (unbox<CancelInvocationMessage> >> U7.Case5)
            | MessageType.Ping -> 
                Json.tryConvertFromJsonAs<HubRecords.PingMessage> parsedRaw
                |> Result.map (unbox<PingMessage> >> U7.Case6)
            | _ -> 
                Json.tryConvertFromJsonAs<HubRecords.CloseMessage> parsedRaw
                |> Result.map (unbox<CloseMessage> >> U7.Case7)

        #if FABLE_COMPILER
        member inline this.parseMsgs<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #else
        member this.parseMsgs<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #endif
            let logger =
                match logger with
                | None -> Bindings.signalR.NullLogger() :> ILogger
                | Some logger -> logger

            match input with
            | U3.Case1 "" -> [||]
            | U3.Case1 str ->
                TextMessageFormat.parse(str)
                |> Array.choose(fun m ->
                    let parsedRaw = SimpleJson.parse m

                    SimpleJson.readPath ["type"] parsedRaw
                    |> Option.map Json.convertFromJsonAs<MessageType>
                    |> Option.get
                    |> fun msgType -> this.processMsg<'ClientStreamApi,'ServerApi,'ServerStreamApi>(parsedRaw, msgType)
                    |> function
                    | Ok msg -> Some msg
                    | Error e -> 
                        sprintf "Unknown message type: %s" e
                        |> logger.log LogLevel.Information
                        None)
            | _ -> failwith "Invalid input for JSON hub protocol. Expected a string."
            |> RArray
