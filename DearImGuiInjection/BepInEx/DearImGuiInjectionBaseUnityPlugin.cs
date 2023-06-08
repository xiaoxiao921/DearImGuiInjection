using BepInEx;
using DearImguiSharp;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImguiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));

        DearImGuiInjection.Init();

        DearImGuiInjection.Render += DearImGuiInjection_Render;
    }

    private static bool open = true;
    private void DearImGuiInjection_Render()
    {
        ImGui.ShowDemoWindow(ref open);
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        DearImGuiInjection.Dispose();
    }
}
