using System.Reflection;
using System.Runtime.Intrinsics;
using System.Runtime.Loader;

namespace NLayer.Benchmark;

// Run against a separately built scalar .NET 10 assembly. Both assemblies are
// loaded into isolated contexts but share this process's runtime and float mode.
internal static class SimdVerification
{
    private delegate int ReadSpan(Span<float> samples);
    private const BindingFlags Methods = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static long _comparisons;

    public static void Run(string scalarPath)
    {
        using var scalar = new DecoderAssembly(Path.GetFullPath(scalarPath));
        using var vector = new DecoderAssembly(typeof(MpegFile).Assembly.Location);
        scalar.AssertScalar();
        Console.WriteLine($"SIMD: Vector256={Vector256.IsHardwareAccelerated}, Vector128={Vector128.IsHardwareAccelerated}");
        VerifySynthesis(scalar, vector);
        VerifyHybrid(scalar, vector);
        VerifyLayerIII(scalar, vector);
        VerifyFiles(scalar, vector);
        Console.WriteLine($"PASS: {_comparisons:N0} sample bit patterns match; kernel allocations match zero.");
    }

    private static void VerifySynthesis(DecoderAssembly scalar, DecoderAssembly vector)
    {
        var a = scalar.Create("NLayer.Decoder.LayerIIIDecoder");
        var b = vector.Create("NLayer.Decoder.LayerIIIDecoder");
        var synthA = Bind<Action<int, float[]>>(a, "InversePolyPhase");
        var synthB = Bind<Action<int, float[]>>(b, "InversePolyPhase");
        var resetA = Bind<Action>(a, "ResetForSeek");
        var resetB = Bind<Action>(b, "ResetForSeek");
        var eqA = Bind<Action<float[]?>>(a, "SetEQ");
        var eqB = Bind<Action<float[]?>>(b, "SetEQ");
        var buildA = Bind<Action<float[], float[], int>>(a, "BuildUVec");
        var buildB = Bind<Action<float[], float[], int>>(b, "BuildUVec");
        var dctA = Bind<Action<float[], float[], int>>(a, "DCT32");
        var dctB = Bind<Action<float[], float[], int>>(b, "DCT32");
        var windowA = Bind<Action<float[], float[]>>(a, "DewindowOutput");
        var windowB = Bind<Action<float[], float[]>>(b, "DewindowOutput");
        var random = new Random(381);

        for (int pattern = 0; pattern < 10; pattern++)
        {
            var history = Input(1024, pattern, random);
            for (int k = 0; k < 512; k += 32)
            {
                var uA = new float[512];
                var uB = new float[512];
                buildA(uA, history, k); buildB(uB, history, k);
                Equal(uA, uB, $"BuildUVec pattern={pattern} k={k}");
                var pcmA = new float[32]; var pcmB = new float[32];
                windowA(uA, pcmA); windowB(uB, pcmB);
                Equal(pcmA, pcmB, $"DewindowOutput pattern={pattern} k={k}");
                var dctInput = Input(32, pattern, random);
                var dctOutA = new float[1024]; var dctOutB = new float[1024];
                dctA(dctInput, dctOutA, k); dctB(dctInput, dctOutB, k);
                Equal(dctOutA, dctOutB, $"DCT32 pattern={pattern} k={k}");
            }
        }

        // Reset/replay repeats the same stream on both channels; 96 calls per
        // configuration cross every history offset several times.
        for (int equalizer = 0; equalizer < 3; equalizer++)
        {
            float[]? eq = equalizer == 0 ? null : Enumerable.Range(0, 32)
                .Select(i => equalizer == 1 ? 1f : (i - 16) * 0.125f).ToArray();
            eqA(eq); eqB(eq);
            for (int replay = 0; replay < 2; replay++)
            {
                resetA(); resetB();
                random = new Random(123);
                for (int call = 0; call < 96; call++)
                {
                    var input = Input(32, call % 10, random);
                    var expected = (float[])input.Clone(); var actual = (float[])input.Clone();
                    synthA(call & 1, expected); synthB(call & 1, actual);
                    Equal(expected, actual, $"Synthesis eq={equalizer} replay={replay} call={call}");
                }
            }
        }
        // Explicit impulse in each subband, preserving history across impulses.
        for (int band = 0; band < 32; band++)
        {
            var expected = new float[32]; var actual = new float[32];
            expected[band] = actual[band] = 1;
            synthA(0, expected); synthB(0, actual);
            Equal(expected, actual, $"Synthesis impulse={band}");
        }
        var work = new float[32];
        AssertNoAllocation(() => synthA(0, work), "scalar synthesis");
        AssertNoAllocation(() => synthB(0, work), "SIMD synthesis");
    }

    private static void VerifyHybrid(DecoderAssembly scalar, DecoderAssembly vector)
    {
        var a = scalar.Create("NLayer.Decoder.LayerIIIDecoder+HybridMDCT");
        var b = vector.Create("NLayer.Decoder.LayerIIIDecoder+HybridMDCT");
        var applyA = Bind<Action<float[], int, int, bool>>(a, "Apply");
        var applyB = Bind<Action<float[], int, int, bool>>(b, "Apply");
        var resetA = Bind<Action>(a, "Reset"); var resetB = Bind<Action>(b, "Reset");
        var longA = Bind<Action<float[], float[]>>(a, "LongIMDCT");
        var longB = Bind<Action<float[], float[]>>(b, "LongIMDCT");
        var shortA = Bind<Action<float[], int, float[]>>(a, "ShortIMDCT");
        var shortB = Bind<Action<float[], int, float[]>>(b, "ShortIMDCT");
        var random = new Random(234);
        for (int call = 0; call < 128; call++)
        {
            var input = Input(18, call % 10, random);
            var expected = new float[36]; var actual = new float[36];
            longA(input, expected); longB(input, actual);
            Equal(expected, actual, $"LongIMDCT call={call}");
            foreach (int offset in new[] { 0, 6, 12 })
            {
                var shortExpected = Input(36, call % 10, random);
                var shortActual = (float[])shortExpected.Clone();
                shortA(input, offset, shortExpected); shortB(input, offset, shortActual);
                Equal(shortExpected, shortActual, $"ShortIMDCT call={call} offset={offset}");
            }
        }

        // Legal long -> start -> short/mixed -> stop -> long transitions.
        int[] blocks = [0, 0, 1, 2, 2, 3, 0, 1, 2, 2, 3, 0];
        for (int replay = 0; replay < 2; replay++)
        {
            resetA(); resetB();
            random = new Random(456);
            for (int call = 0; call < 384; call++)
            {
                int block = blocks[(call / 2) % blocks.Length];
                bool mixed = block == 2 && (call / 2) % blocks.Length >= 8;
                var expected = Input(580, (call / 24) % 10, random);
                // Sentinel samples beyond 576 catch over-wide stores at the tail.
                var actual = (float[])expected.Clone();
                applyA(expected, call & 1, block, mixed);
                applyB(actual, call & 1, block, mixed);
                Equal(expected, actual, $"Hybrid block={block} mixed={mixed} call={call}");
            }
        }
        for (int band = 0; band < 32; band++)
        {
            foreach (int block in new[] { 0, 1, 2, 3 })
            {
                var expected = new float[576]; var actual = new float[576];
                expected[band * 18 + band % 18] = actual[band * 18 + band % 18] = 1;
                applyA(expected, 0, block, false); applyB(actual, 0, block, false);
                Equal(expected, actual, $"Hybrid impulse={band} block={block}");
            }
        }
        var work = new float[576];
        AssertNoAllocation(() => applyA(work, 0, 0, false), "scalar hybrid");
        AssertNoAllocation(() => applyB(work, 0, 0, false), "SIMD hybrid");
        AssertNoAllocation(() => shortB(work, 6, work), "SIMD short transform");
    }

    private static void VerifyLayerIII(DecoderAssembly scalar, DecoderAssembly vector)
    {
        var a = scalar.Create("NLayer.Decoder.LayerIIIDecoder");
        var b = vector.Create("NLayer.Decoder.LayerIIIDecoder");
        var antiA = Bind<Action<float[], bool>>(a, "AntiAlias");
        var antiB = Bind<Action<float[], bool>>(b, "AntiAlias");
        var invertA = Bind<Action<float[]>>(a, "FrequencyInversion");
        var invertB = Bind<Action<float[]>>(b, "FrequencyInversion");
        var random = new Random(891);
        // Patterns plus an impulse at every spectral line. Extra elements are
        // sentinels to detect stores past the granule, including the final band.
        for (int pattern = 0; pattern < 10 + 576; pattern++)
        {
            var input = Input(580, pattern < 10 ? pattern : 0, random);
            if (pattern >= 10) input[pattern - 10] = 1;
            input[576] = 123; input[577] = -456; input[578] = -0f; input[579] = float.Epsilon;
            foreach (bool mixed in new[] { false, true })
            {
                var expected = (float[])input.Clone(); var actual = (float[])input.Clone();
                antiA(expected, mixed); antiB(actual, mixed);
                Equal(expected, actual, $"AntiAlias pattern={pattern} mixed={mixed}");
                // Also exercise the actual anti-alias -> inversion ordering.
                invertA(expected); invertB(actual);
                Equal(expected, actual, $"AntiAlias/inversion pattern={pattern} mixed={mixed}");
            }
            var inverted = (float[])input.Clone();
            var reference = (float[])input.Clone();
            invertA(reference); invertB(inverted);
            Equal(reference, inverted, $"FrequencyInversion pattern={pattern}");
            invertB(inverted);
            Equal(input, inverted, $"Double inversion pattern={pattern}");
        }
        var work = new float[576];
        AssertNoAllocation(() => antiB(work, false), "SIMD anti-alias long");
        AssertNoAllocation(() => antiB(work, true), "SIMD anti-alias mixed");
        AssertNoAllocation(() => invertB(work), "SIMD frequency inversion");
    }

    private static void VerifyFiles(DecoderAssembly scalar, DecoderAssembly vector)
    {
        var mp3 = GeneratedMp3.Create(40);
        foreach (StereoMode mode in Enum.GetValues<StereoMode>())
        foreach (int size in new[] { 1, 31, 2304, 4097, 16384 })
        foreach (bool span in new[] { false, true })
        {
            using var streamA = new MemoryStream(mp3);
            using var streamB = new MemoryStream(mp3);
            using var a = (IDisposable)scalar.CreateFile(streamA, (int)mode);
            using var b = (IDisposable)vector.CreateFile(streamB, (int)mode);
            var readA = Bind<Func<float[], int, int, int>>(a, "ReadSamples", typeof(float[]), typeof(int), typeof(int));
            var readB = Bind<Func<float[], int, int, int>>(b, "ReadSamples", typeof(float[]), typeof(int), typeof(int));
            var spanA = Bind<ReadSpan>(a, "ReadSamples", typeof(Span<float>));
            var spanB = Bind<ReadSpan>(b, "ReadSamples", typeof(Span<float>));
            var expected = new float[size + 2]; var actual = new float[size + 2];
            long total = 0;
            for (int replay = 0; replay < 2; replay++)
            {
                if (replay != 0)
                {
                    a.GetType().GetProperty("Position")!.SetValue(a, 0L);
                    b.GetType().GetProperty("Position")!.SetValue(b, 0L);
                }
                int count;
                do
                {
                    count = span ? spanA(expected.AsSpan(1, size)) : readA(expected, 1, size);
                    int actualCount = span ? spanB(actual.AsSpan(1, size)) : readB(actual, 1, size);
                    if (count != actualCount) throw new InvalidOperationException("Decoded sample counts differ.");
                    Equal(expected, actual, $"File mode={mode} size={size} span={span} replay={replay}");
                    total += count;
                } while (count != 0);
            }
            if (total == 0) throw new InvalidOperationException("Empty decoded fixture.");
        }
    }

    private static float[] Input(int count, int pattern, Random random)
    {
        var result = new float[count];
        for (int i = 0; i < count; i++)
            result[i] = pattern switch
            {
                0 => 0f,
                1 => (i & 1) == 0 ? -0f : 0f,
                2 => (i & 1) == 0 ? 1f : -1f,
                3 => (random.NextSingle() - 0.5f) * 1e-12f,
                4 => (random.NextSingle() - 0.5f) * 1e12f,
                5 => (random.NextSingle() - 0.5f) * 0.1f,
                6 => i % 18 == 0 ? 1f : 0f,
                7 => i % 18 == 17 ? -1f : 0f,
                8 => (random.NextSingle() - 0.5f) * 1e-38f,
                _ => (i & 1) == 0 ? float.Epsilon : -float.Epsilon
            };
        return result;
    }

    private static void Equal(float[] expected, float[] actual, string context)
    {
        for (int i = 0; i < expected.Length; i++)
        {
            int a = BitConverter.SingleToInt32Bits(expected[i]);
            int b = BitConverter.SingleToInt32Bits(actual[i]);
            if (a != b) throw new InvalidOperationException($"{context}, index {i}: {a:X8} != {b:X8} ({expected[i]} != {actual[i]}).");
        }
        _comparisons += expected.Length;
    }

    private static void AssertNoAllocation(Action action, string context)
    {
        for (int i = 0; i < 100; i++) action();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) action();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        if (bytes != 0) throw new InvalidOperationException($"{context} allocated {bytes} bytes.");
    }

    private static T Bind<T>(object target, string name, params Type[] parameters) where T : Delegate
    {
        for (Type? type = target.GetType(); type != null; type = type.BaseType)
        {
            var method = parameters.Length == 0
                ? type.GetMethod(name, Methods | BindingFlags.DeclaredOnly)
                : type.GetMethod(name, Methods | BindingFlags.DeclaredOnly, parameters);
            if (method != null) return method.CreateDelegate<T>(target);
        }
        throw new MissingMethodException(target.GetType().FullName, name);
    }

    private sealed class DecoderAssembly : IDisposable
    {
        private readonly AssemblyLoadContext _context = new(null, isCollectible: true);
        private readonly Assembly _assembly;
        public DecoderAssembly(string path) => _assembly = _context.LoadFromAssemblyPath(path);
        public void AssertScalar()
        {
            if (_assembly.GetType("NLayer.Decoder.Simd") != null)
                throw new ArgumentException("The reference assembly must be built with -p:NLayerDisableSimd=true.");
        }
        public object Create(string name) => Activator.CreateInstance(_assembly.GetType(name, true)!, nonPublic: true)!;
        public object CreateFile(Stream stream, int mode)
        {
            var type = _assembly.GetType("NLayer.MpegFile", true)!;
            var file = Activator.CreateInstance(type, stream)!;
            var property = type.GetProperty("StereoMode")!;
            property.SetValue(file, Enum.ToObject(property.PropertyType, mode));
            return file;
        }
        public void Dispose() => _context.Unload();
    }
}
