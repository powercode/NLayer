```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method             | Implementation | Mean      | Error    | StdDev   | Allocated |
|------------------- |--------------- |----------:|---------:|---------:|----------:|
| **AntiAliasLong**      | **Reference**      | **354.61 ns** | **3.672 ns** | **3.255 ns** |         **-** |
| AntiAliasMixed     | Reference      |  28.26 ns | 0.175 ns | 0.155 ns |         - |
| FrequencyInversion | Reference      |  64.40 ns | 0.543 ns | 0.481 ns |         - |
| **AntiAliasLong**      | **Simd**           |  **39.90 ns** | **0.433 ns** | **0.405 ns** |         **-** |
| AntiAliasMixed     | Simd           |  17.49 ns | 0.140 ns | 0.117 ns |         - |
| FrequencyInversion | Simd           |  26.88 ns | 0.169 ns | 0.158 ns |         - |
