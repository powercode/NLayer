#if NET10_0_OR_GREATER && !NLAYER_DISABLE_SIMD
using System;
using System.Runtime.Intrinsics;

namespace NLayer.Decoder
{
    sealed partial class LayerIIIDecoder
    {
        static void AntiAliasSimd(Span<float> samples, int boundaries)
        {
            // Each lane is one independent butterfly across a subband boundary.
            // Keep the two products separate to preserve scalar rounding.
            if (Vector256.IsHardwareAccelerated)
            {
                var cs = Simd.Load256(_scs, 0);
                var ca = Simd.Load256(_sca, 0);
                for (int boundary = 0, offset = SSLIMIT; boundary < boundaries; boundary++, offset += SSLIMIT)
                {
                    var upper = Simd.Reverse(Simd.Load256(samples, offset - 8));
                    var lower = Simd.Load256(samples, offset);
                    Simd.Store(Simd.Reverse(upper * cs - lower * ca), samples, offset - 8);
                    Simd.Store(lower * cs + upper * ca, samples, offset);
                }
            }
            else
            {
                for (int boundary = 0, offset = SSLIMIT; boundary < boundaries; boundary++, offset += SSLIMIT)
                {
                    for (int i = 0; i < 8; i += 4)
                    {
                        var upper = Simd.Reverse(Simd.Load128(samples, offset - i - 4));
                        var lower = Simd.Load128(samples, offset + i);
                        var cs = Simd.Load128(_scs, i);
                        var ca = Simd.Load128(_sca, i);
                        Simd.Store(Simd.Reverse(upper * cs - lower * ca), samples, offset - i - 4);
                        Simd.Store(lower * cs + upper * ca, samples, offset + i);
                    }
                }
            }
        }

        static void FrequencyInversionSimd(Span<float> samples)
        {
            // Only odd samples of odd subbands change sign. XOR preserves the
            // other lanes exactly, including negative zero and subnormals.
            if (Vector256.IsHardwareAccelerated)
            {
                var sign = Vector256.Create(0f, -0f, 0f, -0f, 0f, -0f, 0f, -0f);
                for (int offset = SSLIMIT; offset < SSLIMIT * SBLIMIT; offset += SSLIMIT * 2)
                {
                    Simd.Store(Simd.Load256(samples, offset) ^ sign, samples, offset);
                    Simd.Store(Simd.Load256(samples, offset + 8) ^ sign, samples, offset + 8);
                    samples[offset + 17] = -samples[offset + 17];
                }
            }
            else
            {
                var sign = Vector128.Create(0f, -0f, 0f, -0f);
                for (int offset = SSLIMIT; offset < SSLIMIT * SBLIMIT; offset += SSLIMIT * 2)
                {
                    for (int i = 0; i < 16; i += 4)
                        Simd.Store(Simd.Load128(samples, offset + i) ^ sign, samples, offset + i);
                    samples[offset + 17] = -samples[offset + 17];
                }
            }
        }

        partial class HybridMDCT
        {
            static void WindowShortSimd(ReadOnlySpan<float> input, ReadOnlySpan<float> window, Span<float> output)
            {
                // Rearrange the six transform values before applying the window;
                // negation must happen before multiplication, as in the scalar path.
                var sign = Vector128.Create(-0f);
                var first = Vector128.Create(input[3], input[4], input[5], input[5])
                    ^ Vector128.Create(0f, 0f, 0f, -0f);
                var middle = Vector128.Create(input[4], input[3], input[2], input[1]) ^ sign;
                var last = Vector128.Create(input[0], input[0], input[1], input[2]) ^ sign;
                if (Vector256.IsHardwareAccelerated)
                    Simd.Store(Vector256.Create(first, middle) * Simd.Load256(window, 0), output, 0);
                else
                {
                    Simd.Store(first * Simd.Load128(window, 0), output, 0);
                    Simd.Store(middle * Simd.Load128(window, 4), output, 4);
                }
                Simd.Store(last * Simd.Load128(window, 8), output, 8);
            }

            static class LongCoefficients
            {
                internal static readonly float[] Odd = Select(9, 1, 4);
                internal static readonly float[] Output = Select(18, 0, 2);

                static float[] Select(int count, int start, int stride)
                {
                    var result = new float[count];
                    for (int i = 0; i < count; i++) result[i] = icos72_table[start + i * stride];
                    return result;
                }
            }

            static void AdjacentSumsSimd(ReadOnlySpan<float> input, Span<float> sums)
            {
                if (Vector256.IsHardwareAccelerated)
                {
                    Simd.Store(Simd.Load256(input, 0) + Simd.Load256(input, 1), sums, 0);
                    Simd.Store(Simd.Load256(input, 8) + Simd.Load256(input, 9), sums, 8);
                }
                else
                {
                    for (int i = 0; i < 16; i += 4)
                        Simd.Store(Simd.Load128(input, i) + Simd.Load128(input, i + 1), sums, i);
                }
                sums[16] = input[16] + input[17];
            }

            static void ScaleLongSimd(ReadOnlySpan<float> even, Span<float> odd, Span<float> output)
            {
                if (Vector256.IsHardwareAccelerated)
                {
                    var scaled = Simd.Load256(odd, 0) * Simd.Load256(LongCoefficients.Odd, 0);
                    Simd.Store(scaled, odd, 0);
                    Simd.Store((Simd.Load256(even, 0) + scaled) * Simd.Load256(LongCoefficients.Output, 0), output, 0);
                }
                else
                {
                    for (int i = 0; i < 8; i += 4)
                    {
                        var scaled = Simd.Load128(odd, i) * Simd.Load128(LongCoefficients.Odd, i);
                        Simd.Store(scaled, odd, i);
                        Simd.Store((Simd.Load128(even, i) + scaled) * Simd.Load128(LongCoefficients.Output, i), output, i);
                    }
                }
                odd[8] *= ICOS36_A(8);
                output[8] = (even[8] + odd[8]) * ICOS72_A(8);

                if (Vector256.IsHardwareAccelerated)
                {
                    var difference = Simd.Reverse(Simd.Load256(even, 1)) - Simd.Reverse(Simd.Load256(odd, 1));
                    Simd.Store(difference * Simd.Load256(LongCoefficients.Output, 9), output, 9);
                }
                else
                {
                    for (int i = 9; i < 17; i += 4)
                    {
                        var difference = Simd.Reverse(Simd.Load128(even, 14 - i)) - Simd.Reverse(Simd.Load128(odd, 14 - i));
                        Simd.Store(difference * Simd.Load128(LongCoefficients.Output, i), output, i);
                    }
                }
                output[17] = (even[0] - odd[0]) * ICOS72_A(17);
            }
        }
    }
}
#endif
