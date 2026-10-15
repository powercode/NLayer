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
| **DewindowOutput** | **Reference**      | **48.96 ns** | **0.373 ns** | **0.331 ns** |         **-** |
| **DewindowOutput** | **Simd**           | **41.98 ns** | **0.284 ns** | **0.266 ns** |         **-** |
