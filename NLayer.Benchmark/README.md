# NLayer decoder benchmarks

Run from the repository root, outside the debugger, using the .NET 10 SDK:

```powershell
dotnet run --project NLayer.Benchmark -c Release -- --filter '*' --artifacts BenchmarkDotNet.Artifacts/baseline
```

For a faster initial measurement, add `--job short`. For discovery only, use
`--list flat`. To focus on one area, use `--filter '*MpegFile*'`,
`--filter '*Polyphase*'`, or `--filter '*Hybrid*'`.

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
