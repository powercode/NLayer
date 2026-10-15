```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-MOGWNV : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  InvocationCount=64  UnrollFactor=1  
WarmupCount=15  

```
| Method           | Implementation | Mean      | Error     | StdDev    | Allocated |
|----------------- |--------------- |----------:|----------:|----------:|----------:|
| **ReadSamplesArray** | **Reference**      | **10.278 ms** | **0.0614 ms** | **0.0513 ms** | **192.15 KB** |
| ReadSamplesSpan  | Reference      | 10.243 ms | 0.0859 ms | 0.0803 ms | 192.15 KB |
| **ReadSamplesArray** | **Simd**           |  **8.740 ms** | **0.0795 ms** | **0.0705 ms** | **192.15 KB** |
| ReadSamplesSpan  | Simd           |  8.798 ms | 0.0744 ms | 0.0660 ms | 192.15 KB |
