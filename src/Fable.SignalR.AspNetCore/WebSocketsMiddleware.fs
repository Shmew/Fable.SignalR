namespace Fable.SignalR

#if !NET6_0
open FSharp.Control.Tasks.V2
#endif

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open System

type WebSocketsMiddleware (next: RequestDelegate, path: string) =
    member _.Invoke (httpContext: HttpContext) =
        task {
            let request = httpContext.Request

            let path = if path.StartsWith("/") then path else "/" + path

            match request.Query.TryGetValue("access_token") with
            | true, token when request.Path.StartsWithSegments(PathString(path), StringComparison.OrdinalIgnoreCase) ->
                request.Headers.Add("Authorization", StringValues("Bearer " + (string token)))
            | _ -> ()

            return! next.Invoke httpContext
        }
