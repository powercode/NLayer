using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

[MemoryDiagnoser]
public class DequantizationBenchmarks : KernelComparison
{
    private delegate void Batch(Span<float> samples, int granule, int channel);
    private Action<float[]> _apply = null!;
    private readonly float[] _input = DecoderAccess.Input();
    private readonly float[] _work = new float[576];

    [Params("Long", "Short", "Mixed")]
    public string Block { get; set; } = "Long";

    [GlobalSetup]
    public void Setup()
    {
        var decoder = Create("NLayer.Decoder.LayerIIIDecoder");
        var type = decoder.GetType();
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)type.GetField(name, fields)!.GetValue(decoder)!;
        var l = ((int[][])type.GetField("_sfBandIndexLTable", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!)[0];
        var s = ((int[][])type.GetField("_sfBandIndexSTable", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!)[0];
        type.GetField("_sfBandIndexL", fields)!.SetValue(decoder, l);
        type.GetField("_sfBandIndexS", fields)!.SetValue(decoder, s);
        int cbL = 0, cbS = 0;
        for (int i = 0; i < 576; i++)
        {
            if (i == l[cbL + 1]) cbL++;
            if (i == s[cbS + 1] * 3) cbS++;
            Field<byte[]>("_cbLookupL")[i] = (byte)cbL;
            Field<byte[]>("_cbLookupS")[i] = (byte)cbS;
        }
        int offset = 0;
        for (int band = 0; band < 12; band++)
        for (int window = 0; window < 3; window++)
        for (int i = s[band]; i < s[band + 1]; i++) Field<byte[]>("_cbwLookupS")[offset++] = (byte)window;
        Field<bool[][]>("_blockSplitFlag")[0][0] = Block != "Long";
        Field<int[][]>("_blockType")[0][0] = 2;
        Field<bool[][]>("_mixedBlockFlag")[0][0] = Block == "Mixed";
        Field<float[][]>("_globalGain")[0][0] = 0.125f;
        Field<float[][]>("_scalefacScale")[0][0] = 0.5f;
        Field<int[][]>("_preflag")[0][0] = 1;
        for (int window = 0; window < 3; window++) Field<float[][][]>("_subblockGain")[0][0][window] = -2f * window;
        foreach (var factors in Field<int[][][]>("_scalefac")[0])
            for (int i = 0; i < factors.Length; i++) factors[i] = i % 8;

        if (Implementation == "Simd")
        {
            var batch = Bind<Batch>(decoder, "DequantizeSamples");
            _apply = values => batch(values, 0, 0);
        }
        else
        {
            // Compile the old per-coefficient loop, avoiding delegate dispatch
            // for every value. Both versions have one delegate call per granule.
            var values = Expression.Parameter(typeof(float[]));
            var index = Expression.Variable(typeof(int));
            var done = Expression.Label();
            var value = Expression.ArrayAccess(values, index);
            var call = Expression.Call(Expression.Constant(decoder), type.GetMethod("Dequantize", fields)!,
                index, value, Expression.Constant(0), Expression.Constant(0));
            _apply = Expression.Lambda<Action<float[]>>(Expression.Block([index],
                Expression.Assign(index, Expression.Constant(0)),
                Expression.Loop(Expression.IfThenElse(Expression.LessThan(index, Expression.Constant(576)),
                    Expression.Block(Expression.Assign(value, call), Expression.PostIncrementAssign(index), Expression.Empty()),
                    Expression.Break(done)), done)), values).Compile();
        }
    }

    [Benchmark]
    public float Apply()
    {
        _input.CopyTo(_work, 0);
        _apply(_work);
        return _work[575];
    }
}
