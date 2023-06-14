using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using RendererFinder.Renderers;

namespace RendererFinder;

public static class RendererFinder
{
    public static RendererKind RendererKind { get; private set; }

    public static readonly RendererKind[] AvailableRenderers = { RendererKind.D3D11, RendererKind.D3D12 };

    public static bool Init()
    {
        if (RendererKind != RendererKind.None)
        {
            return true;
        }

        foreach (var availableRenderer in AvailableRenderers)
        {
            var renderer = GetImplementationFromRendererKind(availableRenderer);
            if (renderer != null && renderer.Init())
            {
                RendererKind = availableRenderer;

                return true;
            }
        }

        return false;
    }

    public static void Dispose()
    {
        RendererKind = RendererKind.None;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IRenderer NewDX11Renderer()
    {
        var d3d11ModuleIsHere = false;
        var d3d12ModuleIsHere = false;
        foreach (var processModule in Process.GetCurrentProcess().Modules.Cast<ProcessModule>())
        {
            if (processModule?.ModuleName != null)
            {
                var moduleName = processModule.ModuleName.ToLowerInvariant();
                if (moduleName.Contains("d3d11"))
                {
                    d3d11ModuleIsHere = true;
                }
                else if (moduleName.Contains("d3d12"))
                {
                    d3d12ModuleIsHere = true;
                }
            }
        }

        if (!d3d11ModuleIsHere || d3d12ModuleIsHere)
        {
            return null;
        }

        return new DX11Renderer();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IRenderer NewDX12Renderer()
    {
        var d3d12ModuleIsHere = false;
        foreach (var processModule in Process.GetCurrentProcess().Modules.Cast<ProcessModule>())
        {
            if (processModule?.ModuleName != null)
            {
                var moduleName = processModule.ModuleName.ToLowerInvariant();
                if (moduleName.Contains("d3d12"))
                {
                    d3d12ModuleIsHere = true;
                }
            }
        }

        if (!d3d12ModuleIsHere)
        {
            return null;
        }

        return new DX12Renderer();
    }

    private static IRenderer GetImplementationFromRendererKind(RendererKind rendererKind) =>
        rendererKind switch
        {
            RendererKind.D3D11 => NewDX11Renderer(),
            RendererKind.D3D12 => NewDX12Renderer(),
            _ => null,
        };
}