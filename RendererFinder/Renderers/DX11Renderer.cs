using System;
using System.Collections.Generic;
using CppInterop;
using Reloaded.Hooks;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace RendererFinder.Renderers;

/// <summary>
/// Contains a full list of IDXGISwapChain functions to be used
/// as an indexer into the SwapChain Virtual Function Table entries.
/// </summary>
public enum IDXGISwapChain
{
    // IUnknown
    QueryInterface = 0,
    AddRef = 1,
    Release = 2,

    // IDXGIObject
    SetPrivateData = 3,
    SetPrivateDataInterface = 4,
    GetPrivateData = 5,
    GetParent = 6,

    // IDXGIDeviceSubObject
    GetDevice = 7,

    // IDXGISwapChain
    Present = 8,
    GetBuffer = 9,
    SetFullscreenState = 10,
    GetFullscreenState = 11,
    GetDesc = 12,
    ResizeBuffers = 13,
    ResizeTarget = 14,
    GetContainingOutput = 15,
    GetFrameStatistics = 16,
    GetLastPresentCount = 17,
}

public class DX11Renderer : IRenderer
{
    // https://github.com/BepInEx/BepInEx/blob/master/Runtimes/Unity/BepInEx.Unity.IL2CPP/Hook/INativeDetour.cs#L54
    // Workaround for CoreCLR collecting all delegates
    private static List<object> _cache = new();

    [Reloaded.Hooks.Definitions.X64.Function(Reloaded.Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Reloaded.Hooks.Definitions.X86.Function(Reloaded.Hooks.Definitions.X86.CallingConventions.Stdcall)]
    private delegate IntPtr CDXGISwapChainPresentDelegate(IntPtr self, uint syncInterval, uint flags);

    private static CDXGISwapChainPresentDelegate _swapChainPresentHookDelegate = new(SwapChainPresentHook);
    private static Hook<CDXGISwapChainPresentDelegate> _swapChainPresentHook;

    public static event Action<SwapChain, uint, uint> OnPresent { add { _onPresentAction += value; } remove { _onPresentAction -= value; } }
    private static Action<SwapChain, uint, uint> _onPresentAction;

    [Reloaded.Hooks.Definitions.X64.Function(Reloaded.Hooks.Definitions.X64.CallingConventions.Microsoft)]
    [Reloaded.Hooks.Definitions.X86.Function(Reloaded.Hooks.Definitions.X86.CallingConventions.Stdcall)]
    private delegate IntPtr CDXGISwapChainResizeBuffersDelegate(IntPtr self, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags);

    private static CDXGISwapChainResizeBuffersDelegate _swapChainResizeBuffersHookDelegate = new(SwapChainResizeBuffersHook);
    private static Hook<CDXGISwapChainResizeBuffersDelegate> _swapChainResizeBuffersHook;

    public static event Action<SwapChain, uint, uint, uint, Format, uint> PreResizeBuffers { add { _preResizeBuffers += value; } remove { _preResizeBuffers -= value; } }
    private static Action<SwapChain, uint, uint, uint, Format, uint> _preResizeBuffers;

    public static event Action<SwapChain, uint, uint, uint, Format, uint> PostResizeBuffers { add { _postResizeBuffers += value; } remove { _postResizeBuffers -= value; } }
    private static Action<SwapChain, uint, uint, uint, Format, uint> _postResizeBuffers;

    public unsafe bool Init()
    {
        var windowHandle = Windows.User32.CreateFakeWindow();

        var desc = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(500, 300, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = windowHandle,
            SampleDescription = new SampleDescription(1, 0),
            Usage = Usage.RenderTargetOutput
        };

        Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out var device, out var swapChain);
        var swapChainVTable = VirtualFunctionTable.FromObject((nuint)(nint)swapChain.NativePointer, (nuint)Enum.GetNames(typeof(IDXGISwapChain)).Length);
        var swapChainPresentFunctionPtr = swapChainVTable.TableEntries[(int)IDXGISwapChain.Present].FunctionPointer;
        var swapChainResizeBuffersFunctionPtr = swapChainVTable.TableEntries[(int)IDXGISwapChain.ResizeBuffers].FunctionPointer;

        swapChain.Dispose();
        device.Dispose();

        Windows.User32.DestroyWindow(windowHandle);

        {
            _cache.Add(_swapChainPresentHookDelegate);

            _swapChainPresentHook = new(_swapChainPresentHookDelegate, swapChainPresentFunctionPtr);
            _swapChainPresentHook.Activate();
        }

        {
            _cache.Add(_swapChainResizeBuffersHookDelegate);

            _swapChainResizeBuffersHook = new(_swapChainResizeBuffersHookDelegate, swapChainResizeBuffersFunctionPtr);
            _swapChainResizeBuffersHook.Activate();
        }

        return true;
    }

    public void Dispose()
    {
        _swapChainResizeBuffersHook?.Disable();
        _swapChainResizeBuffersHook = null;

        _swapChainPresentHook?.Disable();
        _swapChainPresentHook = null;

        _onPresentAction = null;
    }

    private static IntPtr SwapChainPresentHook(IntPtr self, uint syncInterval, uint flags)
    {
        var swapChain = new SwapChain(self);

        if (_onPresentAction != null)
        {
            foreach (Action<SwapChain, uint, uint> item in _onPresentAction.GetInvocationList())
            {
                try
                {
                    item(swapChain, syncInterval, flags);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        return _swapChainPresentHook.OriginalFunction(self, syncInterval, flags);
    }

    private static IntPtr SwapChainResizeBuffersHook(IntPtr swapchainPtr, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags)
    {
        var swapChain = new SwapChain(swapchainPtr);

        if (_preResizeBuffers != null)
        {
            foreach (Action<SwapChain, uint, uint, uint, Format, uint> item in _preResizeBuffers.GetInvocationList())
            {
                try
                {
                    item(swapChain, bufferCount, width, height, newFormat, swapchainFlags);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        var result = _swapChainResizeBuffersHook.OriginalFunction(swapchainPtr, bufferCount, width, height, newFormat, swapchainFlags);

        if (_postResizeBuffers != null)
        {
            foreach (Action<SwapChain, uint, uint, uint, Format, uint> item in _postResizeBuffers.GetInvocationList())
            {
                try
                {
                    item(swapChain, bufferCount, width, height, newFormat, swapchainFlags);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        return result;
    }
}