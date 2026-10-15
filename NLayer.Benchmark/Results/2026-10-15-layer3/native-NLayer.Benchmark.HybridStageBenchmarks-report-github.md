```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method      | Implementation | Mean        | Error    | StdDev   | Allocated |
|------------ |--------------- |------------:|---------:|---------:|----------:|
| **ShortIMDCT**  | **Reference**      |    **14.99 ns** | **0.172 ns** | **0.161 ns** |         **-** |
| ShortBlocks | Reference      | 1,742.97 ns | 7.453 ns | 6.607 ns |         - |
| **ShortIMDCT**  | **Simd**           |    **13.10 ns** | **0.071 ns** | **0.063 ns** |         **-** |
| ShortBlocks | Simd           | 1,384.10 ns | 7.731 ns | 6.456 ns |         - |
