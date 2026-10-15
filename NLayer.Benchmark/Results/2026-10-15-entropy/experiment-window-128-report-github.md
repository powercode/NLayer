```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2

Affinity=00000000000000000000000000000001  

```
| Method         | Implementation | Mean     | Error    | StdDev   | Allocated |
|--------------- |--------------- |---------:|---------:|---------:|----------:|
| **DewindowOutput** | **Reference**      | **77.84 ns** | **0.177 ns** | **0.148 ns** |         **-** |
| **DewindowOutput** | **Simd**           | **80.06 ns** | **1.386 ns** | **1.424 ns** |         **-** |
