using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MonoMod.RuntimeDetour;
using NativeMemory;
using PortableExecutable;
using SharpDX.DXGI;

namespace RendererFinder.Renderers;

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
        var dxgiModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(p => p?.ModuleName != null && p.ModuleName.ToLowerInvariant().Contains("dxgi"));
        if (dxgiModule == null)
        {
            Log.Error("dxgiModule == null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dxgiModule.FileName))
        {
            Log.Error("string.IsNullOrWhiteSpace(dxgiModule.FileName)");
            return false;
        }

        var peReader = PEReader.FromFilePath(dxgiModule.FileName);
        if (peReader == null)
        {
            return false;
        }

        var dxgiPdbReader = new PdbReader(peReader);

        var cacheDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnityMod.DearImGui", "pdb");
        dxgiPdbReader.FindOrDownloadPdb(cacheDirectoryPath);

        var swapChainPresentFunctionOffset = dxgiPdbReader.FindFunctionOffset(new BytePattern[] { Encoding.ASCII.GetBytes("CDXGISwapChain::Present\0") });
        if (swapChainPresentFunctionOffset == IntPtr.Zero)
        {
            Log.Error("swapChainPresentFunctionOffset == IntPtr.Zero");
            return false;
        }
        var swapChainPresentFunctionPtr = dxgiModule.BaseAddress.Add(swapChainPresentFunctionOffset);

        _detourPresent = new NativeDetour(
            swapChainPresentFunctionPtr,
            Marshal.GetFunctionPointerForDelegate(new CDXGISwapChainPresentDelegate(SwapChainPresentHook)),
            new NativeDetourConfig { ManualApply = true });
        _originalPresent = _detourPresent.GenerateTrampoline<CDXGISwapChainPresentDelegate>();
        _detourPresent.Apply();

        var swapChainResizeBuffersFunctionOffset = dxgiPdbReader.FindFunctionOffset(new BytePattern[] { Encoding.ASCII.GetBytes("CDXGISwapChain::ResizeBuffers\0") });
        if (swapChainResizeBuffersFunctionOffset == IntPtr.Zero)
        {
            Log.Error("swapChainResizeBuffersFunctionOffset == IntPtr.Zero");
            return false;
        }
        var swapChainResizeBuffersFunctionPtr = dxgiModule.BaseAddress.Add(swapChainResizeBuffersFunctionOffset);

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
        _detourPresent.Dispose();
        _originalPresent = null;

        _onPresentAction = null;
    }

    private static IntPtr SwapChainPresentHook(IntPtr self, uint syncInterval, uint flags)
    {
        using var swapChain = new SwapChain(self);

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
        using var swapChain = new SwapChain(swapchainPtr);

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