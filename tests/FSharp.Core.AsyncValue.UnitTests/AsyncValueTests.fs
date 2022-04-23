module FSharp.Core.AsyncValue.UnitTests.AsyncValueTests

open NUnit.Framework
open Swensen.Unquote
open FSharp.Core

[<Test>]
let ``AsyncValue computation allows to return constant values`` () =
    let value = asyncValue { return 1 }
    test <@ AsyncValue.isAsync value = false @>
    test <@ AsyncValue.isValue value = true @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation allows to return from Async computation`` () =
    let value = asyncValue { return! async { return 1 } }
    test <@ AsyncValue.isAsync value = true @>
    test <@ AsyncValue.isValue value = false @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation allows to return from another AsyncValue computation`` () =
    let value = asyncValue { return! asyncValue { return 1 } }
    test <@ AsyncValue.isAsync value = false @>
    test <@ AsyncValue.isValue value = true @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation allows to bind Async computations`` () =
    let value = asyncValue {
        let! result = async { return 1 }
        return result }
    test <@ AsyncValue.isAsync value = true @>
    test <@ AsyncValue.isValue value = false @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation allows to bind Tasks`` () =
    let value = asyncValue {
        let! result = task { return 1 }
        return result }
    test <@ AsyncValue.isAsync value = true @>
    test <@ AsyncValue.isValue value = false @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation allows to bind another AsyncValue computation`` () =
    let value = asyncValue {
        let! result = asyncValue { return 1 }
        return result }
    test <@ AsyncValue.isAsync value = false @>
    test <@ AsyncValue.isValue value = true @>
    test <@ AsyncValue.get value = 1 @>

[<Test>]
let ``AsyncValue computation defines zero value`` () =
    let value = asyncValue.Zero ()
    test <@ AsyncValue.isAsync value = false @>
    test <@ AsyncValue.isValue value = true @>
    test <@ AsyncValue.get value = Unchecked.defaultof<_> @>

[<Test>]
let ``AsyncValue can be returned from Async computation`` () =
    let value = async { return! asyncValue { return 1 } }
    test <@ Async.RunSynchronously value = 1 @>

[<Test>]
let ``AsyncValue can be bound inside Async computation`` () =
    let value = async {
        let! result = asyncValue { return 1 }
        return result }
    test <@ Async.RunSynchronously value = 1 @>

[<Test>]
let ``AsyncValue sequential collection resolves all values in order of execution`` () =
    let mutable flag = "none"
    let a = async {
        do! Async.Sleep 100
        flag <- "a"
        return 2 }
    let b = async {
        flag <- "b"
        return 4 }
    let array = [| Value 1; Async a; Value 3; Async b |]
    let values = array |> AsyncValue.collectSequential
    test <@ AsyncValue.get values = [| 1; 2; 3; 4 |] @>
    test <@ flag = "b" @>

[<Test>]
let ``AsyncValue parallel collection resolves all values with no order of execution`` () =
    let mutable flag = "none"
    let a = async {
        do! Async.Sleep 100
        flag <- "a"
        return 2 }
    let b = async {
        flag <- "b"
        return 4 }
    let array = [| Value 1; Async a; Value 3; Async b |]
    let values = array |> AsyncValue.collectParallel
    test <@ AsyncValue.get values = [| 1; 2; 3; 4 |] @>
    test <@ flag = "a" @>
