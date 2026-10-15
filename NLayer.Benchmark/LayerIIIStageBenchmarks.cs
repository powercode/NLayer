using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

[MemoryDiagnoser]
public class LayerIIIStageBenchmarks : KernelComparison
{
    private Action<float[], bool> _antiAlias = null!;
    private Action<float[]> _inversion = null!;
    private float[] _input = null!;
    private readonly float[] _work = new float[576];

    [GlobalSetup]
    public void Setup()
    {
        var instance = Create("NLayer.Decoder.LayerIIIDecoder");
        _antiAlias = Bind<Action<float[], bool>>(instance, "AntiAlias");
        _inversion = Bind<Action<float[]>>(instance, "FrequencyInversion");
        _input = DecoderAccess.Input();
    }

    // Restoration is included equally for reference and SIMD implementations.
    [Benchmark]
    public float AntiAliasLong()
    {
        _input.CopyTo(_work, 0);
        _antiAlias(_work, false);
        return _work[17];
    }

    [Benchmark]
    public float AntiAliasMixed()
    {
        _input.CopyTo(_work, 0);
        _antiAlias(_work, true);
        return _work[17];
    }

    [Benchmark]
    public float FrequencyInversion()
    {
        _input.CopyTo(_work, 0);
        _inversion(_work);
        return _work[19];
    }
}
