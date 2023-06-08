using BepInEx;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImguiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));

        DearImGuiInjection.Init();
    }

    private void OnDestroy()
    {
        DearImGuiInjection.Dispose();
    }
}