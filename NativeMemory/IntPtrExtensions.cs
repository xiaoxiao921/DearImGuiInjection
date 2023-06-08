using System;

namespace NativeMemory;

public static class IntPtrExtensions
{
    public static IntPtr Add(this IntPtr ptr, nint offset) =>
        new(ptr + offset);

    public static IntPtr Sub(this IntPtr ptr, nint offset) =>
        new(ptr - offset);

    public static unsafe IntPtr Rip(this IntPtr ptr) =>
        ptr.Add(*(int*)ptr).Add(4);
}