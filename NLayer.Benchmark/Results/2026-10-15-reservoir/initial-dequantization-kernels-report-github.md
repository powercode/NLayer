```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-RLBEIC : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Affinity=00000000000000000000000000000001  

```
| Method | Block | Implementation | Mean       | Error     | StdDev    | Median     | Allocated |
|------- |------ |--------------- |-----------:|----------:|----------:|-----------:|----------:|
| **Apply**  | **Long**  | **Reference**      | **2,619.8 ns** |  **16.94 ns** |  **14.14 ns** | **2,617.7 ns** |         **-** |
| **Apply**  | **Long**  | **Simd**           |   **134.1 ns** |   **0.73 ns** |   **0.68 ns** |   **134.2 ns** |         **-** |
| **Apply**  | **Mixed** | **Reference**      | **3,515.6 ns** |  **33.53 ns** |  **31.36 ns** | **3,517.1 ns** |         **-** |
| **Apply**  | **Mixed** | **Simd**           |   **275.5 ns** |   **1.68 ns** |   **1.40 ns** |   **275.6 ns** |         **-** |
| **Apply**  | **Short** | **Reference**      | **3,867.8 ns** | **272.46 ns** | **803.35 ns** | **3,344.0 ns** |         **-** |
| **Apply**  | **Short** | **Simd**           |   **273.4 ns** |   **0.91 ns** |   **0.81 ns** |   **273.5 ns** |         **-** |
