using BenchmarkDotNet.Attributes;

namespace NLayer.Benchmark;

[MemoryDiagnoser]
public class HuffmanBenchmarks : KernelComparison
{
    private HuffmanHarness _decoder = null!;

    [Params(1, 13, 31, 32, 33)]
    public int Table { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _decoder = new HuffmanHarness(Create("NLayer.Decoder.BitReservoir"));
        var bits = new BitData();
        var codes = HuffmanData.Codes(Table);
        var random = new Random(452);
        for (int i = 0; i < 256; i++)
        {
            var code = codes[random.Next(codes.Length)];
            if (Table >= 32) HuffmanData.WriteQuad(bits, code, random.Next(16));
            else HuffmanData.WritePair(bits, Table, code, random.Next(4),
                random.Next(1 << HuffmanData.LinBits[Table]), random.Next(1 << HuffmanData.LinBits[Table]));
        }
        _decoder.Load(bits.Bytes, start: 8189);
        Decode(); // initialize the selected codebook before measurement
    }

    [Benchmark(OperationsPerInvoke = 256)]
    public float Decode()
    {
        _decoder.Reset();
        float result = 0;
        if (Table >= 32)
        {
            for (int i = 0; i < 256; i++)
            {
                _decoder.Quad(Table, out float x, out float y, out float v, out float w);
                result += x + y + v + w;
            }
        }
        else
        {
            for (int i = 0; i < 256; i++)
            {
                _decoder.Pair(Table, out float x, out float y);
                result += x + y;
            }
        }
        return result;
    }
}
