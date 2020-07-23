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
                |> function
                | Ok res -> Ok (HubRecords.InvocationMessage.Validate res |> unbox<InvocationMessage<'ServerApi>> |> U8.Case1)
                | Error _ ->
                    Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<{| connectionId: string; invocationId: System.Guid; message: 'ServerApi |}>> parsedRaw
                    |> Result.map (HubRecords.InvocationMessage.Validate 
                                   >> unbox<InvocationMessage<{| connectionId: string; invocationId: System.Guid; message: 'ServerApi |}>> >> U8.Case2)
            | MessageType.StreamItem -> 
                Json.tryConvertFromJsonAs<HubRecords.StreamItemMessage<'ServerStreamApi>> parsedRaw
                |> Result.map (HubRecords.StreamItemMessage.Validate >> unbox<StreamItemMessage<'ServerStreamApi>> >> U8.Case3)
            | MessageType.Completion -> 
                Json.tryConvertFromJsonAs<HubRecords.CompletionMessage<'ServerApi>> parsedRaw
                |> Result.map (HubRecords.CompletionMessage.Validate >> unbox<CompletionMessage<'ServerApi>> >> U8.Case4)
            | MessageType.StreamInvocation -> 
                Json.tryConvertFromJsonAs<HubRecords.StreamInvocationMessage<'ClientStreamApi>> parsedRaw
                |> Result.map (unbox<StreamInvocationMessage<'ClientStreamApi>> >> U8.Case5)
            | MessageType.CancelInvocation -> 
                Json.tryConvertFromJsonAs<HubRecords.CancelInvocationMessage> parsedRaw
                |> Result.map (unbox<CancelInvocationMessage> >> U8.Case6)
            | MessageType.Ping -> 
                Json.tryConvertFromJsonAs<HubRecords.PingMessage> parsedRaw
                |> Result.map (unbox<PingMessage> >> U8.Case7)
            | _ -> 
                Json.tryConvertFromJsonAs<HubRecords.CloseMessage> parsedRaw
                |> Result.map (unbox<CloseMessage> >> U8.Case8)

        #if FABLE_COMPILER
        member inline this.parseMsgs<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #else
        member this.parseMsgs<'ClientStreamApi,'ServerApi,'ServerStreamApi> (input: U3<string,JS.ArrayBuffer,System.Buffer>, ?logger: ILogger) =
        #endif
            let logError e =
                #if DEBUG
                match logger with
                | None -> JS.console.error(e)
                | Some logger -> logger.log LogLevel.Error e
                #endif
                match logger with
                | None -> ()
                | Some logger -> logger.log LogLevel.Error e

            match input with
            | U3.Case1 "" -> [||]
            | U3.Case1 str ->
                TextMessageFormat.parse(str)
                |> Array.choose(fun m ->
                    let parsedRaw = SimpleJson.parse m
                    
                    SimpleJson.readPath ["type"] parsedRaw
                    |> Option.map Json.convertFromJsonAs<MessageType>
                    |> Option.get
                    |> fun msgType ->
                        this.processMsg<'ClientStreamApi,'ServerApi,'ServerStreamApi>(parsedRaw, msgType)
                    |> function
                    | Ok msg -> Some msg
                    | Error e -> 
                        sprintf "Unknown message type: %s" e
                        |> logError
                        None)
            | _ -> failwith "Invalid input for JSON hub protocol. Expected a string."
            |> RArray
