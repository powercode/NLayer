#if NET10_0_OR_GREATER && !NLAYER_DISABLE_SIMD
using System;
using System.Runtime.Intrinsics;

namespace NLayer.Decoder
{
    sealed partial class LayerIIIDecoder
    {
        partial class HybridMDCT
        {
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
