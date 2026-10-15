```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NCZKIH : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  InvocationCount=32  UnrollFactor=1  
WarmupCount=15  

```
| Method           | Workload | Implementation | Mean      | Error     | StdDev    | Allocated |
|----------------- |--------- |--------------- |----------:|----------:|----------:|----------:|
| **ReadSamplesArray** | **Sparse**   | **Reference**      |  **9.786 ms** | **0.0465 ms** | **0.0388 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Reference      |  9.769 ms | 0.0647 ms | 0.0573 ms | 192.15 KB |
| **ReadSamplesArray** | **Sparse**   | **Simd**           |  **8.669 ms** | **0.0602 ms** | **0.0502 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Simd           |  8.708 ms | 0.1034 ms | 0.0916 ms | 192.15 KB |
| **ReadSamplesArray** | **Entropy**  | **Reference**      | **29.205 ms** | **0.0974 ms** | **0.0864 ms** |  **435.9 KB** |
| ReadSamplesSpan  | Entropy  | Reference      | 29.205 ms | 0.1540 ms | 0.1366 ms |  435.9 KB |
| **ReadSamplesArray** | **Entropy**  | **Simd**           | **14.308 ms** | **0.1139 ms** | **0.1010 ms** |  **435.9 KB** |
| ReadSamplesSpan  | Entropy  | Simd           | 14.313 ms | 0.1217 ms | 0.1079 ms |  435.9 KB |
