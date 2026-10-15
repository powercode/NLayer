# Reservoir indexing and band-wise dequantization

Reference: `02baf61`, including Huffman prefix lookup and earlier synthesis SIMD.
The candidate keeps the reservoir size constant when calculating circular
indices, allowing constant-divisor code generation without changing signed
remainder semantics. This applies to all target frameworks.

On .NET 10, Huffman decoding now writes unscaled coefficients into the existing
sample buffer. A separate span-based pass computes each band's scale once and
applies `(coefficient * globalGain) * scale`, using Vector256, Vector128 or scalar
arithmetic. Zero coefficients retain the original positive-zero fast path;
nonzero coefficients retain both multiplication rounding steps. Band boundaries,
mixed-block boundaries and retained short-window lookup history are preserved.
Older targets and `NLayerDisableSimd=true` retain immediate scalar dequantization.
No additional frame/granule storage or public runtime switches are introduced.

## Measurements

Windows 11, Intel Core i9-13900K, .NET 10.0.13, SDK 10.0.402, x64 AVX2.
BenchmarkDotNet 0.15.8; benchmarks run sequentially with `--affinity 1`.
Reports contain means, 99.9% confidence intervals, deviations and allocations.
`Reference` loads the saved reference DLL in an isolated assembly-load context;
`Simd` denotes the candidate, including optimizations that do not use SIMD.

Full decoding uses fifteen warmups, ten measured iterations, thirty-two
invocations per iteration and unroll factor one. Both implementations decode the
same 400 generated frames with a 4,096-float output buffer, including seek/reset.

The default-job Huffman comparison repeats the exploratory kernel run. Mean
pair/quad times fall from 11.703 to 8.703 ns (table 1), 17.656 to 13.819 ns (13),
16.848 to 13.924 ns (31), 15.452 to 9.037 ns (32), and 15.367 to 8.824 ns (33):
17–43% less time, with zero allocations. These isolate reservoir indexing;
dequantization and synthesis are outside the measured Huffman kernel.

The default-job dequantization kernel comparison initially had a noisy short-block
reference. Repeating all block types with fifteen warmups and thirty measured
iterations confirmed the gains with much narrower intervals:

| 576-coefficient scaling, including input copy | Reference | Candidate | Speedup |
|---|---:|---:|---:|
| Long | 2,678 ns | 132.8 ns | 20.2× |
| Mixed | 3,548 ns | 271.1 ns | 13.1× |
| Short | 3,449 ns | 270.7 ns | 12.7× |

These are isolated scaling kernels; Huffman decoding is excluded. Both the
initial default-job report and the longer-warmup repeat are retained. The
candidate allocates zero bytes in every kernel comparison.

| Combined changes vs `02baf61` | Reference | Candidate | Time reduction |
|---|---:|---:|---:|
| Sparse, array | 9.708 ms | 8.341 ms | 14.1% |
| Sparse, span | 9.569 ms | 8.767 ms | 8.4% |
| Entropy, array | 16.400 ms | 13.721 ms | 16.3% |
| Entropy, span | 16.437 ms | 13.711 ms | 16.6% |

An independent comparison disables only band-wise dequantization in the
reference build (`NLayerDisableBatchDequantization=true`), keeping the reservoir
and existing SIMD paths identical. Dequantization alone reduces sparse array/span
time by 9.9%/8.6% and entropy array/span time by 15.5%/17.2%. Allocations stay at
192.15 KB for sparse and approximately 435.9 KB for entropy; there are no new
steady-state kernel allocations. Independently run comparisons have different
reference timings; percentage improvements should not be added.

The sparse fixture primarily exercises table 1 and long blocks; entropy exercises
larger codebooks, linbits and both count1 tables with uniformly generated symbols.
Neither fixture is representative of all music. Direct kernel tests provide
short/mixed-block and additional sample-rate coverage.

## Rejected window experiment

Unrolling the existing four-accumulator Vector256 window loop three rows at a
time increased mean kernel time from 43.282 ns to 51.038 ns (17.9%). It was
reverted; the window implementation remains identical to `02baf61`.
The experimental report is retained separately from accepted changes.

## Correctness

- 104,231 direct reservoir checks against the saved `02baf61` assembly cover
  0..32-bit peeks/reads, truncated input, every bit offset, circular boundaries,
  single-bit reads, skips and rewinds, checking returned bits and reader state.
- 590,185 Huffman differential cases cover all symbols/tables, signs, linbits,
  alignments, wraparound and incomplete prefixes; concurrent initialization passes.
- 17,464,136 sample bit patterns match in each native 256-bit, AVX-disabled
  128-bit and all-intrinsics-disabled process. This includes direct band scaling
  against the unchanged scalar formula for all nine layouts, long/start/short/
  mixed/stop blocks, both channels/granules, partial bands, changing scales,
  signed zero, subnormals and retained lookup history after sample-rate changes.
- Generated sparse/dense decoding covers array/span APIs, buffer sizes, stereo
  output modes and reset/replay. Steady-state kernel allocation checks pass.
- All 41 existing tests pass natively, with AVX disabled, and with all intrinsics
  disabled. Release builds pass for netstandard2.0, net8.0 and net10.0.

## Reproduction

Save `02baf61`'s Release net10.0 assembly before building the candidate. Set its
absolute path as `NLAYER_REFERENCE_ASSEMBLY`, then run:

```powershell
dotnet run --project NLayer.Benchmark -c Release -- --filter '*HuffmanBenchmarks*' '*DequantizationBenchmarks*' --affinity 1
dotnet run --project NLayer.Benchmark -c Release -- --filter '*DequantizationBenchmarks*' --affinity 1 --warmupCount 15 --iterationCount 30
dotnet run --project NLayer.Benchmark -c Release -- --filter '*DecodeComparisonBenchmarks*' --affinity 1 --warmupCount 15 --iterationCount 10 --invocationCount 32 --unrollFactor 1
./NLayer.Benchmark/Verify-Simd.ps1
```

For the dequantization-only full-decode comparison, build the candidate library
with `-p:NLayerDisableBatchDequantization=true -p:OutputPath=bin/reservoir-only/`
and point `NLAYER_REFERENCE_ASSEMBLY` to that assembly. Build the timed candidate
without the property. Do not replace the saved baseline DLL.
