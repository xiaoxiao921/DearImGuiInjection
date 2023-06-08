using System;
using System.Collections.Generic;
using Logger;

namespace NativeMemory;

public class PatternBatch
{
    internal struct Entry
    {
        internal string Name;
        internal BytePattern BytePattern;
        internal Action<IntPtr> OnCompletion;

        internal Entry(string name, BytePattern bytePattern, Action<IntPtr> onCompletion)
        {
            Name = name;
            BytePattern = bytePattern;
            OnCompletion = onCompletion;
        }
    }

    private readonly List<Entry> Entries = new();

    public IntPtr StartAddress { get; }
    public long Size { get; }

    internal PatternBatch(IntPtr startAddress, long size)
    {
        StartAddress = startAddress;
        Size = size;
    }

    public void Add(string name, BytePattern bytePattern, Action<IntPtr> onCompletion) => Entries.Add(new Entry(name, bytePattern, onCompletion));

    public void Run()
    {
        foreach (Entry entry in Entries)
        {
            IntPtr result = entry.BytePattern.Match(StartAddress, Size);
            if (result == IntPtr.Zero)
            {
                Log.Error($"BytePattern {entry.Name} failed.");
            }
            else
            {
                try
                {
                    entry.OnCompletion?.Invoke(result);
                }
                catch (Exception e)
                {
                    Log.Error($"{e}");
                }
            }
        }
    }
}