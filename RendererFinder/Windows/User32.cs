using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RendererFinder.Windows;

internal static class User32
{
    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreateWindowExW(uint dwExStyle, IntPtr windowClass, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
    uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr pvParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);
    [DllImport("user32.dll")]
    static extern IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
    delegate IntPtr WndProcDelegate(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    static IntPtr WndProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
    {
        return DefWindowProcW(window, message, wParam, lParam);
    }
    struct WNDCLASSEXW
    {
        public int cbSize;
        public int style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszMenuName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpszClassName;
        public IntPtr hIconSm;
    }
    static readonly WndProcDelegate s_WndProc = WndProc;
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U2)]
    static extern ushort RegisterClassExW([In] ref WNDCLASSEXW lpwcx);

    [DllImport("user32.dll")]
    public static extern bool DestroyWindow(IntPtr hWnd);

    public static IntPtr CreateFakeWindow()
    {
        // Register window class
        const string defaultWindowClass = "DearImGuiInjectionWindowClass";

        // Register window class
        var windowClass = new WNDCLASSEXW();
        windowClass.cbSize = Marshal.SizeOf<WNDCLASSEXW>();
        windowClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(s_WndProc);
        windowClass.hInstance = GetModuleHandle(null);
        windowClass.lpszClassName = defaultWindowClass;

        var registeredClass = RegisterClassExW(ref windowClass);
        if (registeredClass == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var windowHandle = CreateWindowExW(0, new IntPtr(registeredClass), "DearImGuiInjection Window", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, GetModuleHandle(null), IntPtr.Zero);
        if (windowHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        return windowHandle;
    }
}
