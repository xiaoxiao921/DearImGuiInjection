using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DearImGuiInjection.Backends;
using DearImGuiInjection.Windows;
using ImGuiNET;
using RendererFinder.Renderers;

namespace DearImGuiInjection;

public static class DearImGuiInjection
{
    /// <summary>
    /// True if the injection has been initialized, else false.
    /// </summary>
    public static bool Initialized { get; internal set; }

    public static IntPtr Context { get; internal set; }

    public static ImGuiIOPtr IO { get; internal set; }

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
    public static IConfigEntry<VirtualKey> CursorVisibilityToggle { get; internal set; }
    internal const VirtualKey CursorVisibilityToggleDefault = VirtualKey.Insert;

    public static ImGuiStyle Style { get; private set; }

    /// <summary>
    /// User supplied function to render the Dear ImGui UI.
    /// </summary>
    public static event Action Render { add { RenderAction += value; } remove { RenderAction -= value; } }
    internal static Action RenderAction;

    internal static void Init(string imguiIniConfigDirectoryPath, string assetsFolder, IConfigEntry<VirtualKey> cursorVisibilityConfig)
    {
        if (RendererFinder.RendererFinder.Init())
        {
            ImGuiIniConfigPath = Path.Combine(imguiIniConfigDirectoryPath, IniFileName);
            AssetsFolderPath = assetsFolder;
            CursorVisibilityToggle = cursorVisibilityConfig;

            InitImplementationFromRendererKind(RendererFinder.RendererFinder.RendererKind);
        }
    }

    internal static unsafe void InitImGui()
    {
        Context = ImGui.CreateContext(null);
        IO = ImGui.GetIO();

        ImGui.GetIO().NativePtr->IniFilename = (byte*)Marshal.StringToHGlobalAnsi(ImGuiIniConfigPath);

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

        Marshal.FreeHGlobal((IntPtr)ImGui.GetIO().NativePtr->IniFilename);
        IO = null;

        ImGui.DestroyContext(Context);
        Context = IntPtr.Zero;

        Initialized = false;
    }

    private static void InitImplementationFromRendererKind(RendererKind rendererKind)
    {
        switch (rendererKind)
        {
            case RendererKind.None:
                break;
            case RendererKind.D3D11:
                InitImGuiDX11();
                break;
            case RendererKind.D3D12:
                InitImGuiDX12();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitImGuiDX11() => ImGuiDX11.Init();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InitImGuiDX12() => ImGuiDX12.Init();

    private static void DisposeImplementationFromRendererKind(RendererKind rendererKind)
    {
        switch (rendererKind)
        {
            case RendererKind.None:
                break;
            case RendererKind.D3D11:
                ImGuiDX11.Dispose();
                break;
            case RendererKind.D3D12:
                ImGuiDX12.Dispose();
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
            int flags = (int)IO.ConfigFlags;
            flags &= ~(int)ImGuiConfigFlags.NoMouse;
            IO.ConfigFlags = (ImGuiConfigFlags)flags;
        }
        else
        {
            IO.MouseDrawCursor = false;
            int flags = (int)IO.ConfigFlags;
            flags |= (int)ImGuiConfigFlags.NoMouse;
            IO.ConfigFlags = (ImGuiConfigFlags)flags;
        }
    }
}