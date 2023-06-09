using System.IO;
using BepInEx;
using DearImGuiInjection.Backends;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImGuiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));

        var imguiIniConfigDirectoryPath = Paths.ConfigPath;

        var assetsFolder = Path.Combine(Path.GetDirectoryName(Info.Location), "Assets");

        var cursorVisibilityConfig = new BepInExConfigEntry<VirtualKey>(
            Config.Bind("Keybinds", "CursorVisibility",
            DearImGuiInjection.CursorVisibilityToggleDefault,
            "Key for switching the cursor visibility."));
        DearImGuiInjection.Init(imguiIniConfigDirectoryPath, assetsFolder, cursorVisibilityConfig);

        gameObject.AddComponent<UnityMainThreadDispatcher>();
    }

    private void OnDestroy()
    {
        DearImGuiInjection.Dispose();
    }
}