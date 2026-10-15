```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method           | Mean     | Error    | StdDev   | Allocated |
|----------------- |---------:|---------:|---------:|----------:|
| InversePolyphase | 10.51 μs | 0.030 μs | 0.025 μs |         - |
