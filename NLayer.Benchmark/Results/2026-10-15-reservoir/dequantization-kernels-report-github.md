```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-ZDYHKL : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  IterationCount=30  WarmupCount=15  

```
| Method | Block | Implementation | Mean       | Error    | StdDev    | Allocated |
|------- |------ |--------------- |-----------:|---------:|----------:|----------:|
| **Apply**  | **Long**  | **Reference**      | **2,677.7 ns** | **28.00 ns** |  **40.16 ns** |         **-** |
| **Apply**  | **Long**  | **Simd**           |   **132.8 ns** |  **1.05 ns** |   **1.44 ns** |         **-** |
| **Apply**  | **Mixed** | **Reference**      | **3,548.5 ns** | **25.78 ns** |  **36.97 ns** |         **-** |
| **Apply**  | **Mixed** | **Simd**           |   **271.1 ns** |  **1.89 ns** |   **2.65 ns** |         **-** |
| **Apply**  | **Short** | **Reference**      | **3,449.1 ns** | **85.76 ns** | **122.99 ns** |         **-** |
| **Apply**  | **Short** | **Simd**           |   **270.7 ns** |  **1.57 ns** |   **2.10 ns** |         **-** |
