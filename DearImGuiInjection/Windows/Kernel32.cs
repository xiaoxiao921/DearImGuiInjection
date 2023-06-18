using System;
using System.Runtime.InteropServices;
using System.Security;

namespace DearImGuiInjection.Windows;

public static class Kernel32
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [SuppressUnmanagedCodeSecurity]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    public const UInt32 INFINITE = 0xFFFFFFFF;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

    [DllImport("kernel32.dll")]
    public static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

    /// <summary>
    /// Sets the bits of a 64-bit value to indicate the comparison operator to use for a specified operating system version attribute. 
    /// This function is used to build the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>  and 
    /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions.
    /// </summary>
    /// <param name="dwlConditionMask">
    /// A value to be passed as the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> and 
    /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions. The function stores the comparison information in the bits of this variable.
    /// </param>
    /// <param name="dwTypeBitMask">A mask that indicates the member of <see cref="NativeTypes.OSVERSIONINFOEX"/> whose comparision operator is being set.
    /// This value corresponds to one of the bits specified in the dwTypeMask parameter of <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>. 
    /// This parameter can have one of the values from <see cref="NativeConstants.TypeBitMasks"/></param>
    /// <param name="dwConditionMask"></param>
    /// <returns>The function returns the condition mask value.</returns>
    /// <remarks> 
    /// Before the first call to this function, initialize <paramref name="dwlConditionMask"/> variable to zero. 
    /// For subsequent calls, pass in the variable used in the previous call.
    /// </remarks>
    [SecurityCritical, SuppressUnmanagedCodeSecurity]
    [DllImport("kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern ulong VerSetConditionMask(ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask);


    /// <summary>
    /// Sets the bits of a 64-bit value to indicate the comparison operator to use for a specified operating system version attribute. 
    /// This function is used to build the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>  and 
    /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions.
    /// </summary>
    /// <param name="dwlConditionMask">
    /// A value to be passed as the dwlConditionMask parameter of the <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> and 
    /// <see cref="VerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/> functions. The function stores the comparison information in the bits of this variable.
    /// </param>
    /// <param name="dwTypeBitMask">A mask that indicates the member of <see cref="NativeTypes.OSVERSIONINFOEX"/> whose comparision operator is being set.
    /// This value corresponds to one of the bits specified in the dwTypeMask parameter of <see cref="RtlVerifyVersionInfo(NativeTypes.OSVERSIONINFOEX, uint, ulong)"/>. 
    /// This parameter can have one of the values from <see cref="NativeConstants.TypeBitMasks"/></param>
    /// <param name="dwConditionMask"></param>
    /// <remarks> 
    /// Before the first call to this function, initialize <paramref name="dwlConditionMask"/> variable to zero. 
    /// For subsequent calls, pass in the variable used in the previous call.
    /// </remarks>
    [SecuritySafeCritical]
    internal static void VER_SET_CONDITION(ref ulong dwlConditionMask, uint dwTypeBitMask, byte dwConditionMask)
    {
        dwlConditionMask = VerSetConditionMask(dwlConditionMask, dwTypeBitMask, dwConditionMask);
    }

    public static ushort HiByte(ushort wValue)
    {
        return (ushort)((wValue >> 8) & 0xFF);
    }

    public const uint MB_PRECOMPOSED = 0x00000001;

    public const uint CP_ACP = 0;
    public const uint CP_OEMCP = 1;
    public const uint CP_SYMBOL = 42;
    public const uint CP_UTF7 = 65000;
    public const uint CP_UTF8 = 65001;
    public const uint CP_GB2312 = 936;
    public const uint CP_BIG5 = 950;
    public const uint CP_SHIFTJIS = 932;

    [DllImport("kernel32.dll", SetLastError = true)]
    public static unsafe extern int MultiByteToWideChar(uint codePage,
                                                  uint dwFlags,
                                                  [In] [MarshalAs(UnmanagedType.LPArray)]
                                                  byte[] lpMultiByteStr,
                                                  int cbMultiByte,
                                                  IntPtr lpWideCharStr,
                                                  int cchWideChar);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool QueryPerformanceFrequency(out long frequency);
}

[StructLayout(LayoutKind.Explicit)]
public struct LargeInteger
{
    [FieldOffset(0)]
    public int Low;
    [FieldOffset(4)]
    public int High;
    [FieldOffset(0)]
    public long QuadPart;

    // use only when QuadPart canot be passed
    public long ToInt64()
    {
        return ((long)this.High << 32) | (uint)this.Low;
    }

    // just for demonstration
    public static LargeInteger FromInt64(long value)
    {
        return new LargeInteger
        {
            Low = (int)(value),
            High = (int)((value >> 32))
        };
    }

}
