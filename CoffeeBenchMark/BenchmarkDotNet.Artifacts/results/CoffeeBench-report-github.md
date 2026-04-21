```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8246/25H2/2025Update/HudsonValley2)
Intel Core i7-8550U CPU 1.80GHz (Max: 1.99GHz) (Kaby Lake R), 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method   | Mean     | Error    | StdDev   | Median   | Gen0   | Allocated |
|--------- |---------:|---------:|---------:|---------:|-------:|----------:|
| UseSplit | 57.78 ns | 1.245 ns | 3.031 ns | 57.51 ns | 0.0305 |     128 B |
| UseSpan  | 19.91 ns | 1.002 ns | 2.809 ns | 18.85 ns | 0.0134 |      56 B |
