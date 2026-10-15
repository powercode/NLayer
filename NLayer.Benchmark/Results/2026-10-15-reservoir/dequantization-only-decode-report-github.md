```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-LTHMEF : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  InvocationCount=32  IterationCount=10  
UnrollFactor=1  WarmupCount=15  

```
| Method           | Workload | Implementation | Mean      | Error     | StdDev    | Allocated |
|----------------- |--------- |--------------- |----------:|----------:|----------:|----------:|
| **ReadSamplesArray** | **Sparse**   | **Reference**      |  **9.574 ms** | **0.0546 ms** | **0.0286 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Reference      |  9.522 ms | 0.1363 ms | 0.0811 ms | 192.15 KB |
| **ReadSamplesArray** | **Sparse**   | **Simd**           |  **8.629 ms** | **0.1070 ms** | **0.0560 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Simd           |  8.705 ms | 0.4073 ms | 0.2424 ms | 192.15 KB |
| **ReadSamplesArray** | **Entropy**  | **Reference**      | **16.128 ms** | **0.3046 ms** | **0.1812 ms** |  **435.9 KB** |
| ReadSamplesSpan  | Entropy  | Reference      | 16.216 ms | 0.2712 ms | 0.1794 ms | 435.89 KB |
| **ReadSamplesArray** | **Entropy**  | **Simd**           | **13.623 ms** | **0.3279 ms** | **0.1951 ms** |  **435.9 KB** |
| ReadSamplesSpan  | Entropy  | Simd           | 13.431 ms | 0.2049 ms | 0.1220 ms |  435.9 KB |
