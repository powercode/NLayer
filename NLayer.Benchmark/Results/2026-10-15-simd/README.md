# Synthesis and hybrid MDCT SIMD results

The original ten benchmarks were run sequentially with BenchmarkDotNet's default
measurement settings and `--affinity 1`, first against an archive of commit
`3d520fe`, then against the SIMD working tree. The original short-run reports in
`Baselines` are retained separately. No benchmark workload changed between runs.

Environment: Windows 11, Intel Core i9-13900K, .NET 10.0.13, SDK 10.0.402,
x64, AVX2, 256-bit vectors. Affinity mask 1 pins benchmark processes to logical
CPU zero. BenchmarkDotNet could not identify the CPU through WMI or change the
power plan; the CPU name was read from the registry. Results are specific to this
machine and the synthetic workloads.

These timings precede the span-based SIMD helper signatures. That refactor was
validated for bit-exact output and zero kernel allocations across all three
dispatch modes; benchmarks were not repeated for the signature change.

| Workload | Scalar mean | SIMD mean | Time reduction |
|---|---:|---:|---:|
| ReadSamples array, 2,304-float buffer | 23.26 ms | 9.485 ms | 59.2% |
| ReadSamples span, 2,304-float buffer | 23.17 ms | 9.384 ms | 59.5% |
| ReadSamples array, 16,384-float buffer | 23.25 ms | 9.370 ms | 59.7% |
| ReadSamples span, 16,384-float buffer | 23.27 ms | 9.460 ms | 59.3% |
| Polyphase, one channel/granule | 10.51 us | 2.424 us | 76.9% |
| Hybrid MDCT, long | 2.246 us | 1.559 us | 30.6% |
| Hybrid MDCT, start | 2.204 us | 1.552 us | 29.6% |
| Hybrid MDCT, short | 1.915 us | 1.694 us | 11.5% |
| Hybrid MDCT, mixed | 1.964 us | 1.669 us | 15.0% |
| Hybrid MDCT, stop | 2.209 us | 1.550 us | 29.8% |

Each full decode processes 400 generated frames (approximately 10.45 seconds of
audio), including decoder construction and disposal. All ten cases improved;
none approached the 3% regression limit. Primed synthesis and hybrid kernels
allocate zero managed bytes per operation. Full decoding still allocates about
310 KB per operation for the decoder and stream infrastructure.

The adjacent reports contain errors, standard deviations and allocation figures.
Reports prefixed `baseline-` and `optimized-` are the unchanged original suite.
Reports prefixed `stages-` compare individual methods against a separately built
scalar .NET 10 assembly in isolated load contexts, using default measurement
settings and the same affinity. Stage measurements include delegate invocation;
mutating stages include restoring input buffers.

Individual stages also improved: BuildUVec 165.79 -> 24.15 ns, window accumulation
315.57 -> 39.73 ns, DCT32 92.89 -> 71.31 ns, long IMDCT 44.49 -> 33.13 ns,
long blocks 2.090 -> 1.529 us, and short blocks 1.769 -> 1.663 us. DCT16's gain
was small, so it was repeated with two process launches: 29.58 -> 29.04 ns
(about 1.8%, zero allocations). Its confirmation report is included separately.

## Correctness and compatibility

`Verify-Simd.ps1` passed 7,778,356 sample bit-pattern comparisons in each of three
fresh processes: native 256-bit vectors, AVX disabled (128-bit vectors), and all
hardware intrinsics disabled (scalar fallback). Coverage includes signed zero,
subnormals, impulses, random magnitudes, history wraparound, equalization, channel
independence, resets, MDCT window transitions and complete generated-file decoding
through array/span APIs and all stereo output modes. Primed kernel allocation
checks passed in each process.

All 41 existing tests passed in each of the same three dispatch modes. The
library built for netstandard2.0, net8.0 and net10.0 with zero warnings/errors.
The repository's global.json selects Microsoft.Testing.Platform while the tests
use VSTest, so the existing suite was run with `dotnet vstest` after building it.
No runner configuration was changed.

SIMD code is restricted to .NET 10 and uses portable Vector256/Vector128 APIs.
Older targets retain their scalar code. Numerical operations retain scalar
rounding and addition order; sign changes use the sign bit. ARM64 was not
available for validation; the 128-bit path was exercised on x64.

## Reproduction

```powershell
./NLayer.Benchmark/Verify-Simd.ps1
dotnet run --project NLayer.Benchmark -c Release -- --filter '*MpegFileBenchmarks*' '*PolyphaseBenchmarks*' '*HybridMdctBenchmarks*' --affinity 1 --artifacts BenchmarkDotNet.Artifacts/optimized-pinned
$env:NLAYER_SCALAR_ASSEMBLY = (Resolve-Path NLayer/bin/scalar/NLayer.dll).Path
dotnet run --project NLayer.Benchmark -c Release -- --filter '*StageBenchmarks*' --affinity 1 --artifacts BenchmarkDotNet.Artifacts/stages-pinned
Remove-Item Env:NLAYER_SCALAR_ASSEMBLY
```

For the original-suite scalar comparison, use a separate checkout of `3d520fe`
and run the identical filter and affinity there. Store its reports separately.
Run baseline, optimized and stage measurements sequentially. If offline restore
fails while fetching NuGet audit data, temporarily set `$env:NuGetAudit = 'false'`
in the measurement shell.
