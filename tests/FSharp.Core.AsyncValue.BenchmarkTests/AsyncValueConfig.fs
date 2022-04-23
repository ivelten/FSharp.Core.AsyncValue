namespace FSharp.Core.BenchmarkTests

open BenchmarkDotNet.Configs
open BenchmarkDotNet.Columns
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Exporters
open BenchmarkDotNet.Exporters.Csv

type AsyncValueConfig () as this =
    inherit ManualConfig ()
    do  this.AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            .AddColumn(StatisticColumn.OperationsPerSecond)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(CsvExporter(CsvSeparator.Comma)) |> ignore
