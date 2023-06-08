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
    private delegate long CDXGISwapChainPresentDelegate(IntPtr self, uint SyncInterval, uint Flags);

    public static event Action<SwapChain, uint, uint> OnPresent { add { _onPresentAction += value; } remove { _onPresentAction -= value; } }
    private static event Action<SwapChain, uint, uint> _onPresentAction;

    private static NativeDetour _detour;
    private static CDXGISwapChainPresentDelegate _original;

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

        _detour = new NativeDetour(
            swapChainPresentFunctionPtr,
            Marshal.GetFunctionPointerForDelegate(new CDXGISwapChainPresentDelegate(SwapChainPresentHook)),
            new NativeDetourConfig { ManualApply = true });
        _original = _detour.GenerateTrampoline<CDXGISwapChainPresentDelegate>();
        _detour.Apply();

        return true;
    }

    public void Dispose()
    {
        _detour.Dispose();
        _original = null;

        _onPresentAction = null;
    }

    private static long SwapChainPresentHook(IntPtr self, uint syncInterval, uint flags)
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

        return _original(self, syncInterval, flags);
    }
}