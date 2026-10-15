```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v2

Affinity=00000000000000000000000000000001  

```
| Method             | Implementation | Mean      | Error    | StdDev   | Allocated |
|------------------- |--------------- |----------:|---------:|---------:|----------:|
| **AntiAliasLong**      | **Reference**      | **315.16 ns** | **4.096 ns** | **3.631 ns** |         **-** |
| AntiAliasMixed     | Reference      |  27.05 ns | 0.174 ns | 0.154 ns |         - |
| FrequencyInversion | Reference      |  63.54 ns | 0.447 ns | 0.397 ns |         - |
| **AntiAliasLong**      | **Simd**           |  **84.20 ns** | **1.055 ns** | **0.986 ns** |         **-** |
| AntiAliasMixed     | Simd           |  19.10 ns | 0.316 ns | 0.280 ns |         - |
| FrequencyInversion | Simd           |  32.52 ns | 0.151 ns | 0.141 ns |         - |
