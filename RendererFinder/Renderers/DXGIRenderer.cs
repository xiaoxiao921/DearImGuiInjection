using System;
using System.Runtime.InteropServices;
using CppInterop;
using MonoMod.RuntimeDetour;
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

public class DXGIRenderer : IRenderer
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr CDXGISwapChainPresentDelegate(IntPtr self, uint syncInterval, uint flags);
    private static NativeDetour _detourPresent;
    private static CDXGISwapChainPresentDelegate _originalPresent;

    public static event Action<SwapChain, uint, uint> OnPresent { add { _onPresentAction += value; } remove { _onPresentAction -= value; } }
    private static event Action<SwapChain, uint, uint> _onPresentAction;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr CDXGISwapChainResizeBuffersDelegate(IntPtr self, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags);
    private static NativeDetour _detourResizeBuffers;
    private static CDXGISwapChainResizeBuffersDelegate _originalResizeBuffers;

    public static event Action<SwapChain, uint, uint, uint, Format, uint> PreResizeBuffers { add { _preResizeBuffers += value; } remove { _preResizeBuffers -= value; } }
    private static event Action<SwapChain, uint, uint, uint, Format, uint> _preResizeBuffers;

    public static event Action<SwapChain, uint, uint, uint, Format, uint> PostResizeBuffers { add { _postResizeBuffers += value; } remove { _postResizeBuffers -= value; } }
    private static event Action<SwapChain, uint, uint, uint, Format, uint> _postResizeBuffers;

    public bool Init()
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

        Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, desc, out var dx11Device, out var dxgiSwapChain);
        var DXGIVTable = VirtualFunctionTable.FromObject(dxgiSwapChain.NativePointer, Enum.GetNames(typeof(IDXGISwapChain)).Length);
        var swapChainPresentFunctionPtr = DXGIVTable.TableEntries[(int)IDXGISwapChain.Present].FunctionPointer;
        var swapChainResizeBuffersFunctionPtr = DXGIVTable.TableEntries[(int)IDXGISwapChain.ResizeBuffers].FunctionPointer;

        dxgiSwapChain.Dispose();
        dx11Device.Dispose();

        Windows.User32.DestroyWindow(windowHandle);

        _detourPresent = new NativeDetour(
            swapChainPresentFunctionPtr,
            Marshal.GetFunctionPointerForDelegate(new CDXGISwapChainPresentDelegate(SwapChainPresentHook)),
            new NativeDetourConfig { ManualApply = true });
        _originalPresent = _detourPresent.GenerateTrampoline<CDXGISwapChainPresentDelegate>();
        _detourPresent.Apply();

        _detourResizeBuffers = new NativeDetour(
            swapChainResizeBuffersFunctionPtr,
            Marshal.GetFunctionPointerForDelegate(new CDXGISwapChainResizeBuffersDelegate(SwapChainResizeBuffersHook)),
            new NativeDetourConfig { ManualApply = true });
        _originalResizeBuffers = _detourResizeBuffers.GenerateTrampoline<CDXGISwapChainResizeBuffersDelegate>();
        _detourResizeBuffers.Apply();

        return true;
    }

    public void Dispose()
    {
        _detourResizeBuffers.Dispose();
        _originalResizeBuffers = null;

        _detourPresent.Dispose();
        _originalPresent = null;

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

        return _originalPresent(self, syncInterval, flags);
    }

    private IntPtr SwapChainResizeBuffersHook(IntPtr swapchainPtr, uint bufferCount, uint width, uint height, Format newFormat, uint swapchainFlags)
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

        var result = _originalResizeBuffers(swapchainPtr, bufferCount, width, height, newFormat, swapchainFlags);

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