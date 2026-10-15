#if NET10_0_OR_GREATER && !NLAYER_DISABLE_SIMD
using System;
using System.Runtime.Intrinsics;

namespace NLayer.Decoder
{
    sealed partial class LayerIIIDecoder
    {
        void DequantizeSamples(Span<float> samples, int gr, int ch)
        {
            bool shortBlock = _blockSplitFlag[gr][ch] && _blockType[gr][ch] == 2;
            int longEnd = shortBlock ? (_mixedBlockFlag[gr][ch] ? _sfBandIndexL[8] : 0) : 576;
            float gain = _globalGain[gr][ch];
            float scale = _scalefacScale[gr][ch];
            int offset = 0;
            while (offset < samples.Length)
            {
                int end;
                int power;
                if (offset < longEnd)
                {
                    int band = _cbLookupL[offset];
                    end = Math.Min(longEnd, _sfBandIndexL[band + 1]);
                    power = (int)(2 * scale * (_scalefac[ch][3][band] + _preflag[gr][ch] * PRETAB[band]));
                }
                else
                {
                    int band = _cbLookupS[offset];
                    int window = _cbwLookupS[offset];
                    if (band == 12)
                    {
                        // This tail is normally window zero, but can retain
                        // other windows after a sample-rate change. Follow the
                        // existing lookup rather than assuming a window here.
                        end = offset + 1;
                        while (end < samples.Length && _cbwLookupS[end] == window) end++;
                    }
                    else
                    {
                        end = _sfBandIndexS[band] * 3
                            + (_sfBandIndexS[band + 1] - _sfBandIndexS[band]) * (window + 1);
                    }
                    power = (int)(-2 * (_subblockGain[gr][ch][window] - scale * _scalefac[ch][window][band]));
                }
                end = Math.Min(end, samples.Length);
                var bandSamples = samples[offset..end];
                // Match the scalar zero fast path, including negative zero.
                // The two multiplications must retain their separate rounding.
                // Delay the table lookup until a nonzero coefficient exists.
                int first = 0;
                while (first < bandSamples.Length && bandSamples[first] == 0f)
                    bandSamples[first++] = 0f;
                if (first < bandSamples.Length)
                    ScaleCoefficients(bandSamples[first..], gain, POW2_TAB[power]);
                offset = end;
            }
        }

        static void ScaleCoefficients(Span<float> samples, float gain, float scale)
        {
            int i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                var g = Vector256.Create(gain);
                var s = Vector256.Create(scale);
                for (; i <= samples.Length - 8; i += 8)
                {
                    var value = Simd.Load256(samples, i);
                    var result = (value * g) * s;
                    Simd.Store(Vector256.ConditionalSelect(Vector256.Equals(value, Vector256<float>.Zero),
                        Vector256<float>.Zero, result), samples, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                var g = Vector128.Create(gain);
                var s = Vector128.Create(scale);
                for (; i <= samples.Length - 4; i += 4)
                {
                    var value = Simd.Load128(samples, i);
                    var result = (value * g) * s;
                    Simd.Store(Vector128.ConditionalSelect(Vector128.Equals(value, Vector128<float>.Zero),
                        Vector128<float>.Zero, result), samples, i);
                }
            }
            for (; i < samples.Length; i++)
                samples[i] = samples[i] == 0f ? 0f : (samples[i] * gain) * scale;
        }
    }
}
#endif
