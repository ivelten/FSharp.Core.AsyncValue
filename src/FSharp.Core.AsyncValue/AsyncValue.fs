namespace FSharp.Core

open System.Collections.Generic
open System.Threading.Tasks

#nowarn "25"

/// A struct used to operate on both synchronous values and Async computations.
[<Struct; NoEquality; NoComparison>]
type AsyncValue<'T> =
    /// Indicates that the AsyncValue structure contains an asynchronous computation.
    | Async of computation : Async<'T>
    /// Indicates that the AsyncValue structure contains a synchronous value.
    | Value of value : 'T
    /// Indicates that the AsyncValue structure holds an exception raised during the operation of computing the value.
    | Failure of error : exn

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AsyncValue =
    let inline isAsync (x : AsyncValue<'T>) =
        match x with
        | Async _ -> true
        | _ -> false

    let inline isValue (x : AsyncValue<'T>) =
        match x with
        | Value _ -> true
        | _ -> false

    let inline isFailure (x : AsyncValue<'T>) =
        match x with
        | Failure _ -> true
        | _ -> false

    let get (x : AsyncValue<'T>) =
        match x with
        | Async c -> Async.RunSynchronously c
        | Value v -> v
        | Failure e -> raise e

    let inline ofAsync (x : Async<'T>) = Async x

    let inline ofValue (x : 'T) = Value x

    let inline ofTask (x : Task<'T>) = x |> Async.AwaitTask |> Async

    let toAsync (x : AsyncValue<'T>) =
        match x with
        | Async c -> c
        | Value v -> async.Return v
        | Failure e -> async.Return (raise e)

    let toTask (x : AsyncValue<'T>) =
        match x with
        | Async c -> Async.StartAsTask c
        | Value v -> Task.FromResult v
        | Failure e -> Task.FromResult (raise e)

    let map (mapper : 'T -> 'U) (x : AsyncValue<'T>) =
        match x with
        | Value v -> Value (mapper v)
        | Async c ->
            async {
                let! result = c
                return mapper result
            } |> Async
        | Failure e -> Failure e

    let rescue (rescuer : exn -> 'T) (x : AsyncValue<'T>) =
        match x with
        | Value v -> Value v
        | Async c ->
            async {
                try return! c
                with e -> return rescuer e
            } |> Async
        | Failure e -> Value (rescuer e)

    let fold (folder : 'State -> 'T -> 'State) (zero : 'State) (x : AsyncValue<'T>) =
        match x with
        | Value v -> Value (folder zero v)
        | Async c ->
            async {
                let! result = c
                return folder zero result
            } |> Async
        | Failure e -> Failure e

    let bind (binder : 'T -> AsyncValue<'U>) (x : AsyncValue<'T>) =
        match x with
        | Value v -> binder v
        | Async c ->
            async {
                let! result = c
                let bound = binder result
                match bound with
                | Value v -> return v
                | Async c -> return! c
                | Failure e -> return raise e
            } |> Async
        | Failure e -> Failure e

    let bindAsync (binder : 'T -> AsyncValue<'U>) (x : Async<'T>) =
        async {
            let! result = x
            let bound = binder result
            match bound with
            | Value v -> return v
            | Async c -> return! c
            | Failure e -> return raise e
        } |> Async

    let bindTask (binder : 'T -> AsyncValue<'U>) (x : Task<'T>) =
        x |> Async.AwaitTask |> bindAsync binder

    let collectSequential (values: AsyncValue<'T> []) =
        if values.Length = 0 then Value Array.empty
        elif values |> Array.exists isAsync then
            async {
                let results = Array.zeroCreate values.Length
                for i = 0 to values.Length - 1 do
                    let v = values.[i]
                    match v with
                    | Value v -> results.[i] <- v
                    | Async a ->
                        let! result = a
                        results.[i] <- result
                    | Failure f ->
                        results.[i] <- raise f
                return results } |> Async
        else values |> Array.map (fun (Value v) -> v) |> Value

    let collectParallel (values: AsyncValue<'T> []) =
        if values.Length = 0 then Value Array.empty
        else
            let indexes = List<_>(0)
            let continuations = List<_>(0)
            let results = Array.zeroCreate values.Length
            for i = 0 to values.Length - 1 do
                let value = values.[i]
                match value with
                | Value v -> results.[i] <- v
                | Async a ->
                    indexes.Add i
                    continuations.Add a
                | Failure f ->
                    results.[i] <- raise f
            if indexes.Count = 0
            then Value results
            else
                async {
                    let! values = continuations |> Async.Parallel
                    for i = 0 to indexes.Count - 1 do
                        results.[indexes.[i]] <- values.[i]
                    return results } |> Async

    let appendParallel (values: AsyncValue<'T []> []) =
        values
        |> collectParallel
        |> map (Array.fold Array.append Array.empty)

    let appendSequential (values: AsyncValue<'T []> []) =
        values
        |> collectSequential
        |> map (Array.fold Array.append Array.empty)

[<Sealed>]
type AsyncValueBuilder () =
    member __.Zero () = Value Unchecked.defaultof<'T>

    member __.Return (value : 'T) = AsyncValue.ofValue value

    member __.ReturnFrom (value : AsyncValue<'T>) = value

    member __.ReturnFrom (computation : Async<'T>) = AsyncValue.ofAsync computation

    member __.ReturnFrom (task : Task<'T>) = AsyncValue.ofTask task

    member __.Bind (value : AsyncValue<'T>, binder : 'T -> AsyncValue<'U>) =
        AsyncValue.bind binder value

    member __.Bind (computation : Async<'T>, binder : 'T -> AsyncValue<'U>) =
        AsyncValue.bindAsync binder computation

    member __.Bind (task : Task<'T>, binder : 'T -> AsyncValue<'U>) =
        AsyncValue.bindTask binder task

[<AutoOpen>]
module AsyncValueExtensions =
    let asyncValue = AsyncValueBuilder ()

    type AsyncBuilder with
        member __.ReturnFrom (value : AsyncValue<'T>) =
            match value with
            | Value v -> async.Return v
            | Async c -> async.ReturnFrom c
            | Failure e -> async.Return (raise e)

        member __.Bind (value : AsyncValue<'T>, binder : 'T -> Async<'U>) =
            match value with
            | Value v -> async.Bind (async.Return v, binder)
            | Async c -> async.Bind (c, binder)
            | Failure e -> async.Bind (async.Return (raise e), binder)
