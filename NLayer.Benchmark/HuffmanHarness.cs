using System.Linq.Expressions;
using System.Reflection;

namespace NLayer.Benchmark;

internal delegate void DecodePair(int table, out float x, out float y);
internal delegate void DecodeQuad(int table, out float x, out float y, out float v, out float w);

// Reflection/expression compilation happens only during setup. Timed decode and
// reset calls use delegates and preallocated reservoir storage, without boxing.
internal sealed class HuffmanHarness
{
    private readonly byte[] _buffer;
    private readonly Action<int, int, int, long> _setState;
    private readonly Func<long> _bitsRead;
    private readonly Func<int> _available;
    private int _start, _end, _left;
    internal readonly Func<int, byte> Symbol;
    internal readonly DecodePair Pair;
    internal readonly DecodeQuad Quad;

    internal HuffmanHarness(object reservoir)
    {
        var type = reservoir.GetType();
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
        _buffer = (byte[])type.GetField("_buf", fields)!.GetValue(reservoir)!;
        var target = Expression.Constant(reservoir, type);
        var start = Expression.Parameter(typeof(int)); var end = Expression.Parameter(typeof(int));
        var left = Expression.Parameter(typeof(int)); var read = Expression.Parameter(typeof(long));
        _setState = Expression.Lambda<Action<int, int, int, long>>(Expression.Block(
            Expression.Assign(Expression.Field(target, type.GetField("_start", fields)!), start),
            Expression.Assign(Expression.Field(target, type.GetField("_end", fields)!), end),
            Expression.Assign(Expression.Field(target, type.GetField("_bitsLeft", fields)!), left),
            Expression.Assign(Expression.Field(target, type.GetField("_bitsRead", fields)!), read),
            Expression.Empty()), start, end, left, read).Compile();
        _bitsRead = type.GetProperty("BitsRead")!.GetMethod!.CreateDelegate<Func<long>>(reservoir);
        _available = type.GetProperty("BitsAvailable")!.GetMethod!.CreateDelegate<Func<int>>(reservoir);
        var huffman = type.Assembly.GetType("NLayer.Decoder.Huffman", true)!;
        Symbol = Bind<Func<int, byte>>("DecodeSymbol", 0);
        Pair = Bind<DecodePair>("Decode", 2);
        Quad = Bind<DecodeQuad>("Decode", 4);

        T Bind<T>(string name, int outputs) where T : Delegate
        {
            var table = Expression.Parameter(typeof(int));
            var floats = Enumerable.Range(0, outputs).Select(_ => Expression.Parameter(typeof(float).MakeByRefType())).ToArray();
            var types = new[] { type, typeof(int) }.Concat(floats.Select(p => p.Type.IsByRef ? p.Type : p.Type.MakeByRefType())).ToArray();
            var method = huffman.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic, types)!;
            Expression[] arguments = [target, table, .. floats];
            return Expression.Lambda<T>(Expression.Call(method, arguments), [table, .. floats]).Compile();
        }
    }

    internal long BitsRead => _bitsRead();
    internal int Available => _available();

    internal void Load(byte[] data, int bitOffset = 0, int start = 0)
    {
        if (data.Length >= _buffer.Length) throw new ArgumentOutOfRangeException(nameof(data));
        for (int i = 0; i < data.Length; i++) _buffer[(start + i) % _buffer.Length] = data[i];
        _start = start;
        _end = (start + data.Length - 1 + _buffer.Length) % _buffer.Length;
        if (_end == _buffer.Length - 1) _end = -1; // AddBits' canonical end marker
        _left = data.Length == 0 ? 0 : 8 - bitOffset;
        Reset();
    }

    internal void Reset() => _setState(_start, _end, _left, 0);
}
