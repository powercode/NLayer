```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method      | Implementation | Mean        | Error     | StdDev    | Allocated |
|------------ |--------------- |------------:|----------:|----------:|----------:|
| **ShortIMDCT**  | **Reference**      |    **14.58 ns** |  **0.074 ns** |  **0.065 ns** |         **-** |
| ShortBlocks | Reference      | 1,745.39 ns | 23.068 ns | 21.577 ns |         - |
| **ShortIMDCT**  | **Simd**           |    **10.51 ns** |  **0.153 ns** |  **0.136 ns** |         **-** |
| ShortBlocks | Simd           | 1,458.47 ns | 21.244 ns | 19.871 ns |         - |
