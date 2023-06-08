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

    private void DearImGuiInjection_Render()
    {
        var dummy = true;
        if (DearImGuiInjection.IsCursorVisible)
        {
            ImGui.ShowDemoWindow(ref dummy);
        }
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        DearImGuiInjection.Dispose();
    }
}