namespace Fable.SignalR

open Fable.Core
open Messages
open System.ComponentModel

[<EditorBrowsable(EditorBrowsableState.Never)>]
module Protocol =
    open Fable.SignalR.Shared

    [<RequireQualifiedAccess>]
    module HubRecords =
        type InvocationMessage<'T> = 
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option
              target: string
              arguments: ResizeArray<'T> 
              streamIds: ResizeArray<string> option }

        [<Erase;RequireQualifiedAccess>]
        module InvocationMessage =
            let inline validate<'T> (msg: InvocationMessage<'T>) =
                if msg.target = "" then
                    failwith "Invalid payload for Invocation message."
                if msg.invocationId.IsSome then
                    if msg.invocationId.Value = "" then
                        failwith "Invalid payload for Invocation message."
                msg

            let inline create<'T> headers invocationId target (args: ResizeArray<'T>) streamIds =
                { ``type`` = MessageType.Invocation
                  headers = headers
                  invocationId = invocationId
                  target = target
                  arguments = args
                  streamIds = streamIds }
                |> validate
                |> unbox<Fable.SignalR.Messages.InvocationMessage<'T>>

        type StreamInvocationMessage<'T> =
            { ``type``: MessageType
              target: string
              headers: Map<string,string> option
              invocationId: string
              arguments: ResizeArray<'T> 
              streamIds: ResizeArray<string> option }

        [<Erase;RequireQualifiedAccess>]
        module StreamInvocationMessage =
            let inline create<'T> headers invocationId target (args: ResizeArray<'T>) streamIds =
                { ``type`` = MessageType.StreamInvocation
                  headers = headers
                  invocationId = invocationId
                  target = target
                  arguments = args
                  streamIds = streamIds }
                |> unbox<Fable.SignalR.Messages.StreamInvocationMessage<'T>>

        type StreamItemMessage<'T> =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option
              item: 'T }
              
        [<Erase;RequireQualifiedAccess>]
        module StreamItemMessage =
            let inline validate<'T> (msg: StreamItemMessage<'T>) =
                match msg.invocationId with
                | Some invocationId when invocationId <> "" -> msg
                | _ -> failwith "Invalid payload for StreamItem message."

            let inline create<'T> headers invocationId (item: 'T) =
                { ``type`` = MessageType.StreamItem
                  headers = headers
                  invocationId = invocationId
                  item = item } 
                |> validate
                |> unbox<Fable.SignalR.Messages.StreamItemMessage<'T>>

        type CompletionMessage<'T> =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string
              error: string option
              result: 'T option }
              
        [<Erase;RequireQualifiedAccess>]
        module CompletionMessage =
            let inline validate<'T> (msg: CompletionMessage<'T>) =
                let fail () = failwith "Invalid payload for Completion message."

                match msg.result, msg.error with
                | Some _, Some _ -> fail()
                | None, Some err -> if err = "" then fail()
                | _ -> if msg.invocationId = "" then fail()

                msg

            let inline create<'T> headers invocationId error (result: 'T option) =
                { ``type`` = MessageType.Completion
                  headers = headers
                  invocationId = invocationId
                  error = error
                  result = result } 
                |> validate
                |> unbox<Fable.SignalR.Messages.CompletionMessage<'T>>

        type PingMessage =
            { ``type``: MessageType }
            
        [<Erase;RequireQualifiedAccess>]
        module PingMessage =
            let inline create () = 
                { ``type`` = MessageType.Ping }
                |> unbox<Fable.SignalR.Messages.PingMessage>

        type CloseMessage =
            { ``type``: MessageType
              error: string option
              allowReconnect: bool option }

        [<Erase;RequireQualifiedAccess>]
        module CloseMessage =
            let inline create error allowReconnect =
                { ``type`` = MessageType.Close
                  error = error
                  allowReconnect = allowReconnect } 
                |> unbox<Fable.SignalR.Messages.CloseMessage>

        type CancelInvocationMessage =
            { ``type``: MessageType
              headers: Map<string,string> option
              invocationId: string option }
              
        [<Erase;RequireQualifiedAccess>]
        module CancelInvocationMessage =
            let inline create headers invocationId =
                { ``type`` = MessageType.CancelInvocation
                  headers = headers
                  invocationId = invocationId } 
                |> unbox<Fable.SignalR.Messages.CancelInvocationMessage>

    [<RequireQualifiedAccess>]
    module Json =
        open Fable.SimpleJson

        type RArray<'T> = System.Collections.Generic.List<'T>

        [<RequireQualifiedAccess>]
        module TextMessageFormat =
            let private recordSeparatorChar = char 0x1e
            let private recordSeparator = unbox<string> recordSeparatorChar

            let write (output: string) =
                sprintf "%s%s" output recordSeparator

            let parse (input: string) =
                if input.EndsWith(recordSeparator) then
                    let arr = input.Split(recordSeparatorChar)
                    arr.[0..(arr.Length-2)]
                else failwith "Message is incomplete."

        type JsonProtocol () =
            member inline _.writeMessage (message: HubMessage<'ClientStreamFromApi,'ClientApi,'ClientApi,'ClientStreamToApi>) =
                TextMessageFormat.write(Json.stringify message)
                |> U2.Case1

            member inline _.processMsg<'ServerApi,'ServerStreamApi> (parsedRaw: Json, msgType: MessageType) =
                match msgType with
                | MessageType.Invocation ->
                    Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<'ServerApi>> parsedRaw
                    |> function
                    | Ok res -> Ok (HubRecords.InvocationMessage.validate res |> unbox<InvocationMessage<'ServerApi>> |> U8.Case1)
                    | Error _ ->
                        Json.tryConvertFromJsonAs<HubRecords.InvocationMessage<InvokeArg<'ServerApi>>> parsedRaw
                        |> Result.map (HubRecords.InvocationMessage.validate 
                                        >> unbox<InvocationMessage<InvokeArg<'ServerApi>>> >> U8.Case2)
                | MessageType.StreamItem -> 
                    Json.tryConvertFromJsonAs<HubRecords.StreamItemMessage<'ServerStreamApi>> parsedRaw
                    |> Result.map (HubRecords.StreamItemMessage.validate >> unbox<StreamItemMessage<'ServerStreamApi>> >> U8.Case3)
                | MessageType.Completion -> 
                    Json.tryConvertFromJsonAs<HubRecords.CompletionMessage<'ServerApi>> parsedRaw
                    |> Result.map (HubRecords.CompletionMessage.validate >> unbox<CompletionMessage<'ServerApi>> >> U8.Case4)
                // This case never happens
                | MessageType.StreamInvocation -> 
                    Json.tryConvertFromJsonAs<HubRecords.StreamInvocationMessage<unit>> parsedRaw
                    |> Result.map (unbox<StreamInvocationMessage<unit>> >> U8.Case5)
                | MessageType.CancelInvocation -> 
                    Json.tryConvertFromJsonAs<HubRecords.CancelInvocationMessage> parsedRaw
                    |> Result.map (unbox<CancelInvocationMessage> >> U8.Case6)
                | MessageType.Ping -> 
                    Json.tryConvertFromJsonAs<HubRecords.PingMessage> parsedRaw
                    |> Result.map (unbox<PingMessage> >> U8.Case7)
                | MessageType.Close -> 
                    Json.tryConvertFromJsonAs<HubRecords.CloseMessage> parsedRaw
                    |> Result.map (unbox<CloseMessage> >> U8.Case8)
                // Shouldn't ever match
                | _ -> failwithf "Invalid message: %A" parsedRaw

            #if FABLE_COMPILER
            member inline this.parseMsgs<'ServerApi,'ServerStreamApi> (input: U2<string,JS.ArrayBuffer>) (logger: ILogger) =
            #else
            member this.parseMsgs<'ServerApi,'ServerStreamApi> (input: U2<string,JS.ArrayBuffer>) (logger: ILogger) =
            #endif
                match input with
                | U2.Case1 "" -> [||]
                | U2.Case1 str ->
                    try
                        TextMessageFormat.parse(str)
                        |> Array.choose(fun m ->
                            let parsedRaw = SimpleJson.parse m
                        
                            SimpleJson.readPath ["type"] parsedRaw
                            |> Option.map Json.convertFromJsonAs<MessageType>
                            |> Option.get
                            |> fun msgType ->
                                this.processMsg<'ServerApi,'ServerStreamApi>(parsedRaw, msgType)
                            |> function
                            | Ok msg -> Some msg
                            | Error e -> 
                                sprintf "Unknown message type: %s" e
                                |> logger.log LogLevel.Error
                                None)
                    with e ->
                        sprintf "An error occured during message deserialization: %s" e.Message
                        |> logger.log LogLevel.Error
                        [||]
                | _ -> 
                    logger.log LogLevel.Error "Invalid input for JSON hub protocol. Expected a string, got an array buffer instead."
                    [||]
                |> RArray

        let inline create<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
            let protocol = JsonProtocol()
            { new IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> with
                member _.name = "json"
                member _.version = 1
                member _.transferFormat = TransferFormat.Text

                member _.parseMessages (input, logger) = protocol.parseMsgs input logger
                member _.writeMessage msg = protocol.writeMessage msg }

    [<RequireQualifiedAccess>]
    module MsgPack =
        open Fable.Remoting.MsgPack
        open Fable.SignalR.Shared.MsgPack

        [<Erase>]
        module StreamIds =
            let inline toOpt (streamIds: string []) =
                if Array.isEmpty streamIds then None
                else Some streamIds
        
        [<Erase>]
        module String =
            let inline toOpt (s: string) =
                if isNull s || s = "" then None
                else Some s

        [<Erase>]
        module Obj =
            let inline toOpt (o: obj) =
                if isNull o then None
                else Some o

        module Write =
            open FSharp.Reflection
            open System
            open System.Collections.Generic
            open System.Text

            let serializerCache = Dictionary<Type, obj -> ResizeArray<byte> -> unit> ()
            
            let cacheGetOrAdd (typ, f) =
                match serializerCache.TryGetValue typ with
                | true, f -> f
                | _ ->
                    serializerCache.Add (typ, f)
                    f
            
            let inline write32bitNumber b1 b2 b3 b4 (out: ResizeArray<byte>) writeFormat =
                if b2 > 0uy || b1 > 0uy then
                    if writeFormat then out.Add Format.Uint32
                    out.Add b1
                    out.Add b2
                    out.Add b3
                    out.Add b4
                elif (b3 > 0uy) then
                    if writeFormat then out.Add Format.Uint16
                    out.Add b3
                    out.Add b4
                else
                    if writeFormat then out.Add Format.Uint8
                    out.Add b4
            
            let write64bitNumber b1 b2 b3 b4 b5 b6 b7 b8 (out: ResizeArray<byte>) =
                if b4 > 0uy || b3 > 0uy || b2 > 0uy || b1 > 0uy then
                    out.Add Format.Uint64
                    out.Add b1
                    out.Add b2
                    out.Add b3
                    out.Add b4
                    write32bitNumber b5 b6 b7 b8 out false
                else
                    write32bitNumber b5 b6 b7 b8 out true
            
            let inline writeUnsigned32bitNumber (n: UInt32) (out: ResizeArray<byte>) =
                write32bitNumber (n >>> 24 |> byte) (n >>> 16 |> byte) (n >>> 8 |> byte) (byte n) out
            
            let inline writeUnsigned64bitNumber (n: UInt64) (out: ResizeArray<byte>) =
                write64bitNumber (n >>> 56 |> byte) (n >>> 48 |> byte) (n >>> 40 |> byte) (n >>> 32 |> byte) (n >>> 24 |> byte) (n >>> 16 |> byte) (n >>> 8 |> byte) (byte n) out
             
            let inline writeNil (out: ResizeArray<byte>) = out.Add Format.Nil
            let inline writeBool x (out: ResizeArray<byte>) = out.Add (if x then Format.True else Format.False)
            
            let writeSignedNumber bytes (out: ResizeArray<byte>) =
                if BitConverter.IsLittleEndian then
                    Array.rev bytes |> out.AddRange
                else
                    out.AddRange bytes
            
            let writeUInt64 (n: UInt64) (out: ResizeArray<byte>) =
                if n < 128UL then
                    out.Add (Format.fixposnum n)
                else
                    writeUnsigned64bitNumber n out
            
            let writeInt64 (n: int64) (out: ResizeArray<byte>) =
                if n >= 0L then
                    writeUInt64 (uint64 n) out 
                else
                    if n > -32L then
                        out.Add (Format.fixnegnum n)
                    else
                        out.Add Format.Int64
                        writeSignedNumber (BitConverter.GetBytes n) out
            
            let inline writeByte b (out: ResizeArray<byte>) =
                out.Add b
            
            let inline writeString (str: string) (out: ResizeArray<byte>) =
                let str = Encoding.UTF8.GetBytes str
            
                if str.Length < 32 then
                    out.Add (Format.fixstr str.Length)
                else
                    if str.Length < 256 then
                        out.Add Format.Str8
                    elif str.Length < 65536 then
                        out.Add Format.Str16
                    else
                        out.Add Format.Str32
            
                    writeUnsigned32bitNumber (uint32 str.Length) out false
            
                out.AddRange str
            
            let writeSingle (n: float32) (out: ResizeArray<byte>) =
                out.Add Format.Float32
                writeSignedNumber (BitConverter.GetBytes n) out
                
            let writeDouble (n: float) (out: ResizeArray<byte>) =
                out.Add Format.Float64
                writeSignedNumber (BitConverter.GetBytes n) out
            
            let writeBin (data: byte[]) (out: ResizeArray<byte>) =
                if data.Length < 256 then
                    out.Add Format.Bin8
                elif data.Length < 65536 then
                    out.Add Format.Bin16
                else
                    out.Add Format.Bin32
            
                writeUnsigned32bitNumber (uint32 data.Length) out false
            
                out.AddRange data
            
            let inline writeDateTimeOffset (out: ResizeArray<byte>) (dto: DateTimeOffset) =
                out.Add (Format.fixarr 2uy)
                writeInt64 dto.Ticks out
                writeInt64 (int64 dto.Offset.TotalMinutes) out
            
            let writeArrayHeader len (out: ResizeArray<byte>) =
                if len < 16 then
                    out.Add (Format.fixarr len)
                elif len < 65536 then
                    out.Add Format.Array16
                    out.Add (len >>> 8 |> FSharp.Core.Operators.byte)
                    out.Add (FSharp.Core.Operators.byte len)
                else
                    out.Add Format.Array32
                    writeUnsigned32bitNumber (uint32 len) out false
            
            let rec writeArray (out: ResizeArray<byte>) t (arr: System.Collections.ICollection) =
                writeArrayHeader arr.Count out
            
                for x in arr do
                    writeObject x t out
            
            and writeMap (out: ResizeArray<byte>) keyType valueType (dict: IDictionary<obj, obj>) =
                let length = dict.Count
            
                if length < 16 then
                    out.Add (Format.fixmap length)
                elif length < 65536 then
                    out.Add Format.Map16
                    out.Add (length >>> 8 |> FSharp.Core.Operators.byte)
                    out.Add (FSharp.Core.Operators.byte length)
                else
                    out.Add Format.Map32
                    writeUnsigned32bitNumber (uint32 length) out false
            
                for kvp in dict do
                    writeObject kvp.Key keyType out
                    writeObject kvp.Value valueType out
            
            and inline writeRecord (out: ResizeArray<byte>) (types: Type[]) (vals: obj[]) =
                writeArrayHeader vals.Length out
            
                for i in 0 .. vals.Length - 1 do
                    writeObject vals.[i] types.[i] out
            
            and inline writeTuple (out: ResizeArray<byte>) (types: Type[]) (vals: obj[]) =
                writeRecord out types vals
            
            and writeUnion (out: ResizeArray<byte>) tag (types: Type[]) (vals: obj[]) =
                out.Add (Format.fixarr 2uy)
                out.Add (Format.fixposnum tag)
            
                if vals.Length <> 1 then
                    writeArrayHeader vals.Length out
            
                    for i in 0 .. vals.Length - 1 do
                        writeObject vals.[i] types.[i] out
                else
                    writeObject vals.[0] types.[0] out

            and writeObject (x: obj) (t: Type) (out: ResizeArray<byte>) =
                if isNull x then writeNil out else
            
                match serializerCache.TryGetValue t with
                | true, writer ->
                    writer x out
                | _ ->
                    if FSharpType.IsRecord (t, true) then
                        let fieldTypes = FSharpType.GetRecordFields (t, true) |> Array.map (fun x -> x.PropertyType)
                        cacheGetOrAdd (t, fun x out -> writeRecord out fieldTypes (FSharpValue.GetRecordFields (x, true))) x out
                    elif t.IsArray then
                        let elementType = t.GetElementType ()
                        cacheGetOrAdd (t, fun x out -> writeArray out elementType (x :?> System.Collections.ICollection)) x out
                    elif t.IsGenericType && t.GetGenericTypeDefinition () = typedefof<_ list> then
                        let elementType = t.GetGenericArguments () |> Array.head
                        cacheGetOrAdd (t, fun x out -> writeArray out elementType (x :?> System.Collections.ICollection)) x out
                    elif t.IsGenericType && t.GetGenericTypeDefinition () = typedefof<_ option> then
                        let elementType = t.GetGenericArguments ()
                        cacheGetOrAdd (t, fun x out ->
                            let opt = x :?> _ option
                            let tag, value = if Option.isSome opt then 1, opt.Value else 0, null
                            writeUnion out tag elementType [| value |]) x out
                    elif FSharpType.IsUnion (t, true) then
                        cacheGetOrAdd (t, fun x out ->
                            let case, fields = FSharpValue.GetUnionFields (x, t, true)
                            let fieldTypes = case.GetFields () |> Array.map (fun x -> x.PropertyType)
                            writeUnion out case.Tag fieldTypes fields) x out
                    elif FSharpType.IsTuple t then
                        let fieldTypes = FSharpType.GetTupleElements t
                        cacheGetOrAdd (t, fun x out -> writeTuple out fieldTypes (FSharpValue.GetTupleFields x)) x out
                    elif t.IsGenericType && List.contains (t.GetGenericTypeDefinition ()) [ typedefof<Dictionary<_, _>>; typedefof<Map<_, _>> ] then
                        let mapTypes = t.GetGenericArguments ()
                        let keyType = mapTypes.[0]
                        let valueType = mapTypes.[1]
                        cacheGetOrAdd (t, fun x out -> writeMap out keyType valueType (box x :?> IDictionary<obj, obj>)) x out
                    elif t.IsEnum then
                        cacheGetOrAdd (t, fun x -> writeInt64 (box x :?> int64)) x out
                    elif t.FullName = "Microsoft.FSharp.Core.int16`1" || t.FullName = "Microsoft.FSharp.Core.int32`1" || t.FullName = "Microsoft.FSharp.Core.int64`1" then
                        cacheGetOrAdd (t, fun x out -> writeInt64 (x :?> int64) out) x out
                    elif t.FullName = "Microsoft.FSharp.Core.decimal`1" then
                        cacheGetOrAdd (t, fun x out -> writeDouble (x :?> decimal |> float) out) x out
                    elif t.FullName = "Microsoft.FSharp.Core.float`1" then
                        cacheGetOrAdd (t, fun x out -> writeDouble (x :?> float) out) x out
                    elif t.FullName = "Microsoft.FSharp.Core.float32`1" then
                        cacheGetOrAdd (t, fun x out -> writeSingle (x :?> float32) out) x out
                    else
                        failwithf "Cannot serialize %s." t.Name
            
            serializerCache.Add (typeof<byte>, fun x out -> writeByte (x :?> byte) out)
            serializerCache.Add (typeof<sbyte>, fun x out -> writeInt64 (x :?> sbyte |> int64) out)
            serializerCache.Add (typeof<unit>, fun _ out -> writeNil out)
            serializerCache.Add (typeof<bool>, fun x out -> writeBool (x :?> bool) out)
            serializerCache.Add (typeof<string>, fun x out -> writeString (x :?> string) out)
            serializerCache.Add (typeof<int>, fun x out -> writeInt64 (x :?> int |> int64) out)
            serializerCache.Add (typeof<int16>, fun x out -> writeInt64 (x :?> int16 |> int64) out)
            serializerCache.Add (typeof<int64>, fun x out -> writeInt64 (x :?> int64) out)
            serializerCache.Add (typeof<UInt32>, fun x out -> writeUInt64 (x :?> UInt32 |> uint64) out)
            serializerCache.Add (typeof<UInt16>, fun x out -> writeUInt64 (x :?> UInt16 |> uint64) out)
            serializerCache.Add (typeof<UInt64>, fun x out -> writeUInt64 (x :?> UInt64) out)
            serializerCache.Add (typeof<float32>, fun x out -> writeSingle (x :?> float32) out)
            serializerCache.Add (typeof<float>, fun x out -> writeDouble (x :?> float) out)
            serializerCache.Add (typeof<decimal>, fun x out -> writeDouble (x :?> decimal |> float) out)
            serializerCache.Add (typeof<byte[]>, fun x out -> writeBin (x :?> byte[]) out)
            serializerCache.Add (typeof<bigint>, fun x out -> writeBin ((x :?> bigint).ToByteArray ()) out)
            serializerCache.Add (typeof<Guid>, fun x out -> writeBin ((x :?> Guid).ToByteArray ()) out)
            serializerCache.Add (typeof<DateTime>, fun x out -> writeInt64 (x :?> DateTime).Ticks out)
            serializerCache.Add (typeof<DateTimeOffset>, fun x out -> writeDateTimeOffset out (x :?> DateTimeOffset))
            serializerCache.Add (typeof<TimeSpan>, fun x out -> writeInt64 (x :?> TimeSpan).Ticks out)

        [<RequireQualifiedAccess;StringEnum(CaseRules.None)>]
        type InvocationTarget =
            | Invoke
            | Send

        let inline getInvocationTarget (message: InvocationMessage<_>) =
            if message.target = (unbox InvocationTarget.Invoke) then
                InvocationTarget.Invoke
            else InvocationTarget.Send

        type MsgPackProtocol () =
            member inline _.writeMessage<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (message: HubMessageBase) =
                match message.``type`` with
                | MessageType.Invocation ->
                    let invocation = unbox<InvocationMessage<'ClientApi>> message

                    match getInvocationTarget invocation with
                    | InvocationTarget.Send ->
                        Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Invocation (
                            invocation.headers,
                            invocation.invocationId,
                            invocation.target,
                            unbox invocation.arguments,
                            unbox<string [] option> invocation.streamIds
                        )
                    | InvocationTarget.Invoke ->
                        if invocation.arguments.Count = 2 then
                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvokeInvocation (
                                invocation.headers,
                                invocation.invocationId,
                                invocation.target,
                                invocation.arguments.Item(0),
                                unbox<System.Guid> (invocation.arguments.Item(1)),
                                unbox<string [] option> invocation.streamIds
                            )
                        else
                            let invocation = unbox<InvocationMessage<InvokeArg<'ClientApi>>> message

                            Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.InvocationExplicit (
                                invocation.headers,
                                invocation.invocationId,
                                invocation.target,
                                unbox invocation.arguments,
                                unbox<string [] option> invocation.streamIds
                            )
                | MessageType.StreamItem ->
                    let streamItem = unbox<StreamItemMessage<'ClientStreamToApi>> message
                    
                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamItem (
                        streamItem.headers,
                        Some streamItem.invocationId, 
                        streamItem.item
                    )
                | MessageType.Completion ->
                    let completion = unbox<CompletionMessage<unit>> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Completion (
                        completion.headers, 
                        completion.invocationId, 
                        completion.error, 
                        completion.result
                    )
                | MessageType.StreamInvocation ->
                    let streamInvocation = unbox<StreamInvocationMessage<'ClientStreamFromApi>> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.StreamInvocation (
                        streamInvocation.headers, 
                        streamInvocation.invocationId, 
                        streamInvocation.target,
                        unbox streamInvocation.arguments, 
                        unbox<string [] option> streamInvocation.streamIds
                    )
                | MessageType.CancelInvocation ->
                    let cancelInvocation = unbox<CancelInvocationMessage> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.CancelInvocation(cancelInvocation.headers, cancelInvocation.invocationId)
                | MessageType.Ping -> Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Ping
                | MessageType.Close -> 
                    let close = unbox<CloseMessage> message

                    Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>.Close(close.error, close.allowReconnect)
                // Shouldn't ever match
                | _ -> failwithf "Invalid message: %A" message
                |> fun msg -> 
                    let outArr = ResizeArray []
                    Write.writeObject msg typeof<Msg<'ClientStreamFromApi,'ClientApi,unit,'ClientStreamToApi>> outArr
                
                    JS.Constructors.Uint8Array.Create(outArr).buffer
                    |> U2.Case2
            
            #if FABLE_COMPILER
            member inline _.parseMsgs<'ServerApi,'ServerStreamApi> (buffer: JS.ArrayBuffer) (logger: ILogger) : ResizeArray<HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi>> =
            #else
            member _.parseMsgs<'ServerApi,'ServerStreamApi> (buffer: JS.ArrayBuffer) (logger: ILogger) : ResizeArray<HubMessage<unit,'ServerApi,'ServerApi,'ServerStreamApi>> =
            #endif
                try
                    let reader = Read.Reader(unbox (JS.Constructors.Uint8Array.Create(buffer)))

                    reader.Read typeof<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> unbox<Msg<unit,'ServerApi,'ServerApi,'ServerStreamApi>>
                    |> function
                    | Msg.Invocation (headers, invocationId, target, args, streamIds) ->
                        HubRecords.InvocationMessage.create<'ServerApi> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                        |> U8.Case1
                    | Msg.InvocationExplicit (headers, invocationId, target, args, streamIds) ->
                        HubRecords.InvocationMessage.create<InvokeArg<'ServerApi>> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                        |> U8.Case2
                    | Msg.StreamItem (headers, invocationId, item) ->
                        HubRecords.StreamItemMessage.create<'ServerStreamApi> headers invocationId item
                        |> U8.Case3
                    | Msg.Completion (headers, invocationId, error, result) ->
                        HubRecords.CompletionMessage.create<'ServerApi> headers invocationId error result
                        |> U8.Case4
                    
                    | Msg.CancelInvocation (headers, invocationId) ->
                        HubRecords.CancelInvocationMessage.create headers invocationId
                        |> U8.Case6
                    | Msg.Ping -> 
                        HubRecords.PingMessage.create()
                        |> U8.Case7
                    | Msg.Close (error, allowReconnect) ->
                        HubRecords.CloseMessage.create error allowReconnect
                        |> unbox<CloseMessage>
                        |> U8.Case8
                    // Shouldn't ever match
                    | Msg.InvokeInvocation (headers, invocationId, target, msg, invokeId, streamIds) ->
                        let args = ResizeArray [| box msg; box invokeId |]
                        HubRecords.InvocationMessage.create<obj> headers invocationId target args (unbox<ResizeArray<string> option> streamIds)
                        |> unbox
                        |> U8.Case1
                    // Shouldn't ever match
                    | Msg.StreamInvocation (headers, invocationId, target, args, streamIds) ->
                        HubRecords.StreamInvocationMessage.create<unit> headers invocationId target (ResizeArray args) (unbox<ResizeArray<string> option> streamIds)
                        |> U8.Case5
                    |> Array.singleton
                with e ->
                    sprintf "An error occured during message deserialization: %s" e.Message
                    |> logger.log LogLevel.Error
                    [||]
                |> ResizeArray

        let inline create<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> () =
            let protocol = MsgPackProtocol()
            { new IHubProtocol<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi,'ServerApi,'ServerStreamApi> with
                member _.name = "messagepack"
                member _.version = 1
                member _.transferFormat = TransferFormat.Binary

                member _.parseMessages (input, logger) = protocol.parseMsgs<'ServerApi,'ServerStreamApi> (unbox input) logger
                member _.writeMessage msg = protocol.writeMessage<'ClientApi,'ClientStreamFromApi,'ClientStreamToApi> (unbox msg) }
