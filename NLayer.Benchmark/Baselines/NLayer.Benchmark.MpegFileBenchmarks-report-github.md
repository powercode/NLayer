```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method           | BufferSize | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------- |----------- |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| **ReadSamplesArray** | **2304**       | **22.45 ms** |  **1.458 ms** | **0.080 ms** |  **1.00** |    **0.00** | **310.16 KB** |        **1.00** |
| ReadSamplesSpan  | 2304       | 22.77 ms |  3.561 ms | 0.195 ms |  1.01 |    0.01 | 310.16 KB |        1.00 |
|                  |            |          |           |          |       |         |           |             |
| **ReadSamplesArray** | **16384**      | **23.41 ms** | **14.960 ms** | **0.820 ms** |  **1.00** |    **0.04** | **310.16 KB** |        **1.00** |
| ReadSamplesSpan  | 16384      | 22.82 ms |  6.687 ms | 0.367 ms |  0.98 |    0.03 | 310.16 KB |        1.00 |
