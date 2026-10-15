using BenchmarkDotNet.Running;
using NLayer.Benchmark;

if (args.Length == 2 && args[0] == "--verify-simd")
    SimdVerification.Run(args[1]);
else
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
