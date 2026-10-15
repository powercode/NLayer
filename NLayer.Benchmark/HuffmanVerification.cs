using System.Reflection;
using System.Runtime.Loader;

namespace NLayer.Benchmark;

internal static class HuffmanVerification
{
    internal static void Run(string referencePath)
    {
        using var context = new ReferenceContext(Path.GetFullPath(referencePath));
        var before = new HuffmanHarness(context.Reservoir());
        var type = typeof(MpegFile).Assembly.GetType("NLayer.Decoder.BitReservoir", true)!;
        var after = new HuffmanHarness(Activator.CreateInstance(type, nonPublic: true)!);
        VerifyReservoir(before, after);
        long cases = 0;
        foreach (int table in Enumerable.Range(1, 33).Where(t => t != 4 && t != 14))
        foreach (var code in HuffmanData.Codes(table))
        {
            for (int offset = 0; offset < 8; offset++)
            foreach (int signs in table >= 32 ? new[] { 0, 5, 10, 15 } : new[] { 0, 1, 2, 3 })
            {
                var bits = new BitData(); bits.Write(0, offset);
                int extra = table >= 32 ? 0 : (1 << HuffmanData.LinBits[table]) - 1;
                if (table >= 32) HuffmanData.WriteQuad(bits, code, signs);
                else HuffmanData.WritePair(bits, table, code, signs, (signs & 1) == 0 ? 0 : extra, extra);
                var bytes = bits.Bytes;
                foreach (int start in new[] { 0, 8189, 8191 })
                {
                    before.Load(bytes, offset, start); after.Load(bytes, offset, start);
                    if (table >= 32)
                    {
                        before.Quad(table, out float x, out float y, out float v, out float w);
                        after.Quad(table, out float ax, out float ay, out float av, out float aw);
                        Equal(x, ax); Equal(y, ay); Equal(v, av); Equal(w, aw);
                    }
                    else
                    {
                        before.Pair(table, out float x, out float y);
                        after.Pair(table, out float ax, out float ay);
                        Equal(x, ax); Equal(y, ay);
                    }
                    State();
                    if (after.BitsRead != bits.Length - offset)
                        throw new InvalidOperationException($"Incorrect code/sign/linbits consumption in table {table}.");
                    cases++;
                }
            }

            // Every incomplete code prefix, with no trailing padding available.
            // Compare symbol, consumed bits and reservoir position at EOF.
            for (int length = 0; length <= code.Length; length++)
            {
                int offset = (8 - (length & 7)) & 7;
                var bits = new BitData();
                if (length != 0) { bits.Write(0, offset); bits.Write(code.Bits >> (code.Length - length), length); }
                var bytes = bits.Bytes;
                foreach (int start in new[] { 0, 8191 })
                {
                    before.Load(bytes, offset, start); after.Load(bytes, offset, start);
                    byte expected = before.Symbol(table), actual = after.Symbol(table);
                    if (expected != actual) throw new InvalidOperationException($"Symbol mismatch: table {table}, value {code.Value}, prefix length {length}.");
                    if (length == code.Length && actual != code.Value)
                        throw new InvalidOperationException($"Generated codebook symbol differs in table {table}.");
                    State();
                    if (after.BitsRead != (length == code.Length ? length : 0))
                        throw new InvalidOperationException($"Truncated-code consumption mismatch in table {table}.");
                    cases++;
                }
            }
        }

        foreach (int table in new[] { 0, 4, 14 })
        {
            before.Load([]); after.Load([]);
            before.Pair(table, out float x, out float y); after.Pair(table, out float ax, out float ay);
            Equal(x, ax); Equal(y, ay); State(); cases++;
        }
        Console.WriteLine($"PASS: {cases:N0} Huffman cases match (all symbols/tables, signs, linbits, alignments, wraparound, truncated prefixes).");
        var cache = typeof(MpegFile).Assembly.GetType("NLayer.Decoder.Huffman+LookupTables", true)!;
        var tables = (Array)cache.GetField("Tables", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        int entries = 0;
        foreach (object lazy in tables)
        {
            var value = lazy.GetType().GetProperty("Value")!.GetValue(lazy)!;
            entries += ((int[])value.GetType().GetField("Entries", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!).Length;
        }
        Console.WriteLine($"Lookup storage: {tables.Length} shared codebooks, {entries:N0} entries ({entries * sizeof(int):N0} bytes of entry arrays when all books are initialized).");

        using var cold = new ReferenceContext(typeof(MpegFile).Assembly.Location);
        var sharedCode = HuffmanData.Codes(16).Single(c => c.Value == 255);
        var sharedBits = new BitData(); sharedBits.Write(sharedCode.Bits, sharedCode.Length);
        byte[] sharedBytes = sharedBits.Bytes;
        using var startGate = new Barrier(8);
        var workers = Enumerable.Range(0, 8).Select(i => Task.Factory.StartNew(() =>
        {
            var decoder = new HuffmanHarness(cold.Reservoir());
            decoder.Load(sharedBytes);
            startGate.SignalAndWait();
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                decoder.Reset();
                if (decoder.Symbol(16 + i) != 255 || decoder.BitsRead != sharedCode.Length)
                    throw new InvalidOperationException("Concurrent shared-codebook initialization failed.");
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();
        Task.WaitAll(workers);
        Console.WriteLine("PASS: concurrent first use of all eight aliases of codebook 16.");

        void State()
        {
            if (before.BitsRead != after.BitsRead || before.Available != after.Available)
                throw new InvalidOperationException("Huffman reservoir position differs.");
        }
    }

    private static void VerifyReservoir(HuffmanHarness before, HuffmanHarness after)
    {
        var random = new Random(32768);
        long cases = 0;
        foreach (int size in new[] { 0, 1, 2, 3, 4, 5, 31, 100, 8191 })
        foreach (int start in new[] { 0, 8188, 8191 })
        for (int offset = 0; offset < 8; offset++)
        {
            var bytes = new byte[size]; random.NextBytes(bytes);
            for (int count = 0; count <= 32; count++)
            {
                before.Load(bytes, offset, start); after.Load(bytes, offset, start);
                int expected = before.Peek(count, out int expectedRead);
                int actual = after.Peek(count, out int actualRead);
                if (expected != actual || expectedRead != actualRead) throw new InvalidOperationException("Reservoir peek mismatch.");
                if (expectedRead == count)
                {
                    if (before.Get(count) != after.Get(count)) throw new InvalidOperationException("Reservoir read mismatch.");
                    CheckState();
                    before.Rewind(count); after.Rewind(count);
                    CheckState();
                    if (before.Peek(count, out expectedRead) != after.Peek(count, out actualRead) || expectedRead != actualRead)
                        throw new InvalidOperationException("Reservoir rewind mismatch.");
                }
                CheckState();
                cases++;
            }
            before.Load(bytes, offset, start); after.Load(bytes, offset, start);
            int remaining = size == 0 ? 0 : size * 8 - offset;
            while (remaining > 0)
            {
                int count = Math.Min(remaining, random.Next(1, 33));
                if (count == 1)
                {
                    if (before.GetOne() != after.GetOne()) throw new InvalidOperationException("Reservoir single-bit mismatch.");
                }
                else { before.Skip(count); after.Skip(count); }
                remaining -= count;
                CheckState();
                cases++;
            }
        }
        Console.WriteLine($"PASS: {cases:N0} reservoir checks (0..32 bits, truncation, wraparound, skip, rewind and single-bit reads).");

        void CheckState()
        {
            if (before.BitsRead != after.BitsRead || before.Available != after.Available)
                throw new InvalidOperationException("Reservoir state mismatch.");
        }
    }

    private static void Equal(float expected, float actual)
    {
        if (BitConverter.SingleToInt32Bits(expected) != BitConverter.SingleToInt32Bits(actual))
            throw new InvalidOperationException($"Huffman PCM value differs: {expected} != {actual}.");
    }

    private sealed class ReferenceContext : IDisposable
    {
        private readonly AssemblyLoadContext _context = new(null, isCollectible: true);
        private readonly Type _type;
        internal ReferenceContext(string path) => _type = _context.LoadFromAssemblyPath(path).GetType("NLayer.Decoder.BitReservoir", true)!;
        internal object Reservoir() => Activator.CreateInstance(_type, nonPublic: true)!;
        public void Dispose() => _context.Unload();
    }
}
