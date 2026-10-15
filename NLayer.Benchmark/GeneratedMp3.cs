namespace NLayer.Benchmark;

// MPEG-1 Layer III, 44.1 kHz, 128 kbps stereo. No reservoir, CRC or padding.
// Each granule/channel contains 32 Huffman table-1 pairs (+/-1, +/-1), no
// scalefactors or count1 values. This exercises entropy decoding without
// needing an external encoder or a copyrighted audio fixture.
internal static class GeneratedMp3
{
    public static byte[] Create(int frameCount)
    {
        const int frameLength = 417;
        var data = new byte[frameCount * frameLength];
        var random = new Random(42);
        for (int frame = 0; frame < frameCount; frame++)
        {
            int start = frame * frameLength;
            data[start] = 0xff;
            data[start + 1] = 0xfb;
            data[start + 2] = 0x90;
            int bit = (start + 4) * 8;
            Write(0, 9 + 3 + 8); // main_data_begin, private bits, scfsi
            for (int granuleChannel = 0; granuleChannel < 4; granuleChannel++)
            {
                Write(160, 12); // part2_3_length: 32 pairs * (3 code + 2 sign bits)
                Write(32, 9);   // big_values
                Write(180, 8);  // global_gain
                Write(0, 4);   // scalefac_compress
                Write(0, 1);   // window_switching_flag: long block
                Write(1, 5); Write(1, 5); Write(1, 5); // table_select
                Write(0, 4 + 3 + 1 + 1 + 1); // regions, preflag, scale, count1 table
            }
            for (int pair = 0; pair < 128; pair++)
            {
                Write(0, 3); // table 1: (1, 1)
                Write(random.Next(4), 2); // signs
            }

            void Write(int value, int count)
            {
                for (int shift = count - 1; shift >= 0; shift--, bit++)
                    data[bit >> 3] |= (byte)(((value >> shift) & 1) << (7 - (bit & 7)));
            }
        }
        return data;
    }
}
