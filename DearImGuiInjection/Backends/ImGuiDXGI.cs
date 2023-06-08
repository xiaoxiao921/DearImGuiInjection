using System;
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
            Log.Info($"[DX11 Present] Init DX11, Window Handle: {windowHandle:X}");
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

            Log.Info($"[ImguiHook] Init with Window Handle {(long)_windowHandle:X}");
            ImGui.ImGuiImplWin32Init(_windowHandle);

            // todo
            // https://github.com/Sewer56/Reloaded.Imgui.Hook/blob/master/Reloaded.Imgui.Hook/ImguiHook.cs#L235
            //var wndProcHandlerPtr = (IntPtr)SDK.Hooks.Utilities.GetFunctionPointer(typeof(ImguiHook), nameof(WndProcHandler));
            //WndProcHook = WndProcHook.Create(_windowHandle, Unsafe.As<IntPtr, WndProcHook.WndProc>(ref wndProcHandlerPtr));
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

        if ((ImGui.GetIO().ConfigFlags & (int)ImGuiConfigFlags.ViewportsEnable) > 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault(IntPtr.Zero, IntPtr.Zero);
        }
    }
}