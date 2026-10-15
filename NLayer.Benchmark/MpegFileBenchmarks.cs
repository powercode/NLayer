using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

[MemoryDiagnoser]
public class MpegFileBenchmarks
{
    private byte[] _mp3 = null!;
    private float[] _buffer = null!;
    private long _expectedSamples;

    [Params(2304, 16384)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _mp3 = GeneratedMp3.Create(400); // approximately 10.45 seconds
        _buffer = new float[BufferSize];
        using var file = new MpegFile(new MemoryStream(_mp3, writable: false));
        float peak = 0;
        int read;
        while ((read = file.ReadSamples(_buffer, 0, _buffer.Length)) > 0)
        {
            _expectedSamples += read;
            for (int i = 0; i < read; i++)
            {
                if (!float.IsFinite(_buffer[i])) throw new InvalidOperationException("Non-finite fixture output.");
                peak = Math.Max(peak, Math.Abs(_buffer[i]));
            }
        }
        if (_expectedSamples != 400L * 1152 * 2 || peak == 0)
            throw new InvalidOperationException($"Invalid generated MP3: {_expectedSamples} samples, peak {peak}.");
        Console.WriteLine($"Generated MP3: {_mp3.Length} bytes, {_expectedSamples} samples, peak {peak}.");
    }

    [Benchmark(Baseline = true)]
    public long ReadSamplesArray()
    {
        using var file = new MpegFile(new MemoryStream(_mp3, writable: false));
        long total = 0;
        int read;
        while ((read = file.ReadSamples(_buffer, 0, _buffer.Length)) > 0) total += read;
        if (total != _expectedSamples) throw new InvalidOperationException("Unexpected decoded length.");
        return total;
    }

    [Benchmark]
    public long ReadSamplesSpan()
    {
        using var file = new MpegFile(new MemoryStream(_mp3, writable: false));
        long total = 0;
        int read;
        while ((read = file.ReadSamples(_buffer.AsSpan())) > 0) total += read;
        if (total != _expectedSamples) throw new InvalidOperationException("Unexpected decoded length.");
        return total;
    }
}
