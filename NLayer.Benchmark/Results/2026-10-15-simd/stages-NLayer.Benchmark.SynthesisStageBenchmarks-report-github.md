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
| **BuildUVec**          | **Scalar**         | **165.79 ns** | **1.072 ns** | **0.895 ns** |         **-** |
| DewindowOutput     | Scalar         | 315.57 ns | 2.827 ns | 2.644 ns |         - |
| Dct32              | Scalar         |  92.89 ns | 0.569 ns | 0.504 ns |         - |
| Dct16              | Scalar         |  29.29 ns | 0.307 ns | 0.287 ns |         - |
| EqualizedSynthesis | Scalar         | 581.58 ns | 2.772 ns | 2.458 ns |         - |
| **BuildUVec**          | **Simd**           |  **24.15 ns** | **0.115 ns** | **0.102 ns** |         **-** |
| DewindowOutput     | Simd           |  39.73 ns | 0.238 ns | 0.211 ns |         - |
| Dct32              | Simd           |  71.31 ns | 0.310 ns | 0.259 ns |         - |
| Dct16              | Simd           |  28.94 ns | 0.211 ns | 0.176 ns |         - |
| EqualizedSynthesis | Simd           | 130.22 ns | 1.508 ns | 1.411 ns |         - |
