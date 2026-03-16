```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8037)
13th Gen Intel Core i7-13650HX, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  Job-ENICXL : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2

IterationCount=5  WarmupCount=2  

```
| Method                       | Mean         | Error        | StdDev     | Ratio | RatioSD | Gen0      | Gen1    | Allocated  | Alloc Ratio |
|----------------------------- |-------------:|-------------:|-----------:|------:|--------:|----------:|--------:|-----------:|------------:|
| FluentValidation_10k_Valid   |  2,744.62 μs | 1,045.633 μs | 271.548 μs |  1.01 |    0.13 |  527.3438 |       - |  6640000 B |        1.00 |
| FluentValidation_10k_Invalid | 31,232.30 μs |   648.899 μs | 100.418 μs | 11.47 |    1.01 | 7125.0000 | 31.2500 | 89520131 B |       13.48 |
| LiteSg_10k_Valid             |     65.81 μs |     3.575 μs |   0.553 μs |  0.02 |    0.00 |         - |       - |          - |        0.00 |
| LiteSg_10k_Invalid           |    204.06 μs |    28.510 μs |   7.404 μs |  0.07 |    0.01 |   95.4590 |       - |  1200000 B |        0.18 |
