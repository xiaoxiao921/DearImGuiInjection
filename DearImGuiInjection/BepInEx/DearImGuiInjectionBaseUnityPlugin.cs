using System.IO;
using BepInEx;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImGuiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));

        DearImGuiInjection.Init(Paths.ConfigPath, Path.Combine(Path.GetDirectoryName(Info.Location), "Assets"));

        gameObject.AddComponent<UnityMainThreadDispatcher>();
    }

    private void OnDestroy()
    {
        DearImGuiInjection.Dispose();
    }
}