```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-PMTNYO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  LaunchCount=2  

```
| Method | Implementation | Mean     | Error    | StdDev   | Allocated |
|------- |--------------- |---------:|---------:|---------:|----------:|
| **Dct16**  | **Scalar**         | **29.58 ns** | **0.261 ns** | **0.375 ns** |         **-** |
| **Dct16**  | **Simd**           | **29.04 ns** | **0.278 ns** | **0.399 ns** |         **-** |
