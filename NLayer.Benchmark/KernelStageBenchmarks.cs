using System.Reflection;
using System.Runtime.Loader;
using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

// Optional comparisons against a scalar or previous implementation. An absolute
// path survives BenchmarkDotNet's change to its generated executable directory.
public abstract class KernelComparison
{
    private AssemblyLoadContext? _context;

    [ParamsSource(nameof(Implementations))]
    public string Implementation { get; set; } = "Simd";

    public IEnumerable<string> Implementations =>
        Environment.GetEnvironmentVariable("NLAYER_REFERENCE_ASSEMBLY") != null
            ? ["Reference", "Simd"]
            : Environment.GetEnvironmentVariable("NLAYER_SCALAR_ASSEMBLY") == null
                ? ["Simd"] : ["Scalar", "Simd"];

    protected object Create(string name, params object[] arguments)
    {
        var assembly = typeof(MpegFile).Assembly;
        if (Implementation == "Scalar" || Implementation == "Reference")
        {
            _context = new AssemblyLoadContext(null, isCollectible: true);
            string variable = Implementation == "Reference" ? "NLAYER_REFERENCE_ASSEMBLY" : "NLAYER_SCALAR_ASSEMBLY";
            assembly = _context.LoadFromAssemblyPath(Environment.GetEnvironmentVariable(variable)!);
        }
        var type = assembly.GetType(name, throwOnError: true)!;
        return arguments.Length == 0
            ? Activator.CreateInstance(type, nonPublic: true)!
            : Activator.CreateInstance(type, arguments)!;
    }

    protected static T Bind<T>(object instance, string name) where T : Delegate
    {
        for (Type? type = instance.GetType(); type != null; type = type.BaseType)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null) return method.CreateDelegate<T>(instance);
        }
        throw new MissingMethodException(instance.GetType().FullName, name);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        DisposeResources();
        _context?.Unload();
    }

    protected virtual void DisposeResources() { }
}

[MemoryDiagnoser]
public class SynthesisStageBenchmarks : KernelComparison
{
    private Action<float[], float[], int> _build = null!, _dct32 = null!;
    private Action<float[], float[]> _window = null!, _dct16 = null!;
    private Action<int, float[]> _synthesis = null!;
    private float[] _input = null!;
    private float[] _history = null!;
    private readonly float[] _u = new float[512];
    private readonly float[] _output = new float[1024];
    private readonly float[] _work = new float[576];
    private int _offset;

    [GlobalSetup]
    public void Setup()
    {
        var instance = Create("NLayer.Decoder.LayerIIIDecoder");
        _build = Bind<Action<float[], float[], int>>(instance, "BuildUVec");
        _dct32 = Bind<Action<float[], float[], int>>(instance, "DCT32");
        _dct16 = Bind<Action<float[], float[]>>(instance, "DCT16");
        _window = Bind<Action<float[], float[]>>(instance, "DewindowOutput");
        _synthesis = Bind<Action<int, float[]>>(instance, "InversePolyPhase");
        _input = DecoderAccess.Input();
        _history = _input.Concat(_input).ToArray();
        Bind<Action<float[]>>(instance, "SetEQ")(Enumerable.Repeat(0.75f, 32).ToArray());
        EqualizedSynthesis();
    }

    [Benchmark]
    public float BuildUVec()
    {
        _offset = (_offset + 32) & 511;
        _build(_u, _history, _offset);
        return _u[0];
    }

    [Benchmark]
    public float DewindowOutput()
    {
        // Scalar windowing overwrites its scratch input; restore it every call.
        Array.Copy(_input, _work, 512);
        _window(_work, _output);
        return _output[0];
    }

    [Benchmark]
    public float Dct32()
    {
        _dct32(_input, _output, 0);
        return _output[0];
    }

    [Benchmark]
    public float Dct16()
    {
        _dct16(_input, _output);
        return _output[0];
    }

    [Benchmark]
    public float EqualizedSynthesis()
    {
        Array.Copy(_input, _work, 32);
        _synthesis(0, _work);
        return _work[0];
    }
}

[MemoryDiagnoser]
public class HybridStageBenchmarks : KernelComparison
{
    private Action<float[], float[]> _long = null!;
    private Action<float[], int, float[]> _short = null!;
    private Action<float[], int, int, float[], int> _longBlocks = null!;
    private Action<float[], int, float[]> _shortBlocks = null!;
    private float[] _input = null!;
    private readonly float[] _work = new float[576];
    private readonly float[] _next = new float[576];
    private readonly float[] _output = new float[36];

    [GlobalSetup]
    public void Setup()
    {
        var instance = Create("NLayer.Decoder.LayerIIIDecoder+HybridMDCT");
        _long = Bind<Action<float[], float[]>>(instance, "LongIMDCT");
        _short = Bind<Action<float[], int, float[]>>(instance, "ShortIMDCT");
        _longBlocks = Bind<Action<float[], int, int, float[], int>>(instance, "LongImpl");
        _shortBlocks = Bind<Action<float[], int, float[]>>(instance, "ShortImpl");
        _input = DecoderAccess.Input();
    }

    [Benchmark]
    public float LongIMDCT()
    {
        _long(_input, _output);
        return _output[0];
    }

    [Benchmark]
    public float ShortIMDCT()
    {
        _short(_input, 6, _output);
        return _output[0];
    }

    [Benchmark]
    public float LongBlocks()
    {
        _input.CopyTo(_work, 0);
        _longBlocks(_work, 0, 32, _next, 0);
        return _work[0];
    }

    [Benchmark]
    public float ShortBlocks()
    {
        _input.CopyTo(_work, 0);
        _shortBlocks(_work, 0, _next);
        return _work[0];
    }
}
