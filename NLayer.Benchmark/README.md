# NLayer decoder benchmarks

The first SIMD implementation and its default-job comparison are documented in
[the 2026-10-15 results](Results/2026-10-15-simd/README.md).
The follow-up short-window, anti-aliasing and frequency-inversion pass is recorded
in [the Layer III results](Results/2026-10-15-layer3/README.md).

Run from the repository root, outside the debugger, using the .NET 10 SDK:

```powershell
dotnet run --project NLayer.Benchmark -c Release -- --filter '*' --artifacts BenchmarkDotNet.Artifacts/baseline
```

For a faster initial measurement, add `--job short`. For discovery only, use
`--list flat`. To focus on one area, use `--filter '*MpegFile*'`,
`--filter '*Polyphase*'`, or `--filter '*Hybrid*'`.

The original ten cases can be selected without the additional stage benchmarks:

```powershell
dotnet run --project NLayer.Benchmark -c Release -- --filter '*MpegFileBenchmarks*' '*PolyphaseBenchmarks*' '*HybridMdctBenchmarks*'
```

BenchmarkDotNet records runtime/CPU information, timing distributions and managed
allocations in the artifacts directory. Save the baseline reports before making
decoder changes, then run with a different artifacts directory (for example
`BenchmarkDotNet.Artifacts/optimized`) on the same machine and power settings.
Short runs are useful for iteration; use the default job to confirm small changes.

## Workloads

* `MpegFileBenchmarks`: decode 400 generated MPEG-1 Layer III stereo frames
  (approximately 10.45 seconds of audio) from memory to EOF. Measures both array
  and span APIs with 2,304- and 16,384-float buffers. Each operation includes
  stream/decoder construction and disposal; input generation, disk I/O and output
  buffer allocation are excluded. The array API is the within-run ratio baseline.
  Setup validates the decoded sample count and finite, nonzero output.
* `PolyphaseBenchmarks`: synthesize one channel of one granule (576 samples),
  including Layer III's subband transpose and output copy. History is primed and
  then carried between invocations, as in streaming decoding.
* `HybridMdctBenchmarks`: apply one channel of one granule with long, start,
  short, mixed or stop windows. Includes a 576-float input copy because the
  transform overwrites its input; overlap history is retained between calls.

The kernel benchmarks use seeded, nonzero spectral input. Internal methods are
bound to delegates once in setup, avoiding reflection invocation in timed code
and avoiding changes to NLayer's visibility or public API. Delegate invocation
overhead is included. These measurements isolate kernels, rather than reproduce
their precise inlining/call context in a full decode.

The generated MP3 uses Huffman table 1, long blocks and no bit reservoir. It is a
repeatable synthetic baseline, not a representative music corpus: it does not
cover all Huffman tables, joint stereo, reservoir handling, MPEG-2/2.5 or short
block entropy decoding. The separate MDCT benchmarks exercise other window types.
No external encoder, media downloads or binary fixtures are required.

BenchmarkDotNet: https://benchmarkdotnet.org/

## Initial baseline (2026-10-15)

The reports in [Baselines](Baselines) record a successful run of all ten cases
with `--job short` against decoder commit `d0d74db`, before optimization.
Release build succeeded with zero warnings. Fixture validation produced 921,600
samples with peak amplitude 0.13307124.

The run used .NET 10.0.13, SDK 10.0.402 and x64 AVX2 on Windows 11. BenchmarkDotNet
could not identify the processor or set the power plan. Some short-run confidence
intervals are wide; these reports establish an initial reference, not evidence
of small differences between APIs or window types. Full logs and CSV/HTML reports
are retained locally under `BenchmarkDotNet.Artifacts/baseline` (git-ignored).

## SIMD correctness

```powershell
./NLayer.Benchmark/Verify-Simd.ps1
```

This builds .NET 10 with `-p:NLayerDisableSimd=true` into `NLayer/bin/scalar`,
then builds the optimized verifier into a separate output directory. It compares
the two assemblies in isolated load contexts on the same runtime in three fresh
processes: native vector width, AVX disabled (128-bit on x64), and all hardware
intrinsics disabled. The original environment variables are restored afterwards.
The printed vector widths show which paths the current machine exercised.

Comparisons use sample bit patterns, including negative zero, subnormals, silence,
impulses and seeded random inputs at several magnitudes. They exercise synthesis
history wraparound, equalization, both channels, reset/replay, MDCT block types
and transitions, output sentinels, and full generated-MP3 decoding through array
and span APIs at several buffer sizes and every stereo output mode. Primed
synthesis and hybrid kernels must allocate zero bytes during repeated calls.

To compare against an existing scalar .NET 10 assembly instead, pass
`-ScalarAssembly <path>` to the script. The verifier rejects an assembly containing
the SIMD implementation. It can also be run directly:

```powershell
dotnet NLayer.Benchmark/bin/verify/NLayer.Benchmark.dll --verify-simd NLayer/bin/scalar/NLayer.dll
```

## Individual stages

`SynthesisStageBenchmarks` measures buffer rearrangement, window accumulation,
DCT32, DCT16, and synthesis with equalization. `HybridStageBenchmarks` measures
the long transform, long blocks with windowing, and short blocks with overlap.
They restore inputs for methods that mutate them, including the same copy cost
for scalar and SIMD. Every stage uses preallocated buffers and closed delegates.

Set an absolute scalar assembly path to compare implementations in the same job:

```powershell
$env:NLAYER_SCALAR_ASSEMBLY = (Resolve-Path NLayer/bin/scalar/NLayer.dll).Path
dotnet run --project NLayer.Benchmark -c Release -- --filter '*StageBenchmarks*' --artifacts BenchmarkDotNet.Artifacts/stages
Remove-Item Env:NLAYER_SCALAR_ASSEMBLY
```

To compare against a previous SIMD checkpoint, set `NLAYER_REFERENCE_ASSEMBLY`
to that build's absolute DLL path instead. It takes precedence over the scalar
assembly setting and labels that implementation `Reference` in reports.
`LayerIIIStageBenchmarks` covers long/mixed anti-aliasing and frequency inversion;
`HybridStageBenchmarks.ShortIMDCT` isolates the short transform and window.

`DecodeComparisonBenchmarks` compares array/span decoding against the same
reference assembly using 400 generated frames and a 4,096-float buffer. It reuses
a decoder, seeks to zero before each timed operation, and decodes to EOF. Seeking
and decoder reset are included; construction, input generation and delegate
binding are excluded. Its timings are separate from the original constructor-based
`MpegFileBenchmarks` workload.

On a hybrid-core CPU, use the same `--affinity 1` mask for baseline and optimized
runs to pin benchmark processes to logical CPU zero. Run measurements sequentially.
When offline NuGet vulnerability auditing prevents BenchmarkDotNet's generated
project from restoring, set `$env:NuGetAudit = 'false'` for that measurement shell.
No repository-wide audit setting is changed.
