using System.Reflection;

namespace NLayer.Benchmark;

internal readonly record struct HuffmanCode(int Value, int Bits, int Length);

internal static class HuffmanData
{
    private static readonly Dictionary<int, HuffmanCode[]> Books = new();
    internal static readonly int[] LinBits = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 6, 8, 10, 13, 4, 5, 6, 7, 8, 9, 11, 13];

    internal static HuffmanCode[] Codes(int table)
    {
        lock (Books)
        {
            if (Books.TryGetValue(table, out var existing)) return existing;
            int index = table switch
            {
                >= 32 => table - 17,
                >= 24 => 14,
                >= 16 => 13,
                >= 15 => table - 3,
                >= 5 => table - 2,
                _ => table - 1
            };
            var type = typeof(MpegFile).Assembly.GetType("NLayer.Decoder.Huffman", true)!;
            var trees = (byte[][,])type.GetField("_codeTables", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var tree = trees[index];
            var codes = new List<HuffmanCode>();
            Walk(0, 0, 0);
            return Books[table] = codes.OrderBy(c => c.Value).ToArray();

            // Traverse forwards, independently of the decoder's backwards
            // parent search. Offsets >= 250 continue the same branch/bit.
            void Walk(int node, int bits, int length)
            {
                if (tree[node, 0] == 0)
                {
                    codes.Add(new HuffmanCode(tree[node, 1], bits, length));
                    return;
                }
                for (int bit = 0; bit < 2; bit++)
                {
                    int offset = tree[node, bit];
                    int next = node + offset;
                    while (offset >= 250)
                    {
                        offset = tree[next, bit];
                        next += offset;
                    }
                    Walk(next, (bits << 1) | bit, length + 1);
                }
            }
        }
    }

    internal static void WritePair(BitData data, int table, HuffmanCode code, int signs, int extraX = 0, int extraY = 0)
    {
        data.Write(code.Bits, code.Length);
        int x = code.Value >> 4, y = code.Value & 15;
        if (x == 15 && LinBits[table] != 0) data.Write(extraX, LinBits[table]);
        if (x != 0) data.Write(signs & 1, 1);
        if (y == 15 && LinBits[table] != 0) data.Write(extraY, LinBits[table]);
        if (y != 0) data.Write((signs >> 1) & 1, 1);
    }

    internal static void WriteQuad(BitData data, HuffmanCode code, int signs)
    {
        data.Write(code.Bits, code.Length);
        for (int bit = 3; bit >= 0; bit--)
            if ((code.Value & (1 << bit)) != 0) data.Write((signs >> bit) & 1, 1);
    }
}

internal sealed class BitData
{
    private readonly List<byte> _bytes = new();
    internal int Length { get; private set; }
    internal byte[] Bytes => _bytes.ToArray();

    internal void Write(int value, int count)
    {
        for (int shift = count - 1; shift >= 0; shift--)
        {
            if ((Length & 7) == 0) _bytes.Add(0);
            _bytes[^1] |= (byte)(((value >> shift) & 1) << (7 - (Length & 7)));
            Length++;
        }
    }

    internal void Append(BitData other)
    {
        var bytes = other.Bytes;
        for (int i = 0; i < other.Length; i++) Write((bytes[i >> 3] >> (7 - (i & 7))) & 1, 1);
    }
}
