using System;
using DearImGuiInjection.Backends;
using DearImguiSharp;
using RendererFinder.Renderers;

namespace DearImGuiInjection;

public static class DearImGuiInjection
{
    /// <summary>
    /// True if the injection has been initialized, else false.
    /// </summary>
    public static bool Initialized { get; internal set; }

    public static ImGuiContext Context { get; internal set; }

    public static ImGuiIO IO { get; internal set; }

    /// <summary>
    /// True if the Dear ImGui GUI cursor is visible
    /// </summary>
    public static bool IsCursorVisible { get; internal set; } = true;

    /// <summary>
    /// Key for switching the cursor visibility.
    /// </summary>
    public static VirtualKey CursorVisibilityToggle { get; internal set; } = VirtualKey.Insert;

    /// <summary>
    /// User supplied function to render the Dear ImGui UI.
    /// </summary>
    public static event Action Render { add { RenderAction += value; } remove { RenderAction -= value; } }
    internal static Action RenderAction;

    internal static void Init()
    {
        if (RendererFinder.RendererFinder.Init())
        {
            InitImplementationFromRendererKind(RendererFinder.RendererFinder.RendererKind);
        }
    }

    internal static void Dispose()
    {
        if (!Initialized)
        {
            return;
        }

        DisposeImplementationFromRendererKind(RendererFinder.RendererFinder.RendererKind);
    }

    private static void InitImplementationFromRendererKind(RendererKind rendererKind)
    {
        switch (rendererKind)
        {
            case RendererKind.None:
                break;
            case RendererKind.DXGI:
                ImGuiDXGI.Init();
                break;
        }
    }

    private static void DisposeImplementationFromRendererKind(RendererKind rendererKind)
    {
        switch (rendererKind)
        {
            case RendererKind.None:
                break;
            case RendererKind.DXGI:
                ImGuiDXGI.Dispose();
                break;
        }
    }
}