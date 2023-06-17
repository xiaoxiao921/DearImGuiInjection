using System;
using System.Runtime.InteropServices;
using DearImGuiInjection.Windows;
using ImGuiNET;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace DearImGuiInjection.Backends;

internal static class ImGuiDX11
{
    private static IntPtr _windowHandle;

    private static RenderTargetView _renderTargetView;
    private static User32.WndProcDelegate _myWindowProc;
    private static IntPtr _originalWindowProc;
    private const int GWL_WNDPROC = -4;

    private static User32.POINT _cursorCoords;

    internal static void Init()
    {
        RendererFinder.Renderers.DX11Renderer.OnPresent += InitImGui;

        RendererFinder.Renderers.DX11Renderer.OnPresent += RenderImGui;
        RendererFinder.Renderers.DX11Renderer.PreResizeBuffers += PreResizeBuffers;
        RendererFinder.Renderers.DX11Renderer.PostResizeBuffers += PostResizeBuffers;
    }

    internal static void Dispose()
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        RendererFinder.Renderers.DX11Renderer.PostResizeBuffers -= PostResizeBuffers;
        RendererFinder.Renderers.DX11Renderer.PreResizeBuffers -= PreResizeBuffers;
        RendererFinder.Renderers.DX11Renderer.OnPresent -= RenderImGui;

        User32.SetWindowLong(_windowHandle, GWL_WNDPROC, _originalWindowProc);

        ImGuiWin32Impl.Shutdown();

        _renderTargetView = null;

        Log.Info("ImGui.ImGuiImplDX11Shutdown()");
        ImGuiDX11Impl.Shutdown();

        _windowHandle = IntPtr.Zero;
    }

    private static unsafe void InitImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!DearImGuiInjection.Initialized)
        {
            DearImGuiInjection.InitImGui();

            InitImGuiWin32(windowHandle);

            DearImGuiInjection.UpdateCursorVisibility();

            InitImGuiDX11(swapChain);

            DearImGuiInjection.Initialized = true;
        }

        RendererFinder.Renderers.DX11Renderer.OnPresent -= InitImGui;
    }

    private static unsafe void InitImGuiWin32(IntPtr windowHandle)
    {
        if (!DearImGuiInjection.Initialized)
        {
            _windowHandle = windowHandle;
            if (_windowHandle == IntPtr.Zero)
                return;

            Log.Info($"ImGuiImplWin32Init, Window Handle: {windowHandle:X}");
            ImGuiWin32Impl.Init(_windowHandle);

            _myWindowProc = new User32.WndProcDelegate(WndProcHandler);
            _originalWindowProc = User32.SetWindowLong(windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_myWindowProc));
        }
    }

    private static unsafe void InitImGuiDX11(SwapChain swapChain)
    {
        using var device = InitImGuiDX11Internal(swapChain);

        ImGuiDX11Impl.Init((void*)device.NativePointer, (void*)device.ImmediateContext.NativePointer);
    }

    private static unsafe Device InitImGuiDX11Internal(SwapChain swapChain)
    {
        var device = swapChain.GetDevice<Device>();
        using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        _renderTargetView = new RenderTargetView(device, backBuffer);

        return device;
    }

    private static unsafe IntPtr WndProcHandler(IntPtr windowHandle, WindowMessage message, IntPtr wParam, IntPtr lParam)
    {
        ImGuiWin32Impl.WndProcHandler(windowHandle, message, wParam, lParam);

        if (message == WindowMessage.WM_KEYUP && (VirtualKey)wParam == DearImGuiInjection.CursorVisibilityToggle.Get())
        {
            SaveOrRestoreCursorPosition();

            DearImGuiInjection.ToggleCursor();
        }

        return User32.CallWindowProc(_originalWindowProc, windowHandle, message, wParam, lParam);
    }

    private static unsafe void SaveOrRestoreCursorPosition()
    {
        if (DearImGuiInjection.IsCursorVisible)
        {
            User32.GetCursorPos(out _cursorCoords);
        }
        else if (_cursorCoords.X + _cursorCoords.Y != 0)
        {
            User32.SetCursorPos(_cursorCoords.X, _cursorCoords.Y);
        }
    }

    private static unsafe void RenderImGui(SwapChain swapChain, uint syncInterval, uint flags)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!IsTargetWindowHandle(windowHandle))
        {
            Log.Info($"[DX11] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGuiDX11Impl.NewFrame();

        NewFrame();

        using var device = swapChain.GetDevice<Device>();
        device.ImmediateContext.OutputMerger.SetRenderTargets(_renderTargetView);

        var drawData = ImGui.GetDrawData();
        ImGuiDX11Impl.RenderDrawData(drawData.NativePtr);
    }

    private static bool IsTargetWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle != IntPtr.Zero)
        {
            return windowHandle == _windowHandle || !DearImGuiInjection.Initialized;
        }

        return false;
    }

    private static unsafe void NewFrame()
    {
        ImGuiWin32Impl.NewFrame();
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

        if ((DearImGuiInjection.IO.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) > 0)
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

        if (!IsTargetWindowHandle(windowHandle))
        {
            Log.Info($"[DX11 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        _renderTargetView?.Dispose();
        _renderTargetView = null;
        ImGuiDX11Impl.InvalidateDeviceObjects();
    }

    private static void PostResizeBuffers(SwapChain swapChain, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags)
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        var windowHandle = swapChain.Description.OutputHandle;

        if (!IsTargetWindowHandle(windowHandle))
        {
            Log.Info($"[DX11 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGuiDX11Impl.CreateDeviceObjects();

        using var device = swapChain.GetDevice<Device>();
        using var backBuffer = swapChain.GetBackBuffer<Texture2D>(0);
        _renderTargetView = new RenderTargetView(device, backBuffer);
    }
}