```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2

Affinity=00000000000000000000000000000001  

```
| Method      | Implementation | Mean        | Error     | StdDev    | Allocated |
|------------ |--------------- |------------:|----------:|----------:|----------:|
| **ShortIMDCT**  | **Reference**      |    **14.83 ns** |  **0.116 ns** |  **0.108 ns** |         **-** |
| ShortBlocks | Reference      | 1,711.50 ns |  6.912 ns |  6.127 ns |         - |
| **ShortIMDCT**  | **Simd**           |    **10.48 ns** |  **0.044 ns** |  **0.034 ns** |         **-** |
| ShortBlocks | Simd           | 1,376.30 ns | 13.738 ns | 12.178 ns |         - |
