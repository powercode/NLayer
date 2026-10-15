# Short windows, anti-aliasing and frequency inversion

Historical fixture note: the later [entropy pass](../2026-10-15-entropy/README.md)
corrects table-1 code lengths in the generated MP3. Full-file timings here use the
previous generator; the independent DSP kernel measurements are unaffected.

This pass compares against commit `749a3ba`, which already contains vectorized
synthesis and hybrid overlap/window processing. `Reference` is a saved .NET 10
build of that checkpoint; `Simd` contains the three additional optimizations.

All kernels use spans. Short windowing rearranges six transform values and
applies twelve window coefficients with separate sign changes and multiplication.
Anti-aliasing operates on eight independent butterflies at each subband boundary.
Frequency inversion flips sign bits at odd samples of odd subbands. Arithmetic
order and signed zero are preserved; older targets and unsupported hardware keep
the scalar implementations. No public API changed.

## Kernel results

BenchmarkDotNet 0.15.8, default measurement settings, .NET 10.0.13 / SDK 10.0.402,
Windows 11, Intel Core i9-13900K, logical CPU zero (`--affinity 1`). Runs were
sequential. Native uses 256-bit vectors with 128-bit tails; the second run sets
`DOTNET_EnableAVX=0` to exercise 128-bit dispatch. ARM64 was not available.

| Kernel | Native reference | Native SIMD | 128-bit reference | 128-bit SIMD |
|---|---:|---:|---:|---:|
| Short IMDCT including window | 14.99 ns | 13.10 ns | 14.83 ns | 10.48 ns |
| Full granule of short blocks | 1,742.97 ns | 1,384.10 ns | 1,711.50 ns | 1,376.30 ns |
| Anti-alias, long blocks | 354.61 ns | 39.90 ns | 315.16 ns | 84.20 ns |
| Anti-alias, mixed blocks | 28.26 ns | 17.49 ns | 27.05 ns | 19.10 ns |
| Frequency inversion | 64.40 ns | 26.88 ns | 63.54 ns | 32.52 ns |

All candidates improved in both dispatch modes and allocated zero managed bytes
per kernel operation. Mutating stages restore input buffers on every invocation;
the same copy cost is included for both implementations. Adjacent reports record
confidence intervals, standard deviations, runtime details and allocations.

The `experiment-narrow-window` report tests using three 128-bit groups even when
256-bit vectors are available. It improves the isolated short transform
(14.58 -> 10.51 ns) but gives slower complete short-block processing than the
wider candidate (1,458.47 ns versus 1,384.10 ns). The wider candidate is retained
for native dispatch, with the 128-bit fallback preserved.

## Full decoding

`DecodeComparisonBenchmarks` processes 400 generated frames through array/span
APIs with a 4,096-float buffer. Each timed invocation seeks an existing decoder
to zero and decodes to EOF. Construction and input generation are excluded;
seeking and decoder reset are included. Its means must not be compared directly
with the original constructor-based `MpegFileBenchmarks` reports.

The initial default-job comparison is retained as `decode-initial`. Tiered-JIT
transitions continued into some measurements, so a confirmation run uses fifteen
warmups and sixty-four invocations per iteration. The confirmation report is the
primary full-decoding comparison.

| API | Reference | SIMD | Time reduction |
|---|---:|---:|---:|
| Array | 10.278 ms | 8.740 ms | 15.0% |
| Span | 10.243 ms | 8.798 ms | 14.1% |

Both cases improved beyond measurement noise, with unchanged measured managed
allocations (192.15 KB per operation). No full-decoding case regressed.

The generated MP3 uses long blocks. It measures anti-aliasing and frequency
inversion improvements; short-window gains are established by the kernel suite.
The narrow-window experiment does not affect this generated-file workload.

## Correctness

`Verify-Simd.ps1` passed 9,831,460 sample bit-pattern comparisons in each of three
fresh processes: native 256-bit, 128-bit, and scalar dispatch. The reference for
these correctness checks is a separately built scalar .NET 10 assembly, loaded
in an isolated assembly context on the same runtime.

New checks cover short transforms at offsets 0, 6 and 12; long/mixed anti-aliasing;
frequency inversion and double inversion; anti-aliasing followed by inversion;
an impulse at every spectral line; signed zeros; subnormals; random magnitudes;
and sentinel elements beyond output boundaries. Existing synthesis, MDCT
transition, reset/replay, full-file and stereo-mode comparisons remain active.
All primed kernel allocation checks pass.

The existing 41 tests passed in native, 128-bit and scalar dispatch modes. All
three library targets (netstandard2.0, net8.0, net10.0) built cleanly. The scalar
fallback and public decoder interfaces are unchanged.

## Reproduction

Build a copy of commit `749a3ba` for net10.0 and preserve its DLL before building
the current source. Set its absolute path in `NLAYER_REFERENCE_ASSEMBLY`:

```powershell
./NLayer.Benchmark/Verify-Simd.ps1
$env:NLAYER_REFERENCE_ASSEMBLY = 'C:/path/to/checkpoint/NLayer.dll'
dotnet run --project NLayer.Benchmark -c Release -- --filter '*LayerIIIStageBenchmarks*' '*HybridStageBenchmarks.Short*' --affinity 1
dotnet run --project NLayer.Benchmark -c Release -- --filter '*DecodeComparisonBenchmarks*' --affinity 1 --warmupCount 15 --invocationCount 64 --unrollFactor 1
$env:DOTNET_EnableAVX = '0'
dotnet run --project NLayer.Benchmark -c Release -- --filter '*LayerIIIStageBenchmarks*' '*HybridStageBenchmarks.Short*' --affinity 1
Remove-Item Env:DOTNET_EnableAVX
Remove-Item Env:NLAYER_REFERENCE_ASSEMBLY
```

Use separate artifacts directories for each run. If offline NuGet audit data
prevents restore, temporarily set `$env:NuGetAudit = 'false'` for that shell.
