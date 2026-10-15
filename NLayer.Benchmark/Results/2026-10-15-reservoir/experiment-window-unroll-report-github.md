```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method         | Implementation | Mean     | Error    | StdDev   | Allocated |
|--------------- |--------------- |---------:|---------:|---------:|----------:|
| **DewindowOutput** | **Reference**      | **43.28 ns** | **0.493 ns** | **0.754 ns** |         **-** |
| **DewindowOutput** | **Simd**           | **51.04 ns** | **1.007 ns** | **1.034 ns** |         **-** |
