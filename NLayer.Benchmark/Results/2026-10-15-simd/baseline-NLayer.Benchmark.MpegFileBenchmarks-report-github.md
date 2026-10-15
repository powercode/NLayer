```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method           | BufferSize | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|----------------- |----------- |---------:|---------:|---------:|------:|----------:|------------:|
| **ReadSamplesArray** | **2304**       | **23.26 ms** | **0.128 ms** | **0.120 ms** |  **1.00** | **310.23 KB** |        **1.00** |
| ReadSamplesSpan  | 2304       | 23.17 ms | 0.169 ms | 0.149 ms |  1.00 | 310.23 KB |        1.00 |
|                  |            |          |          |          |       |           |             |
| **ReadSamplesArray** | **16384**      | **23.25 ms** | **0.088 ms** | **0.074 ms** |  **1.00** | **310.23 KB** |        **1.00** |
| ReadSamplesSpan  | 16384      | 23.27 ms | 0.169 ms | 0.150 ms |  1.00 | 310.23 KB |        1.00 |
