module FSharp.Core.BenchmarkTests.Program

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let switcher = BenchmarkSwitcher [| typeof<AsyncValueBenchmark> |]
    switcher.Run argv |> ignore
    0
