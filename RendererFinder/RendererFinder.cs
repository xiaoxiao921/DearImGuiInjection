using RendererFinder.Renderers;

namespace RendererFinder;

public static class RendererFinder
{
    public static RendererKind RendererKind { get; private set; }

    public static readonly RendererKind[] AvailableRenderers = { RendererKind.DXGI };

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

    private static DXGIRenderer GetImplementationFromRendererKind(RendererKind rendererKind) =>
        rendererKind switch
        {
            RendererKind.DXGI => new DXGIRenderer(),
            _ => null,
        };
}