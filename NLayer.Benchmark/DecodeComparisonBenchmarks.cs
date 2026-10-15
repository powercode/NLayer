using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

// Compare the previous checkpoint and current decoder in the same benchmark
// suite. Setup binds across assembly contexts, avoiding reflection while timing.
[MemoryDiagnoser]
public class DecodeComparisonBenchmarks : KernelComparison
{
    private delegate int ReadSpan(Span<float> samples);
    private IDisposable _file = null!;
    private MemoryStream _stream = null!;
    private Action<long> _seek = null!;
    private Func<float[], int, int, int> _readArray = null!;
    private ReadSpan _readSpan = null!;
    private readonly float[] _output = new float[4096];

    [GlobalSetup]
    public void Setup()
    {
        _stream = new MemoryStream(GeneratedMp3.Create(400), writable: false);
        _file = (IDisposable)Create("NLayer.MpegFile", _stream);
        var type = _file.GetType();
        _seek = type.GetProperty("Position")!.SetMethod!.CreateDelegate<Action<long>>(_file);
        _readArray = type.GetMethod("ReadSamples", [typeof(float[]), typeof(int), typeof(int)])!
            .CreateDelegate<Func<float[], int, int, int>>(_file);
        _readSpan = type.GetMethod("ReadSamples", [typeof(Span<float>)])!.CreateDelegate<ReadSpan>(_file);
    }

    [Benchmark]
    public long ReadSamplesArray()
    {
        _seek(0);
        long total = 0;
        int read;
        while ((read = _readArray(_output, 0, _output.Length)) > 0) total += read;
        if (total != 400L * 1152 * 2) throw new InvalidOperationException("Unexpected decoded length.");
        return total;
    }

    [Benchmark]
    public long ReadSamplesSpan()
    {
        _seek(0);
        long total = 0;
        int read;
        while ((read = _readSpan(_output)) > 0) total += read;
        if (total != 400L * 1152 * 2) throw new InvalidOperationException("Unexpected decoded length.");
        return total;
    }

    protected override void DisposeResources()
    {
        _file.Dispose();
        _stream.Dispose();
    }
}
