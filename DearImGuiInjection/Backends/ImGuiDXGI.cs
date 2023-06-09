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

    private static IntPtr _originalWindowProc;
    private const int GWL_WNDPROC = -4;

    private static POINT _cursorCoords;

    internal static void Init()
    {
        RendererFinder.Renderers.DXGIRenderer.OnPresent += InitImGui;

        RendererFinder.Renderers.DXGIRenderer.OnPresent += RenderImGui;
        RendererFinder.Renderers.DXGIRenderer.PreResizeBuffers += PreResizeBuffers;
        RendererFinder.Renderers.DXGIRenderer.PostResizeBuffers += PostResizeBuffers;
    }

    internal static void Dispose()
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        RendererFinder.Renderers.DXGIRenderer.PostResizeBuffers -= PostResizeBuffers;
        RendererFinder.Renderers.DXGIRenderer.PreResizeBuffers -= PreResizeBuffers;
        RendererFinder.Renderers.DXGIRenderer.OnPresent -= RenderImGui;

        SetWindowLongPtr64(_windowHandle, GWL_WNDPROC, _originalWindowProc);

        _renderTargetView = null;

        Log.Info("ImGui.ImGuiImplDX11Shutdown()");
        ImGui.ImGuiImplDX11Shutdown();

        _windowHandle = IntPtr.Zero;
    }

    private static unsafe void InitImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!DearImGuiInjection.Initialized)
        {
            DearImGuiInjection.Context = ImGui.CreateContext(null);

            // todo: same font as bepinexgui
            // todo: make insert key for making cursor visible configurable
            // todo: imgui.ini file inside bepinex / config
            // todo: 

            DearImGuiInjection.IO = ImGui.GetIO();

            InitImGuiWin32(windowHandle);

            DearImGuiInjection.UpdateCursorVisibility();

            InitImGuiDX11(swapChain);

            DearImGuiInjection.Initialized = true;
        }

        RendererFinder.Renderers.DXGIRenderer.OnPresent -= InitImGui;
    }

    private static unsafe void InitImGuiWin32(IntPtr windowHandle)
    {
        if (!DearImGuiInjection.Initialized)
        {
            _windowHandle = windowHandle;
            if (_windowHandle == IntPtr.Zero)
                return;

            Log.Info($"ImGuiImplWin32Init, Window Handle: {windowHandle:X}");
            ImGui.ImGuiImplWin32Init(_windowHandle);

            _originalWindowProc = SetWindowLongPtr64(windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(new WndProcDelegate(WndProcHandler)));
        }
    }

    private static unsafe void InitImGuiDX11(SwapChain swapChain)
    {
        using var device = swapChain.GetDevice<Device>();
        ImGui.ImGuiImplDX11Init((void*)device.NativePointer, (void*)device.ImmediateContext.NativePointer);
        using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        _renderTargetView = new RenderTargetView(device, backBuffer);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr previousWindowProc, IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProcDelegate(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    struct POINT
    {
        public Int32 X;
        public Int32 Y;
    }

    private static unsafe IntPtr WndProcHandler(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam)
    {
        ImGui.ImplWin32_WndProcHandler((void*)windowHandle, (uint)message, wParam, lParam);

        if (message == WindowMessage.WM_KEYUP && (VirtualKey)wParam == DearImGuiInjection.CursorVisibilityToggle)
        {
            SaveOrRestoreCursorPosition();

            DearImGuiInjection.ToggleCursor();
        }

        return CallWindowProc(_originalWindowProc, windowHandle, message, wParam, lParam);
    }

    private static unsafe void SaveOrRestoreCursorPosition()
    {
        if (DearImGuiInjection.IsCursorVisible)
        {
            GetCursorPos(out _cursorCoords);
        }
        else if (_cursorCoords.X + _cursorCoords.Y != 0)
        {
            SetCursorPos(_cursorCoords.X, _cursorCoords.Y);
        }
    }

    private static void RenderImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!CheckWindowHandle(windowHandle))
        {
            Log.Info($"[DX11] Discarding window handle {windowHandle:X} due to mismatch");
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

    private static void PreResizeBuffers(SwapChain swapChain, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags)
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        var windowHandle = swapChain.Description.OutputHandle;

        Log.Info($"[DX11 ResizeBuffers] Window Handle {windowHandle:X}");

        if (!CheckWindowHandle(windowHandle))
        {
            Log.Info($"[DX11 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        _renderTargetView?.Dispose();
        _renderTargetView = null;
        ImGui.ImGuiImplDX11InvalidateDeviceObjects();
    }

    private static void PostResizeBuffers(SwapChain swapChain, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags)
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        var windowHandle = swapChain.Description.OutputHandle;

        if (!CheckWindowHandle(windowHandle))
        {
            Log.Info($"[DX11 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGui.ImGuiImplDX11CreateDeviceObjects();

        using var device = swapChain.GetDevice<Device>();
        using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        _renderTargetView = new RenderTargetView(device, backBuffer);
    }
}