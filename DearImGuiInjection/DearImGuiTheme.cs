using System;
using System.IO;
using System.Runtime.InteropServices;
using DearImguiSharp;

namespace DearImGuiInjection;

public static class DearImGuiTheme
{
    public static ImGuiStyle Style { get; private set; }

    private static ImVec2 ImVec2(float x, float y) => new() { X = x, Y = y };
    private static ImVec4 ImVec4(float x, float y, float z, float w) => new() { X = x, Y = y, Z = z, W = w };

    private static readonly ImVec4 BGColor = ImVec4(0.117f, 0.113f, 0.172f, .75f);
    private static readonly ImVec4 Primary = ImVec4(0.172f, 0.380f, 0.909f, 1f);
    private static readonly ImVec4 Secondary = ImVec4(0.443f, 0.654f, 0.819f, 1f);
    private static readonly ImVec4 WhiteBroken = ImVec4(0.792f, 0.784f, 0.827f, 1f);

    internal static void Init()
    {
        SetupStyle();

        SetupCustomFont();
    }

    private static void SetupStyle()
    {
        Style = ImGui.GetStyle();
        Style.WindowPadding = ImVec2(10f, 10f);
        Style.PopupRounding = 0f;
        Style.FramePadding = ImVec2(8f, 4f);
        Style.ItemSpacing = ImVec2(10f, 8f);
        Style.ItemInnerSpacing = ImVec2(6f, 6f);
        Style.TouchExtraPadding = ImVec2(0f, 0f);
        Style.IndentSpacing = 21f;
        Style.ScrollbarSize = 15f;
        Style.GrabMinSize = 8f;
        Style.WindowBorderSize = 0f;
        Style.ChildBorderSize = 0f;
        Style.PopupBorderSize = 0f;
        Style.FrameBorderSize = 0f;
        Style.TabBorderSize = 0f;
        Style.WindowRounding = 5f;
        Style.ChildRounding = 2f;
        Style.FrameRounding = 3f;
        Style.ScrollbarRounding = 3f;
        Style.GrabRounding = 0f;
        Style.TabRounding = 3f;
        Style.WindowTitleAlign = ImVec2(0.5f, 0.5f);
        Style.ButtonTextAlign = ImVec2(0.5f, 0.5f);
        Style.DisplaySafeAreaPadding = ImVec2(3f, 3f);

        var colors = Style.Colors;
        colors[(int)ImGuiCol.Text] = ImVec4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = ImVec4(1.00f, 0.90f, 0.19f, 1.00f);
        colors[(int)ImGuiCol.WindowBg] = BGColor;
        colors[(int)ImGuiCol.ChildBg] = ImVec4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.PopupBg] = ImVec4(0.08f, 0.08f, 0.08f, 0.94f);
        colors[(int)ImGuiCol.Border] = ImVec4(0.30f, 0.30f, 0.30f, 0.50f);
        colors[(int)ImGuiCol.BorderShadow] = ImVec4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.FrameBg] = ImVec4(0.21f, 0.21f, 0.21f, 0.54f);
        colors[(int)ImGuiCol.FrameBgHovered] = ImVec4(0.21f, 0.21f, 0.21f, 0.78f);
        colors[(int)ImGuiCol.FrameBgActive] = ImVec4(0.28f, 0.27f, 0.27f, 0.54f);
        colors[(int)ImGuiCol.TitleBg] = ImVec4(0.17f, 0.17f, 0.17f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = ImVec4(0.19f, 0.19f, 0.19f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = ImVec4(0.00f, 0.00f, 0.00f, 0.51f);
        colors[(int)ImGuiCol.MenuBarBg] = ImVec4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarBg] = colors[(int)ImGuiCol.WindowBg];
        colors[(int)ImGuiCol.ScrollbarGrab] = Primary;
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = Secondary;
        colors[(int)ImGuiCol.ScrollbarGrabActive] = Primary;
        colors[(int)ImGuiCol.CheckMark] = ImVec4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = ImVec4(0.34f, 0.34f, 0.34f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = ImVec4(0.39f, 0.38f, 0.38f, 1.00f);
        colors[(int)ImGuiCol.Button] = Primary;
        colors[(int)ImGuiCol.ButtonHovered] = Secondary;
        colors[(int)ImGuiCol.ButtonActive] = colors[(int)ImGuiCol.ButtonHovered];
        colors[(int)ImGuiCol.Header] = ImVec4(0.37f, 0.37f, 0.37f, 0.31f);
        colors[(int)ImGuiCol.HeaderHovered] = ImVec4(0.38f, 0.38f, 0.38f, 0.37f);
        colors[(int)ImGuiCol.HeaderActive] = ImVec4(0.37f, 0.37f, 0.37f, 0.51f);
        colors[(int)ImGuiCol.Separator] = ImVec4(0.38f, 0.38f, 0.38f, 0.50f);
        colors[(int)ImGuiCol.SeparatorHovered] = ImVec4(0.46f, 0.46f, 0.46f, 0.50f);
        colors[(int)ImGuiCol.SeparatorActive] = ImVec4(0.46f, 0.46f, 0.46f, 0.64f);
        colors[(int)ImGuiCol.ResizeGrip] = WhiteBroken;
        colors[(int)ImGuiCol.ResizeGripHovered] = ImVec4(1f, 1f, 1f, 1.00f);
        colors[(int)ImGuiCol.ResizeGripActive] = WhiteBroken;
        colors[(int)ImGuiCol.Tab] = ImVec4(0.21f, 0.21f, 0.21f, 0.86f);
        colors[(int)ImGuiCol.TabHovered] = ImVec4(0.27f, 0.27f, 0.27f, 0.86f);
        colors[(int)ImGuiCol.TabActive] = ImVec4(0.34f, 0.34f, 0.34f, 0.86f);
        colors[(int)ImGuiCol.TabUnfocused] = ImVec4(0.10f, 0.10f, 0.10f, 0.97f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = ImVec4(0.15f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.PlotLines] = ImVec4(0.61f, 0.61f, 0.61f, 1.00f);
        colors[(int)ImGuiCol.PlotLinesHovered] = ImVec4(1.00f, 0.43f, 0.35f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogram] = ImVec4(0.90f, 0.70f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogramHovered] = ImVec4(1.00f, 0.60f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TextSelectedBg] = ImVec4(0.26f, 0.59f, 0.98f, 0.35f);
        colors[(int)ImGuiCol.DragDropTarget] = ImVec4(1.00f, 1.00f, 0.00f, 0.90f);
        colors[(int)ImGuiCol.NavHighlight] = ImVec4(0.26f, 0.59f, 0.98f, 1.00f);
        colors[(int)ImGuiCol.NavWindowingHighlight] = ImVec4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = ImVec4(0.80f, 0.80f, 0.80f, 0.20f);
        colors[(int)ImGuiCol.ModalWindowDimBg] = ImVec4(0.80f, 0.80f, 0.80f, 0.35f);
        Style.Colors = colors;
    }

    private static unsafe void SetupCustomFont()
    {
        var fontPath = Path.Combine(DearImGuiInjection.AssetsFolderPath, "Fonts", "Comfortaa-Medium.ttf");
        var fontByteArray = File.ReadAllBytes(fontPath);
        var fontByteArrayPtr = NativeMemory.Allocator.AllocAndCopy(fontByteArray);

        var fontCfg = ImGui.ImFontConfigImFontConfig();
        fontCfg.FontDataOwnedByAtlas = false;
        var fontPtr = ImGui.__Internal.ImFontAtlasAddFontFromMemoryTTF(DearImGuiInjection.IO.Fonts.__Instance, fontByteArrayPtr, fontByteArray.Length, 15, fontCfg.__Instance, null);
        if (fontPtr != IntPtr.Zero)
        {
            DearImGuiInjection.IO.FontDefault = new((void*)fontPtr);
        }
        Marshal.FreeHGlobal(fontByteArrayPtr);
    }
}
