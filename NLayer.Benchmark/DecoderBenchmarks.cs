using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

internal static class DecoderAccess
{
    // Bind once, outside timing. No reflection invocation/boxing per operation,
    // and no changes to the decoder's public API or assembly visibility.
    public static T Bind<T>(string typeName, string methodName) where T : Delegate
    {
        var type = typeof(MpegFile).Assembly.GetType(typeName, throwOnError: true)!;
        var instance = Activator.CreateInstance(type, nonPublic: true)!;
        return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<T>(instance);
    }

    public static float[] Input()
    {
        var random = new Random(42);
        return Enumerable.Range(0, 576).Select(_ => (random.NextSingle() - 0.5f) * 0.1f).ToArray();
    }
}

[MemoryDiagnoser]
public class PolyphaseBenchmarks
{
    private Action<float[], int, int, float[]> _synthesize = null!;
    private float[] _input = null!;
    private readonly float[] _output = new float[576];

    [GlobalSetup]
    public void Setup()
    {
        _input = DecoderAccess.Input();
        _synthesize = DecoderAccess.Bind<Action<float[], int, int, float[]>>(
            "NLayer.Decoder.LayerIIIDecoder", "InversePolyphase");
        _synthesize(_input, 0, 0, _output); // allocate/prime synthesis history
    }

    [Benchmark]
    public float InversePolyphase()
    {
        _synthesize(_input, 0, 0, _output);
        return _output[0];
    }
}

public enum HybridBlock { Long, Start, Short, Mixed, Stop }

[MemoryDiagnoser]
public class HybridMdctBenchmarks
{
    private Action<float[], int, int, bool> _apply = null!;
    private float[] _input = null!;
    private readonly float[] _work = new float[576];
    private int _blockType;

    [ParamsAllValues]
    public HybridBlock Block { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _input = DecoderAccess.Input();
        _blockType = Block switch { HybridBlock.Long => 0, HybridBlock.Start => 1, HybridBlock.Stop => 3, _ => 2 };
        _apply = DecoderAccess.Bind<Action<float[], int, int, bool>>(
            "NLayer.Decoder.LayerIIIDecoder+HybridMDCT", "Apply");
        Apply();
        Apply(); // allocate both overlap buffers before measuring
    }

    [Benchmark]
    public float Apply()
    {
        // Apply mutates its input. Restore it on every invocation so repeated
        // transforms cannot amplify samples into infinity/NaN. Copy is timed.
        _input.CopyTo(_work, 0);
        _apply(_work, 0, _blockType, Block == HybridBlock.Mixed);
        return _work[0];
    }
}
