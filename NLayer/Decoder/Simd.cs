#if NET10_0_OR_GREATER && !NLAYER_DISABLE_SIMD
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace NLayer.Decoder
{
    // All callers operate on decoder-owned buffers and pass complete vector ranges.
    // Unaligned loads are intentional: subbands start at multiples of 18 floats.
    static class Simd
    {
        internal static bool IsSupported => Vector128.IsHardwareAccelerated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<float> Load256(ReadOnlySpan<float> data, int offset)
            => Vector256.LoadUnsafe(ref MemoryMarshal.GetReference(data), (nuint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<float> Load128(ReadOnlySpan<float> data, int offset)
            => Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(data), (nuint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Store(Vector256<float> value, Span<float> data, int offset)
            => value.StoreUnsafe(ref MemoryMarshal.GetReference(data), (nuint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Store(Vector128<float> value, Span<float> data, int offset)
            => value.StoreUnsafe(ref MemoryMarshal.GetReference(data), (nuint)offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<float> Reverse(Vector256<float> value)
            => Vector256.Shuffle(value, Vector256.Create(7, 6, 5, 4, 3, 2, 1, 0));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<float> Reverse(Vector128<float> value)
            => Vector128.Shuffle(value, Vector128.Create(3, 2, 1, 0));

        internal static void Multiply(ReadOnlySpan<float> left, ReadOnlySpan<float> right, Span<float> output)
        {
            // Check input lengths once before using unchecked vector loads.
            // Exact in-place aliasing is supported (e.g. equalization).
            left = left[..output.Length];
            right = right[..output.Length];
            int i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= output.Length - 8; i += 8)
                    Store(Load256(left, i) * Load256(right, i), output, i);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= output.Length - 4; i += 4)
                    Store(Load128(left, i) * Load128(right, i), output, i);
            }
            for (; i < output.Length; i++)
                output[i] = left[i] * right[i];
        }

        internal static void Add(ReadOnlySpan<float> source, Span<float> destination)
        {
            source = source[..destination.Length];
            int i = 0;
            if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= destination.Length - 8; i += 8)
                    Store(Load256(destination, i) + Load256(source, i), destination, i);
            }
            // Also handles the first four of a six-sample short-block overlap.
            if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= destination.Length - 4; i += 4)
                    Store(Load128(destination, i) + Load128(source, i), destination, i);
            }
            for (; i < destination.Length; i++)
                destination[i] += source[i];
        }
    }
}
#endif
