namespace FSharp.Core.BenchmarkTests

open System
open BenchmarkDotNet.Attributes
open FSharp.Core

[<Config(typeof<AsyncValueConfig>)>]
type AsyncValueBenchmark () =
    let random = Random ()

    let prepareAsyncs n =
        let array = Array.zeroCreate n
        for i = 0 to n - 1 do
            let x = random.Next()
            array[i] <- async { return x }
        array

    let prepareAsyncValues nValues nAsyncs =
        let n = nValues + nAsyncs
        let array = Array.zeroCreate n
        for i = 0 to nValues - 1 do
            let x = random.Next()
            array[i] <- AsyncValue.ofValue x
        for i = nValues to n - 1 do
            let x = random.Next()
            array[i] <- AsyncValue.ofAsync (async { return x })
        array

    let mutable allValues : AsyncValue<_> [] = Array.empty
    let mutable values90Asyncs10 : AsyncValue<_> [] = Array.empty
    let mutable allAsyncValues : AsyncValue<_> [] = Array.empty
    let mutable allAsyncs : Async<_> [] = Array.empty

    [<GlobalSetup>]
    member __.Setup() =
        allValues <- prepareAsyncValues 100 0
        values90Asyncs10 <- prepareAsyncValues 90 10
        allAsyncValues <- prepareAsyncValues 0 100
        allAsyncs <- prepareAsyncs 100

    [<Benchmark>]
    member __.AsyncValueGetValue() =
        asyncValue { return 1337 } |> AsyncValue.get |> ignore

    [<Benchmark>]
    member __.AsyncValueGetAsync() =
        asyncValue { return! async { return 1337 } } |> AsyncValue.get |> ignore

    [<Benchmark>]
    member __.AsyncRunSynchronously() =
        async { return 1337 } |> Async.RunSynchronously |> ignore

    [<Benchmark>]
    member __.AsyncValueCollectionAllValues() =
        allValues |> AsyncValue.collectParallel |> AsyncValue.get |> ignore

    [<Benchmark>]
    member __.AsyncValueCollectionAllAsync() =
        allAsyncValues |> AsyncValue.collectParallel |> AsyncValue.get |> ignore

    [<Benchmark>]
    member __.AsyncCollection() =
        allAsyncs |> Async.Parallel |> Async.RunSynchronously |> ignore

    [<Benchmark>]
    member __.AsyncValueCollection90Values10Asyncs() =
        values90Asyncs10 |> AsyncValue.collectParallel |> AsyncValue.get |> ignore
