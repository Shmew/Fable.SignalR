namespace Fable.SignalR.Shared

module internal MemoryCache =
    open FSharp.Control
    open System
    open System.Collections.Concurrent
    open System.Collections.Generic
    open System.Timers

    [<RequireQualifiedAccess>]
    module Async =
        let lift x = async { return x }
        let map f a = 
            async {
                let! a = a
                return f a
            }

    type CacheExpirationPolicy =
        | NoExpiration
        | AbsoluteExpiration of TimeSpan
        | SlidingExpiration of TimeSpan
    
    type CacheEntryExpiration =
        | NeverExpires
        | ExpiresAt of DateTime
        | ExpiresAfter of TimeSpan
    
    type CacheEntry<'Key, 'Value> =
        { Key: 'Key
          Value: 'Value
          Expiration: CacheEntryExpiration
          LastUpdate: DateTime }

    module private CacheEntry =
        let key ce = ce.Key
        let value ce = ce.Value
        let expiration ce = ce.Expiration
        let lastUpdate ce = ce.LastUpdate
    
    module private CacheExpiration =
        let isExpired (entry: CacheEntry<_,_>) =
            match entry.Expiration with
            | NeverExpires -> false
            | ExpiresAt date -> DateTime.UtcNow > date
            | ExpiresAfter window -> (DateTime.UtcNow - entry.LastUpdate) > window
    
    type private IMemoryCacheStore<'Key, 'Value> =
        inherit IEnumerable<CacheEntry<'Key, 'Value>>

        abstract member Add: CacheEntry<'Key, 'Value> -> unit
        abstract member GetOrAdd: 'Key -> ('Key -> CacheEntry<'Key, 'Value>) -> CacheEntry<'Key, 'Value>
        abstract member Remove: 'Key -> unit
        abstract member Contains: 'Key -> bool
        abstract member Update: 'Key -> (CacheEntry<'Key, 'Value> -> CacheEntry<'Key, 'Value>) -> unit
        abstract member TryFind: 'Key -> CacheEntry<'Key, 'Value> option
    
    type MemoryCache<'Key, 'Value when 'Key : comparison and 'Value : equality> (?cacheExpirationPolicy) =
        let policy = defaultArg cacheExpirationPolicy NoExpiration
        
        let store = 
            let entries = ConcurrentDictionary<'Key, CacheEntry<'Key, 'Value>>()
            let _, getEnumerator =
                let values = entries |> Seq.map (fun kvp -> kvp.Value)
                (fun () -> values), (fun () -> values.GetEnumerator())
            
            { new IMemoryCacheStore<'Key, 'Value> with
                member _.Add entry = entries.AddOrUpdate(entry.Key, entry, fun _ _ -> entry) |> ignore
                member _.GetOrAdd key getValue = entries.GetOrAdd(key, getValue)
                member _.Remove key = entries.TryRemove key |> ignore
                member _.Contains key = entries.ContainsKey key
                member _.Update key update = 
                    match entries.TryGetValue(key) with
                    | (true, entry) -> entries.AddOrUpdate(key, entry, fun _ entry -> update entry) |> ignore
                    | _ -> ()
                member _.TryFind key =
                    match entries.TryGetValue(key) with
                    | (true, entry) -> Some entry
                    | _ -> None
                member _.GetEnumerator () = getEnumerator ()
                member _.GetEnumerator () = getEnumerator () :> Collections.IEnumerator }

        let checkExpiration () =
            store
            |> Seq.iter (fun item ->
                if CacheExpiration.isExpired item then
                    store.Remove(item.Key))
    
        let newCacheEntry key value =
            { Key = key
              Value = value
              Expiration = 
                match policy with
                | NoExpiration -> NeverExpires
                | AbsoluteExpiration time -> ExpiresAt (DateTime.UtcNow + time)
                | SlidingExpiration window -> ExpiresAfter window
              LastUpdate = DateTime.UtcNow }
    
        let get key = store.TryFind key |> Option.map (fun entry -> entry.Value)

        let add key value =
            match get key with
            | Some v ->
                (fun entry -> { entry with Value = value; LastUpdate = DateTime.UtcNow })
                |> store.Update key
            | None ->
                newCacheEntry key value
                |> store.Add

        let remove key = store.Remove key

        let getExpiry key =
            store.TryFind key 
            |> Option.map (fun entry -> entry.Expiration,entry.LastUpdate)
    
        let getKeysBy (chooser: CacheEntry<'Key,'Value> -> 'Key option) =
            Seq.choose chooser store

        let getOrAdd key value =
            let mutable changed = false
            
            store.GetOrAdd key (fun _ -> 
                changed <- true
                newCacheEntry key value)
            |> CacheEntry.value

        let getOrAddAsync key f =
            async {
                let mutable changed = false
                
                return
                    store.GetOrAdd key (fun _ ->
                        changed <- true
                    
                        Async.map (newCacheEntry key) f
                        |> Async.RunSynchronously
                    )
                    |> CacheEntry.value
            }

        let getOrAddResult key f =
            let mutable changed = false

            store.GetOrAdd key (fun _ -> 
                changed <- true
                newCacheEntry key <| f()
            )
            |> CacheEntry.value

        let tryGetOrAdd key f =
            get key
            |> function
            | Some res -> Ok res
            | None -> f() |> Result.map (getOrAdd key)
    
        let tryGetOrAddAsync key f =
            async {
                return!
                    get key
                    |> function
                    | Some res -> Async.lift (Ok res)
                    | None -> Async.map (Result.map (getOrAdd key)) f
            }

        let getTimer (expiration: TimeSpan) =
            match expiration with
            | _ when expiration.TotalSeconds < 1.0 -> TimeSpan.FromMilliseconds 100.0
            | _ when expiration.TotalMinutes < 1.0 -> TimeSpan.FromSeconds 1.0 
            | _ -> TimeSpan.FromMinutes 1.0
            |> fun interval -> new Timer(interval.TotalMilliseconds)
    
        let timer = 
            match policy with
            | NoExpiration -> None
            | AbsoluteExpiration time -> time |> getTimer |> Some
            | SlidingExpiration time -> time |> getTimer |> Some    
    
        let _ =
            match timer with
            | Some t -> 
                let disposable = t.Elapsed |> Observable.subscribe (fun _ -> checkExpiration())
                t.Start()
                Some disposable
            | None -> None
    
        member _.Add key value = add key value
        member _.Remove key = remove key
        member _.Get key = get key
        member _.GetExpiry key = getExpiry key
        member _.GetKeysBy chooser = getKeysBy chooser
        member _.GetOrAdd key value = getOrAdd key value
        member _.GetOrAddAsync key f = getOrAddAsync key f
        member _.GetOrAddResult key value = getOrAddResult key value
        member _.TryGetOrAdd key f = tryGetOrAdd key f
        member _.TryGetOrAddAsync key f = tryGetOrAddAsync key f

    let createCache<'T when 'T : equality> (expirationPolicy: CacheExpirationPolicy) =
        MemoryCache<Guid,'T>(cacheExpirationPolicy = expirationPolicy)

    type CacheItem<'T when 'T : equality> =
        { Id: Guid }

        member this.Add (cache: MemoryCache<Guid,'T>) (f: unit -> 'T) =
            cache.Add this.Id (f())

        member this.AddAsync (cache: MemoryCache<Guid,'T>) (f: Async<'T>) =
            async {
                let! value = f
                cache.Add this.Id value
            }

        member this.Get (cache: MemoryCache<Guid,'T>) =
            cache.Get this.Id

        member this.TryGetOrAdd (cache: MemoryCache<Guid,'T>) (f: unit -> Result<'T,'Error>) =
            cache.TryGetOrAdd this.Id f

        member this.TryGetOrAddAsync (cache: MemoryCache<Guid,'T>) (f: Async<Result<'T,'Error>>) =
            cache.TryGetOrAddAsync this.Id f

        member this.Update (cache: MemoryCache<Guid,'T>) (f: unit -> 'T) =
            cache.Remove this.Id
            cache.GetOrAdd this.Id (f())

        member this.UpdateAsync (cache: MemoryCache<Guid,'T>) (f: Async<'T>) =
            cache.Remove this.Id
            cache.GetOrAddAsync this.Id f

    [<AutoOpen>]
    module Memoization =
        type private MemoizationCache<'a,'b when 'a: equality and 'b: equality> (?cacheExpirationPolicy) =
            let cache = 
                match cacheExpirationPolicy with
                | Some policy -> new MemoryCache<string,'b>(policy)
                | None -> new MemoryCache<string,'b>()

            let getKey (key: 'a) = 
                if key |> box |> isNull
                then typeof<'a>.FullName
                else sprintf "%s_%d" typeof<'a>.FullName <| key.GetHashCode()

            member _.GetOrAdd key getValue =
                let keyString = getKey key
                cache.GetOrAddResult keyString getValue

            member _.GetOrAddAsync key getValue =
                let keyString = getKey key
                cache.GetOrAddAsync keyString getValue
    
        let private memoizeWithCache f (cache: MemoizationCache<_,_>) x = 
            (fun () -> f x) |> cache.GetOrAdd x            
    
        let private memoizeWithCacheAsync f (cache: MemoizationCache<_,_>) x = 
            f x |> cache.GetOrAddAsync x            

        /// Memoize a function.
        ///
        /// This optimization should only be used with referentially transparent functions
        let memoize f = new MemoizationCache<_,_>() |> memoizeWithCache f
        
        /// Memoize a function that returns an asynchronous result.
        ///
        /// This optimization should only be used with referentially transparent functions
        let memoizeAsync f = new MemoizationCache<_,_>() |> memoizeWithCacheAsync f

        /// Memoize a function with expiry. 
        ///
        /// Note: This optimization should only be used with referentially transparent functions
        let memoizeWithExpiration policy f = new MemoizationCache<_,_>(policy) |> memoizeWithCache f
        
        /// Memoize a function that returns an asynchronous result with expiry. 
        ///
        /// Note: This optimization should only be used with referentially transparent functions
        let memoizeWithExpirationAsync policy f = new MemoizationCache<_,_>(policy) |> memoizeWithCacheAsync f
