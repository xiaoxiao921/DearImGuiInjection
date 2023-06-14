using System;
using System.Runtime.InteropServices;
using DearImGuiInjection.Windows;
using DearImguiSharp;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D12.Device;

namespace DearImGuiInjection.Backends;

internal static class ImGuiDX12
{
    private static IntPtr _windowHandle;

    internal class FrameContext
    {
        internal CommandAllocator CommandAllocator;
        internal SharpDX.Direct3D12.Resource MainRenderTargetResource;
        internal CpuDescriptorHandle MainRenderTargetDescriptor;
    }
    private static FrameContext[] _frameContext;
    private static GraphicsCommandList _commandList;
    private static DescriptorHeap _shaderDescriptorHeap;
    private static CommandQueue _commandQueue;

    private static WndProcDelegate _myWindowProc;
    private static IntPtr _originalWindowProc;
    private const int GWL_WNDPROC = -4;

    private static POINT _cursorCoords;

    internal static void Init()
    {
        Log.Info("ImGuiDX12.Init");

        RendererFinder.Renderers.DX12Renderer.OnPresent += InitImGui;

        RendererFinder.Renderers.DX12Renderer.OnPresent += RenderImGui;

        RendererFinder.Renderers.DX12Renderer.OnExecuteCommandList += RetrieveCommandQueue;

        RendererFinder.Renderers.DX12Renderer.PreResizeBuffers += PreResizeBuffers;
        RendererFinder.Renderers.DX12Renderer.PostResizeBuffers += PostResizeBuffers;
    }

    internal static void Dispose()
    {
        if (!DearImGuiInjection.Initialized)
        {
            return;
        }

        RendererFinder.Renderers.DX12Renderer.PostResizeBuffers -= PostResizeBuffers;
        RendererFinder.Renderers.DX12Renderer.PreResizeBuffers -= PreResizeBuffers;

        RendererFinder.Renderers.DX12Renderer.OnExecuteCommandList -= RetrieveCommandQueue;
        RendererFinder.Renderers.DX12Renderer.OnPresent -= RenderImGui;

        SetWindowLong(_windowHandle, GWL_WNDPROC, _originalWindowProc);

        ImGui.ImGuiImplWin32Shutdown();

        Log.Info("ImGui.ImGuiImplDX12Shutdown()");
        ImGui.ImGuiImplDX12Shutdown();

        _windowHandle = IntPtr.Zero;
    }

    private static bool RetrieveCommandQueue(CommandQueue commandQueue, uint arg2, IntPtr ptr)
    {
        if (commandQueue.Description.Type == CommandListType.Direct)
        {
            Log.Info("Retrieved the command queue.");
            _commandQueue = commandQueue;
            return true;
        }

        return false;
    }

    private static unsafe void InitImGui(SwapChain1 swapChain, uint syncInterval, uint flags, IntPtr presentParameters)
    {
        var windowHandle = swapChain.Description.OutputHandle;

        if (!DearImGuiInjection.Initialized)
        {
            Log.Info("DearImGuiInjection.InitImGui()");
            DearImGuiInjection.InitImGui();

            InitImGuiWin32(windowHandle);

            DearImGuiInjection.UpdateCursorVisibility();

            InitImGuiDX12(swapChain);

            DearImGuiInjection.Initialized = true;
        }

        RendererFinder.Renderers.DX12Renderer.OnPresent -= InitImGui;
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

            _myWindowProc = new WndProcDelegate(WndProcHandler);
            _originalWindowProc = SetWindowLong(windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_myWindowProc));
        }
    }

    private static unsafe void InitImGuiDX12(SwapChain swapChain)
    {
        using var device = InitImGuiDX12Internal(swapChain, out var bufferCount);

        ImGui.ImGuiImplDX12Init((void*)device.NativePointer, bufferCount,
                    DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8UNORM,
                    new((void*)_shaderDescriptorHeap.NativePointer), _shaderDescriptorHeap.CPUDescriptorHandleForHeapStart.Ptr, _shaderDescriptorHeap.GPUDescriptorHandleForHeapStart.Ptr);

        Log.Info("InitImGuiDX12 Finished.");
    }

    private static unsafe Device InitImGuiDX12Internal(SwapChain swapChain, out int bufferCount)
    {
        var device = swapChain.GetDevice<Device>();
        var swapChainDescription = swapChain.Description;

        bufferCount = swapChainDescription.BufferCount;
        _shaderDescriptorHeap = device.CreateDescriptorHeap(new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            DescriptorCount = bufferCount,
            Flags = DescriptorHeapFlags.ShaderVisible
        });

        _frameContext = new FrameContext[bufferCount];
        for (int i = 0; i < bufferCount; i++)
        {
            _frameContext[i] = new()
            {
                CommandAllocator = device.CreateCommandAllocator(CommandListType.Direct)
            };
        }

        _commandList = device.CreateCommandList(CommandListType.Direct, _frameContext[0].CommandAllocator, null);
        _commandList.Close();

        var descriptorBackBuffer = device.CreateDescriptorHeap(new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.RenderTargetView,
            DescriptorCount = bufferCount,
            Flags = DescriptorHeapFlags.None,
            NodeMask = 1
        });

        var rtvDescriptorSize = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        var rtvHandle = descriptorBackBuffer.CPUDescriptorHandleForHeapStart;

        for (int i = 0; i < bufferCount; i++)
        {
            var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D12.Resource>(i);

            _frameContext[i].MainRenderTargetDescriptor = rtvHandle;
            _frameContext[i].MainRenderTargetResource = backBuffer;

            device.CreateRenderTargetView(backBuffer, null, rtvHandle);
            rtvHandle.Ptr += rtvDescriptorSize;
        }

        return device;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 8)
            return GetWindowLongPtr64(hWnd, nIndex);
        else
            return GetWindowLongPtr32(hWnd, nIndex);
    }

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

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

        if (message == WindowMessage.WM_KEYUP && (VirtualKey)wParam == DearImGuiInjection.CursorVisibilityToggle.Get())
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

    private static unsafe void RenderImGui(SwapChain3 swapChain, uint syncInterval, uint flags, IntPtr presentParameters)
    {
        if (_commandQueue == null)
        {
            return;
        }

        var windowHandle = swapChain.Description.OutputHandle;

        if (!IsTargetWindowHandle(windowHandle))
        {
            Log.Info($"[DX12] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGui.ImGuiImplDX12NewFrame();

        NewFrame();

        var currentFrameContext = _frameContext[swapChain.CurrentBackBufferIndex];
        currentFrameContext.CommandAllocator.Reset();

        const int D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES = unchecked((int)0xffffffff);
        var barrier = new ResourceBarrier
        {
            Type = ResourceBarrierType.Transition,
            Flags = ResourceBarrierFlags.None,
            Transition = new(currentFrameContext.MainRenderTargetResource, D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, ResourceStates.Present, ResourceStates.RenderTarget)
        };

        _commandList.Reset(currentFrameContext.CommandAllocator, null);
        _commandList.ResourceBarrier(barrier);
        _commandList.SetRenderTargets(currentFrameContext.MainRenderTargetDescriptor, null);
        _commandList.SetDescriptorHeaps(_shaderDescriptorHeap);

        using var drawData = ImGui.GetDrawData();
        ImGui.ImGuiImplDX12RenderDrawData(drawData, new((void*)_commandList.NativePointer));

        barrier.Transition = new(
            currentFrameContext.MainRenderTargetResource, D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, ResourceStates.RenderTarget, ResourceStates.Present);

        _commandList.ResourceBarrier(barrier);
        _commandList.Close();

        _commandQueue.ExecuteCommandList(_commandList);
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

        Log.Info($"[DX12 ResizeBuffers] Window Handle {windowHandle:X}");

        if (!IsTargetWindowHandle(windowHandle))
        {
            Log.Info($"[DX12 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGui.ImGuiImplDX12InvalidateDeviceObjects();
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
            Log.Info($"[DX12 ResizeBuffers] Discarding window handle {windowHandle:X} due to mismatch");
            return;
        }

        ImGui.ImGuiImplDX12CreateDeviceObjects();

        _ = InitImGuiDX12Internal(swapChain, out _);
    }
}