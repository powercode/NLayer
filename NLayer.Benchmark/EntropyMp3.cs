namespace NLayer.Benchmark;

// Dense, valid MPEG-1 stereo frames: four distinct pair tables including linbits,
// followed by both count1 tables. No encoder, copyrighted media or reservoir.
internal static class EntropyMp3
{
    internal static byte[] Create(int frames)
    {
        const int frameLength = 1044; // 320 kbps, 44.1 kHz
        var result = new byte[frameLength * frames];
        var random = new Random(983);
        int[] tables = [13, 16, 24, 31];
        for (int frame = 0; frame < frames; frame++)
        {
            var side = new BitData();
            var main = new BitData();
            side.Write(0, 20); // reservoir, private bits, scfsi
            for (int gc = 0; gc < 4; gc++)
            {
                int table = tables[gc];
                var codes = HuffmanData.Codes(table);
                var payload = new BitData();
                for (int i = 0; i < 64; i++)
                    HuffmanData.WritePair(payload, table, codes[random.Next(codes.Length)], random.Next(4),
                        random.Next(1 << Math.Min(HuffmanData.LinBits[table], 3)),
                        random.Next(1 << Math.Min(HuffmanData.LinBits[table], 3)));
                var quads = HuffmanData.Codes(32 + (gc & 1));
                for (int i = 0; i < 48; i++)
                    HuffmanData.WriteQuad(payload, quads[random.Next(quads.Length)], random.Next(16));
                side.Write(payload.Length, 12);
                side.Write(64, 9); // big_values: followed by 48 count1 quads
                side.Write(155, 8); // global gain
                side.Write(0, 5); // scalefac_compress, window_switching_flag
                side.Write(table, 5); side.Write(table, 5); side.Write(table, 5);
                side.Write(0, 4 + 3 + 1 + 1);
                side.Write(gc & 1, 1); // count1table_select
                main.Append(payload);
            }
            if (side.Length != 256 || main.Length > (frameLength - 36) * 8)
                throw new InvalidOperationException("Generated entropy frame does not fit.");
            int offset = frame * frameLength;
            result[offset] = 0xff; result[offset + 1] = 0xfb; result[offset + 2] = 0xe0;
            side.Bytes.CopyTo(result, offset + 4);
            main.Bytes.CopyTo(result, offset + 36);
        }
        return result;
    }
}
