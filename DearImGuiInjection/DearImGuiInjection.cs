using System;
using System.IO;
using System.Runtime.InteropServices;
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

    public static string ImGuiIniConfigPath { get; private set; }
    private const string IniFileName = "DearImGuiInjection_imgui.ini";

    public static string AssetsFolderPath { get; private set; }

    /// <summary>
    /// True if the Dear ImGui GUI cursor is visible
    /// </summary>
    public static bool IsCursorVisible { get; internal set; } = false;

    /// <summary>
    /// Key for switching the cursor visibility.
    /// </summary>
    public static VirtualKey CursorVisibilityToggle { get; internal set; } = VirtualKey.Insert;

    public static ImGuiStyle Style { get; private set; }

    /// <summary>
    /// User supplied function to render the Dear ImGui UI.
    /// </summary>
    public static event Action Render { add { RenderAction += value; } remove { RenderAction -= value; } }
    internal static Action RenderAction;

    internal static void Init(string imguiIniConfigDirectoryPath, string assetsFolder)
    {
        if (RendererFinder.RendererFinder.Init())
        {
            ImGuiIniConfigPath = Path.Combine(imguiIniConfigDirectoryPath, IniFileName);
            AssetsFolderPath = assetsFolder;

            InitImplementationFromRendererKind(RendererFinder.RendererFinder.RendererKind);
        }
    }

    internal static unsafe void InitImGui()
    {
        Context = ImGui.CreateContext(null);
        IO = ImGui.GetIO();

        ((ImGuiIO.__Internal*)IO.__Instance)->IniFilename = Marshal.StringToHGlobalAnsi(ImGuiIniConfigPath);

        DearImGuiTheme.Init();
    }

    internal static unsafe void Dispose()
    {
        if (!Initialized)
        {
            return;
        }

        DisposeImplementationFromRendererKind(RendererFinder.RendererFinder.RendererKind);

        RenderAction = null;

        Marshal.FreeHGlobal(((ImGuiIO.__Internal*)IO.__Instance)->IniFilename);
        IO = null;

        ImGui.DestroyContext(Context);
        Context = null;

        Initialized = false;
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

    internal static void ToggleCursor()
    {
        IsCursorVisible ^= true;
        UpdateCursorVisibility();
    }

    internal static void UpdateCursorVisibility()
    {
        if (IsCursorVisible)
        {
            IO.MouseDrawCursor = true;
            IO.ConfigFlags &= ~(int)ImGuiConfigFlags.NoMouse;
        }
        else
        {
            IO.MouseDrawCursor = false;
            IO.ConfigFlags |= (int)ImGuiConfigFlags.NoMouse;
        }
    }
}