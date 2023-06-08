using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NativeMemory;

public static class StringBytePattern
{
    public static byte?[] ParseHexBytes(this string str)
    {
        static bool IsHexChar(char lowerC) => '0' <= lowerC && lowerC <= '9' || 'a' <= lowerC && lowerC <= 'f';

        var result = new List<byte?>();

        var sr = new StringReader(str);

        while (sr.Peek() > 0)
        {
            var c = char.ToLower((char)sr.Read());

            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            if (c == ';')
            {
                sr.ReadLine();
            }
            else if (c == '?')
            {
                result.Add(null);
                sr.Read();
            }
            else if (IsHexChar(c) && sr.Peek() > 0)
            {
                var other = char.ToLower((char)sr.Peek());
                if (!IsHexChar(other))
                {
                    continue;
                }

                sr.Read();
                result.Add(byte.Parse($"{c}{other}", NumberStyles.HexNumber));
            }
        }

        return result.ToArray();
    }
}

public class BytePattern
{
    private readonly byte?[] Pattern;
    private readonly int[] JumpTable;

    public BytePattern(string bytes)
    {
        Pattern = bytes.ParseHexBytes();
        JumpTable = CreateJumpTable();
    }

    public BytePattern(byte[] bytes)
    {
        Pattern = bytes.Cast<byte?>().ToArray();
        JumpTable = CreateJumpTable();
    }

    public int Length => Pattern.Length;

    public bool IsE8 => Pattern[0] == 0xE8;

    public static implicit operator BytePattern(string pattern) => new BytePattern(pattern);

    public static implicit operator BytePattern(byte[] pattern) => new BytePattern(pattern);

    // Table-building algorithm from KMP
    private int[] CreateJumpTable()
    {
        var jumpTable = new int[Pattern.Length];

        var substrCandidate = 0;
        jumpTable[0] = -1;
        for (var i = 1; i < Pattern.Length; i++, substrCandidate++)
        {
            if (Pattern[i] == Pattern[substrCandidate])
            {
                jumpTable[i] = jumpTable[substrCandidate];
            }
            else
            {
                jumpTable[i] = substrCandidate;
                while (substrCandidate >= 0 && Pattern[i] != Pattern[substrCandidate])
                {
                    substrCandidate = jumpTable[substrCandidate];
                }
            }
        }

        return jumpTable;
    }

    public unsafe IntPtr Match(IntPtr startAddress, long size)
    {
        var ptr = (byte*)startAddress.ToPointer();
        for (long j = 0, k = 0; j < size;)
        {
            if (Pattern[k] == null || ptr[j] == Pattern[k])
            {
                j++;
                k++;
                if (k == Pattern.Length)
                {
                    return new IntPtr(j - k);
                }
            }
            else
            {
                k = JumpTable[k];
                if (k >= 0)
                {
                    continue;
                }

                j++;
                k++;
            }
        }

        return IntPtr.Zero;
    }
}
