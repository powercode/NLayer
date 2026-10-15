```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method           | Implementation | Mean      | Error     | StdDev    | Allocated |
|----------------- |--------------- |----------:|----------:|----------:|----------:|
| **ReadSamplesArray** | **Reference**      | **10.217 ms** | **0.1013 ms** | **0.0846 ms** | **192.14 KB** |
| ReadSamplesSpan  | Reference      | 10.441 ms | 0.0435 ms | 0.0364 ms | 192.14 KB |
| **ReadSamplesArray** | **Simd**           |  **8.468 ms** | **0.0267 ms** | **0.0514 ms** | **192.19 KB** |
| ReadSamplesSpan  | Simd           |  8.623 ms | 0.1208 ms | 0.2651 ms | 192.19 KB |
