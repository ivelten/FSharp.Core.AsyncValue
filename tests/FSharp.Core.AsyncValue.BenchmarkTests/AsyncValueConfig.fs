namespace FSharp.Core.BenchmarkTests

open BenchmarkDotNet.Configs
open BenchmarkDotNet.Columns
open BenchmarkDotNet.Diagnosers
open BenchmarkDotNet.Exporters

type AsyncValueConfig() as this=
    inherit ManualConfig()
    do  this.AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(StatisticColumn.Mean)
            .AddColumn(StatisticColumn.Min)
            .AddColumn(StatisticColumn.Max)
            .AddColumn(StatisticColumn.OperationsPerSecond)
            .AddExporter(MarkdownExporter.GitHub)
            .AddExporter(Csv.CsvExporter(Csv.CsvSeparator.Comma)) |> ignore
