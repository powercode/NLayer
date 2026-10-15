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
| **LongIMDCT**   | **Scalar**         |    **44.49 ns** |  **0.342 ns** |  **0.320 ns** |         **-** |
| LongBlocks  | Scalar         | 2,090.05 ns | 19.111 ns | 17.877 ns |         - |
| ShortBlocks | Scalar         | 1,768.75 ns | 14.795 ns | 12.355 ns |         - |
| **LongIMDCT**   | **Simd**           |    **33.13 ns** |  **0.193 ns** |  **0.171 ns** |         **-** |
| LongBlocks  | Simd           | 1,529.43 ns | 17.344 ns | 15.375 ns |         - |
| ShortBlocks | Simd           | 1,662.76 ns |  9.787 ns |  8.676 ns |         - |
