```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9711)
Unknown processor
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method | Block | Mean     | Error     | StdDev    | Allocated |
|------- |------ |---------:|----------:|----------:|----------:|
| **Apply**  | **Long**  | **4.595 μs** | **2.1820 μs** | **0.1196 μs** |         **-** |
| **Apply**  | **Start** | **4.167 μs** | **0.0816 μs** | **0.0045 μs** |         **-** |
| **Apply**  | **Short** | **3.148 μs** | **0.1274 μs** | **0.0070 μs** |         **-** |
| **Apply**  | **Mixed** | **3.056 μs** | **6.5265 μs** | **0.3577 μs** |         **-** |
| **Apply**  | **Stop**  | **4.174 μs** | **0.0210 μs** | **0.0011 μs** |         **-** |
