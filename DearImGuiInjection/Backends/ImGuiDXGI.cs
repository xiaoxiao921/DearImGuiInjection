using System;
using System.Runtime.InteropServices;
using DearImguiSharp;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace DearImGuiInjection.Backends;

internal static class ImGuiDXGI
{
    private static IntPtr _windowHandle;

    private static RenderTargetView _renderTargetView;

    internal static void Init()
    {
        RendererFinder.Renderers.DXGIRenderer.OnPresent += InitImGui;

        RendererFinder.Renderers.DXGIRenderer.OnPresent += RenderImGui;
    }

    internal static void Dispose()
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        RendererFinder.Renderers.DXGIRenderer.OnPresent -= RenderImGui;

        Log.Info("ImGui.ImGuiImplDX11Shutdown()");

        ImGui.ImGuiImplDX11Shutdown();

        DearImGuiInjection.Initialized = false;

        _windowHandle = IntPtr.Zero;

        _renderTargetView = null;
    }

    private static unsafe void InitImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!DearImGuiInjection.Initialized)
        {
            DearImGuiInjection.Context = ImGui.CreateContext(null);

            DearImGuiInjection.IO = ImGui.GetIO();

            DearImGuiInjection.IO.ConfigFlags |= (int)ImGuiConfigFlags.ViewportsEnable;

            InitializeWithHandle(windowHandle);

            using var device = swapChain.GetDevice<Device>();
            ImGui.ImGuiImplDX11Init((void*)device.NativePointer, (void*)device.ImmediateContext.NativePointer);

            using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
            _renderTargetView = new RenderTargetView(device, backBuffer);

            DearImGuiInjection.Initialized = true;
        }

        RendererFinder.Renderers.DXGIRenderer.OnPresent -= InitImGui;
    }

    private static unsafe void InitializeWithHandle(IntPtr windowHandle)
    {
        if (!DearImGuiInjection.Initialized)
        {
            _windowHandle = windowHandle;
            if (_windowHandle == IntPtr.Zero)
                return;

            Log.Info($"ImGuiImplWin32Init, Window Handle: {windowHandle:X}");
            ImGui.ImGuiImplWin32Init(_windowHandle);

            var origWindowProc = GetWindowLongPtr(windowHandle, GWL_WNDPROC);
            var wndProcHandlerPtr = Marshal.GetFunctionPointerForDelegate(new WndProcDelegate(WndProcHandler));
            _original = SetWindowLongPtr64(windowHandle, GWL_WNDPROC, wndProcHandlerPtr);
        }
    }

    private static IntPtr _original;
    private const int GWL_WNDPROC = -4;
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    static extern IntPtr CallWindowProc(IntPtr previousWindowProc, IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public Int32 X;
        public Int32 Y;
    }

    private static POINT cursor_coords;
    private static unsafe IntPtr WndProcHandler(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam)
    {
        ImGui.ImplWin32_WndProcHandler((void*)windowHandle, (uint)message, wParam, lParam);

        if (message == WindowMessage.WM_KEYUP && (VirtualKey)wParam == DearImGuiInjection.CursorVisibilityToggle)
        {
            if (DearImGuiInjection.IsCursorVisible)
            {
                GetCursorPos(out cursor_coords);
            }
            else if (cursor_coords.X + cursor_coords.Y != 0)
            {
                SetCursorPos(cursor_coords.X, cursor_coords.Y);
            }

            ToggleCursor();
        }

        return CallWindowProc(_original, windowHandle, message, wParam, lParam);
    }

    private static void ToggleCursor()
    {
        DearImGuiInjection.IsCursorVisible ^= true;

        var io = DearImGuiInjection.IO;
        if (DearImGuiInjection.IsCursorVisible)
        {
            io.MouseDrawCursor = true;
            io.ConfigFlags &= ~(int)ImGuiConfigFlags.NoMouse;
        }
        else
        {
            io.MouseDrawCursor = false;
            io.ConfigFlags |= (int)ImGuiConfigFlags.NoMouse;
        }
    }

    private static void RenderImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        // Ignore windows which don't belong to us.
        if (!CheckWindowHandle(windowHandle))
        {
            Log.Info($"[DX11 Present] Discarding Window Handle {windowHandle} due to Mismatch");
            return;
        }

        ImGui.ImGuiImplDX11NewFrame();

        NewFrame();

        using var device = swapChain.GetDevice<Device>();
        device.ImmediateContext.OutputMerger.SetRenderTargets(_renderTargetView);

        using var drawData = ImGui.GetDrawData();
        ImGui.ImGuiImplDX11RenderDrawData(drawData);
    }

    private static bool CheckWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
            return windowHandle == _windowHandle || !DearImGuiInjection.Initialized;

        return false;
    }

    private static unsafe void NewFrame()
    {
        ImGui.ImGuiImplWin32NewFrame();
        ImGui.NewFrame();

        if (DearImGuiInjection.RenderAction != null)
        {
            foreach (Action item in DearImGuiInjection.RenderAction.GetInvocationList())
            {
                try
                {
                    item();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        ImGui.EndFrame();
        ImGui.Render();

        if ((DearImGuiInjection.IO.ConfigFlags & (int)ImGuiConfigFlags.ViewportsEnable) > 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault(IntPtr.Zero, IntPtr.Zero);
        }
    }
}