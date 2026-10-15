using System;

namespace NLayer.Decoder
{
    partial class Huffman
    {
        // Eight-bit roots handle common short codes in one lookup. Longer codes
        // use one compact suffix table, with a separate width for each prefix.
        // Shared codebooks (16..23 and 24..31) share their lookup storage too.
        static class LookupTables
        {
            internal static readonly byte[] Indexes =
            {
                255, 0, 1, 2, 255, 3, 4, 5, 6, 7, 8, 9, 10, 11, 255, 12,
                13, 13, 13, 13, 13, 13, 13, 13, 14, 14, 14, 14, 14, 14, 14, 14, 15, 16
            };
            internal static readonly Lazy<LookupTable>[] Tables = Create();

            static Lazy<LookupTable>[] Create()
            {
                var tables = new Lazy<LookupTable>[_codeTables.Length];
                for (int i = 0; i < tables.Length; i++)
                {
                    int index = i;
                    tables[i] = new Lazy<LookupTable>(() => new LookupTable(_codeTables[index]));
                }
                return tables;
            }
        }

        sealed class LookupTable
        {
            internal readonly int PrefixBits;
            internal readonly int[] Entries;

            internal LookupTable(byte[,] tree)
            {
                int maxBits;
                var nodes = InitTable(tree, out maxBits);
                PrefixBits = Math.Min(8, maxBits);
                int roots = 1 << PrefixBits;
                var suffixWidths = new int[roots];
                for (var node = nodes; node != null; node = node.Next)
                {
                    if (node.Length > PrefixBits)
                    {
                        int prefix = node.Bits >> (maxBits - PrefixBits);
                        suffixWidths[prefix] = Math.Max(suffixWidths[prefix], node.Length - PrefixBits);
                    }
                }

                int size = roots;
                for (int i = 0; i < roots; i++)
                    if (suffixWidths[i] != 0) size += 1 << suffixWidths[i];
                Entries = new int[size];
                int offset = roots;
                for (int i = 0; i < roots; i++)
                {
                    int width = suffixWidths[i];
                    if (width == 0) continue;
                    // Negative entries encode a suffix offset and bit count;
                    // positive entries encode total code length and symbol.
                    Entries[i] = -((offset << 5) | width);
                    offset += 1 << width;
                }

                for (var node = nodes; node != null; node = node.Next)
                {
                    int value = (node.Length << 8) | node.Value;
                    int code = node.Bits >> (maxBits - node.Length);
                    if (node.Length <= PrefixBits)
                    {
                        int start = code << (PrefixBits - node.Length);
                        int count = 1 << (PrefixBits - node.Length);
                        for (int i = 0; i < count; i++) Entries[start + i] = value;
                    }
                    else
                    {
                        int prefix = code >> (node.Length - PrefixBits);
                        int branch = -Entries[prefix];
                        int width = branch & 31;
                        int padding = PrefixBits + width - node.Length;
                        int start = (branch >> 5) + ((code & ((1 << (node.Length - PrefixBits)) - 1)) << padding);
                        int count = 1 << padding;
                        for (int i = 0; i < count; i++) Entries[start + i] = value;
                    }
                }
            }
        }

        static byte DecodeSymbolLookup(BitReservoir reservoir, int table)
        {
            var lookup = LookupTables.Tables[LookupTables.Indexes[table]].Value;
            int available;
            int bits = reservoir.TryPeekBits(lookup.PrefixBits, out available);
            bits <<= lookup.PrefixBits - available;
            int entry = lookup.Entries[bits];
            if (entry < 0)
            {
                int branch = -entry;
                int width = branch & 31;
                int total = lookup.PrefixBits + width;
                bits = reservoir.TryPeekBits(total, out available);
                bits <<= total - available;
                entry = lookup.Entries[(branch >> 5) + (bits & ((1 << width) - 1))];
            }
            int length = entry >> 8;
            if (length == 0 || length > available) return 0;
            reservoir.SkipBits(length);
            return (byte)entry;
        }
    }
}
