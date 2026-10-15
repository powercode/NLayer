# Huffman lookup and window accumulation

Reference: commit `c7f68ce`, including the previous SIMD passes. The candidate
replaces Huffman's linear linked-list search with an eight-bit root lookup and
at most one suffix lookup. Seventeen lazy codebooks share storage across table
aliases; all entry arrays together contain 6,842 integers (27,368 bytes). Sign
handling, linbits, float lookup values and reservoir consumption are preserved.
The lookup applies to all target frameworks and is not a SIMD operation. `Simd`
is the candidate label inherited from the benchmark comparison harness.

The .NET 10 256-bit window path now maintains four independent sums concurrently.
Each sample's sixteen products and fifteen additions retain their scalar order.
The 128-bit experiment was slower and was reverted.

## Measurements

Windows 11, Intel Core i9-13900K, .NET 10.0.13, SDK 10.0.402, x64 AVX2.
BenchmarkDotNet 0.15.8; runs were sequential and pinned to logical CPU zero with
`--affinity 1`. Kernel measurements use default job settings. Complete decoding
uses fifteen warmups, thirty-two invocations per iteration and unroll factor one
to allow tiered compilation to settle. Raw reports include confidence intervals,
standard deviations, allocations and runtime details.

| Kernel | Reference | Candidate |
|---|---:|---:|
| Huffman pair, table 1 | 13.102 ns | 9.280 ns |
| Huffman pair, table 13 | 141.413 ns | 16.581 ns |
| Huffman pair, table 31 (linbits) | 142.065 ns | 16.105 ns |
| Huffman quad, table 32 | 19.408 ns | 11.015 ns |
| Huffman quad, table 33 | 19.105 ns | 10.330 ns |
| Window accumulation, 256-bit | 48.96 ns | 41.98 ns |

All retained kernel changes improve beyond measurement noise and allocate zero
managed bytes per operation. Huffman timings include complete pair/quad decoding
with sign and escape bits, not only symbol lookup. Uniformly sampled symbols
stress the large tables; these distributions are not a prediction for music.

The archived 128-bit window experiment measured 77.84 -> 80.06 ns. The final code
keeps the reference 128-bit loop. ARM64 was not available for measurement.

| Full decode | Reference | Candidate | Time reduction |
|---|---:|---:|---:|
| Sparse, array | 9.786 ms | 8.669 ms | 11.4% |
| Sparse, span | 9.769 ms | 8.708 ms | 10.9% |
| Entropy, array | 29.205 ms | 14.308 ms | 51.0% |
| Entropy, span | 29.205 ms | 14.313 ms | 51.0% |

Each invocation resets an existing decoder and decodes 400 frames to EOF using a
4,096-float buffer. Construction/input generation are excluded; seeking/reset are
included. Measured managed allocations are unchanged: 192.15 KB for Sparse and
435.9 KB for Entropy. All four complete-decoding cases improve.

Sparse is valid MPEG-1 stereo at 128 kbps, using 32 table-1 `(1,1)` pairs per
granule/channel. Entropy uses 320-kbps frames with pair tables 13/16/24/31, escape
bits and both count1 tables. Both are generated long-block workloads with no bit
reservoir dependency, and neither is a representative music corpus. The user's
original profiling input was not available; these results do not claim to
reproduce its exact speedup.

## Fixture correction

The earlier sparse generator incorrectly wrote two code bits for table 1's
`(1,1)` symbol, whose actual code is three zero bits. It also specified 128 rather
than 160 bits for 32 pairs including sign bits. Both are corrected in this pass.
Reference and candidate decode the exact same corrected data in the comparisons
above. Earlier reports remain historical results on their original input; their
full-decoding means should not be compared directly with these figures. Separate
DSP kernel measurements remain valid independently of the generator.

## Correctness and compatibility

The direct Huffman comparison against the committed decoder passes 590,185 cases:
all symbols and table IDs, sign combinations, zero/maximum linbits, every initial
bit alignment, three circular-buffer positions, all incomplete code prefixes,
decoded bit patterns and reader positions. Forward-generated codewords are also
checked against their expected symbols. Eight synchronized workers verify cold
initialization of all aliases of codebook 16 in a separate assembly context.

The full differential verifier passes 16,815,704 sample bit-pattern comparisons
per process, including both sparse and entropy streams, all stereo modes,
array/span APIs, buffer sizes, seek/replay and the existing DSP checks. Fresh
processes exercise 256-bit, 128-bit and scalar dispatch. The numerical reference
disables both SIMD and Huffman lookup. All kernel allocation checks pass.

All 41 existing tests pass in all three dispatch modes. The library builds for
netstandard2.0, net8.0 and net10.0 without warnings/errors. Public APIs and MPEG
bit-consumption behavior are unchanged. `NLayerDisableHuffmanLookup=true` retains
the original linked-list decoder for build-time comparison or rollback.

## Reproduction

Build and preserve a net10.0 copy of commit `c7f68ce`, then from the current tree:

```powershell
./NLayer.Benchmark/Verify-Simd.ps1
$env:NLAYER_REFERENCE_ASSEMBLY = 'C:/path/to/c7f68ce/NLayer.dll'
dotnet run --project NLayer.Benchmark -c Release -- --filter '*HuffmanBenchmarks*' '*SynthesisStageBenchmarks.DewindowOutput*' --affinity 1 --artifacts BenchmarkDotNet.Artifacts/entropy-kernels
dotnet run --project NLayer.Benchmark -c Release -- --filter '*DecodeComparisonBenchmarks*' --affinity 1 --warmupCount 15 --invocationCount 32 --unrollFactor 1 --artifacts BenchmarkDotNet.Artifacts/entropy-decode
Remove-Item Env:NLAYER_REFERENCE_ASSEMBLY
```

If offline NuGet audit data prevents restore, temporarily set
`$env:NuGetAudit = 'false'` in that shell. No repository-wide audit setting changed.
