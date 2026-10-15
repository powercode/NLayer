#if NET10_0_OR_GREATER && !NLAYER_DISABLE_SIMD
using System;
using System.Runtime.Intrinsics;

namespace NLayer.Decoder
{
    abstract partial class LayerDecoderBase
    {
        // Initialized lazily after the outer class's synthesis table is available.
        static class SynthesisCoefficients
        {
            internal static readonly float[] Dct32 = Select(16, 0, 2);
            internal static readonly float[] Dct16 = Select(8, 1, 4);

            static float[] Select(int count, int start, int stride)
            {
                var result = new float[count];
                for (int i = 0; i < count; i++) result[i] = SYNTH_COS64_TABLE[start + i * stride];
                return result;
            }
        }

        static void DctButterflySimd(ReadOnlySpan<float> input, Span<float> even, Span<float> odd,
            ReadOnlySpan<float> coefficients)
        {
            int count = even.Length;
            if (Vector256.IsHardwareAccelerated)
            {
                for (int i = 0; i < count; i += 8)
                {
                    var a = Simd.Load256(input, i);
                    var b = Simd.Reverse(Simd.Load256(input, count * 2 - i - 8));
                    Simd.Store(a + b, even, i);
                    Simd.Store((a - b) * Simd.Load256(coefficients, i), odd, i);
                }
            }
            else
            {
                for (int i = 0; i < count; i += 4)
                {
                    var a = Simd.Load128(input, i);
                    var b = Simd.Reverse(Simd.Load128(input, count * 2 - i - 4));
                    Simd.Store(a + b, even, i);
                    Simd.Store((a - b) * Simd.Load128(coefficients, i), odd, i);
                }
            }
        }

        static void BuildUVecSimd(Span<float> output, ReadOnlySpan<float> history, int k)
        {
            for (int block = 0; block < 512; block += 64)
            {
                if (Vector256.IsHardwareAccelerated)
                {
                    var sign = Vector256.Create(-0.0f);
                    for (int i = 0; i < 16; i += 8)
                    {
                        Simd.Store(Simd.Load256(history, k + i + 16), output, block + i);
                        Simd.Store(Simd.Reverse(Simd.Load256(history, k + 24 - i)) ^ sign, output, block + i + 17);
                    }
                    k = (k + 32) & 511;
                    for (int i = 0; i < 16; i += 8)
                    {
                        Simd.Store(Simd.Reverse(Simd.Load256(history, k + 9 - i)) ^ sign, output, block + i + 32);
                        Simd.Store(Simd.Load256(history, k + i) ^ sign, output, block + i + 48);
                    }
                }
                else
                {
                    var sign = Vector128.Create(-0.0f);
                    for (int i = 0; i < 16; i += 4)
                    {
                        Simd.Store(Simd.Load128(history, k + i + 16), output, block + i);
                        Simd.Store(Simd.Reverse(Simd.Load128(history, k + 28 - i)) ^ sign, output, block + i + 17);
                    }
                    k = (k + 32) & 511;
                    for (int i = 0; i < 16; i += 4)
                    {
                        Simd.Store(Simd.Reverse(Simd.Load128(history, k + 13 - i)) ^ sign, output, block + i + 32);
                        Simd.Store(Simd.Load128(history, k + i) ^ sign, output, block + i + 48);
                    }
                }
                output[block + 16] = 0f;
                k = (k + 32) & 511;
            }
        }

        static void DewindowOutputSimd(ReadOnlySpan<float> input, Span<float> output)
        {
            // Lanes represent independent PCM samples. Accumulate rows in exactly
            // the scalar order, starting with row zero rather than adding to zero.
            // Separate multiply/add operations deliberately avoid FMA rounding.
            if (Vector256.IsHardwareAccelerated)
            {
                // Four independent output groups hide addition latency. Each
                // lane still visits all sixteen rows in the original order.
                var a = Simd.Load256(input, 0) * Simd.Load256(DEWINDOW_TABLE, 0);
                var b = Simd.Load256(input, 8) * Simd.Load256(DEWINDOW_TABLE, 8);
                var c = Simd.Load256(input, 16) * Simd.Load256(DEWINDOW_TABLE, 16);
                var d = Simd.Load256(input, 24) * Simd.Load256(DEWINDOW_TABLE, 24);
                for (int row = 32; row < 512; row += 32)
                {
                    a += Simd.Load256(input, row) * Simd.Load256(DEWINDOW_TABLE, row);
                    b += Simd.Load256(input, row + 8) * Simd.Load256(DEWINDOW_TABLE, row + 8);
                    c += Simd.Load256(input, row + 16) * Simd.Load256(DEWINDOW_TABLE, row + 16);
                    d += Simd.Load256(input, row + 24) * Simd.Load256(DEWINDOW_TABLE, row + 24);
                }
                Simd.Store(a, output, 0);
                Simd.Store(b, output, 8);
                Simd.Store(c, output, 16);
                Simd.Store(d, output, 24);
            }
            else
            {
                for (int i = 0; i < 32; i += 4)
                {
                    var sum = Simd.Load128(input, i) * Simd.Load128(DEWINDOW_TABLE, i);
                    for (int row = 32; row < 512; row += 32)
                        sum += Simd.Load128(input, row + i) * Simd.Load128(DEWINDOW_TABLE, row + i);
                    Simd.Store(sum, output, i);
                }
            }
        }
    }
}
#endif
