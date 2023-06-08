using System;
using System.Runtime.InteropServices;

namespace NativeMemory;

public static unsafe class Allocator
{
    /// <summary>
    /// alloc array in hglobal memory, don't forget to free !
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static IntPtr AllocAndCopy(byte[] array)
    {
        var arrayPtr = Marshal.AllocHGlobal(array.Length);
        Marshal.Copy(array, 0, arrayPtr, array.Length);
        return arrayPtr;
    }

    /// <summary>
    /// alloc array in hglobal memory and zero it, don't forget to free !
    /// </summary>
    /// <param name="array"></param>
    /// <returns></returns>
    public static IntPtr AllocAndZeroMemory(uint byteCount)
    {
        var arrayPtr = Marshal.AllocHGlobal((int)byteCount);
        var dummyArray = new byte[byteCount];
        Marshal.Copy(dummyArray, 0, arrayPtr, dummyArray.Length);
        return arrayPtr;
    }
}