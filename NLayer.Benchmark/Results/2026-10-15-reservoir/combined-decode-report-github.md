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
| **ReadSamplesArray** | **Sparse**   | **Reference**      |  **9.708 ms** | **0.1432 ms** | **0.0852 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Reference      |  9.569 ms | 0.1083 ms | 0.0645 ms | 192.15 KB |
| **ReadSamplesArray** | **Sparse**   | **Simd**           |  **8.341 ms** | **0.0745 ms** | **0.0443 ms** | **192.15 KB** |
| ReadSamplesSpan  | Sparse   | Simd           |  8.767 ms | 0.2405 ms | 0.1431 ms | 192.15 KB |
| **ReadSamplesArray** | **Entropy**  | **Reference**      | **16.400 ms** | **0.2569 ms** | **0.1529 ms** | **435.89 KB** |
| ReadSamplesSpan  | Entropy  | Reference      | 16.437 ms | 0.2215 ms | 0.1318 ms |  435.9 KB |
| **ReadSamplesArray** | **Entropy**  | **Simd**           | **13.721 ms** | **0.1789 ms** | **0.0936 ms** |  **435.9 KB** |
| ReadSamplesSpan  | Entropy  | Simd           | 13.711 ms | 0.2745 ms | 0.1633 ms |  435.9 KB |
