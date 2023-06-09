using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using DearImGuiInjection.Backends;
using MonoMod.RuntimeDetour;

namespace DearImGuiInjection.BepInEx;

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImGuiInjectionBaseUnityPlugin : BaseUnityPlugin
{
    private Type _eventSystemType;
    private MethodInfo _eventSystemUpdate;
    private Hook _eventSystemUpdateHook;

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

        SetupIgnoreUIObjectsWhenImGuiCursorIsVisible();

        gameObject.AddComponent<UnityMainThreadDispatcher>();
    }

    private void SetupIgnoreUIObjectsWhenImGuiCursorIsVisible()
    {
        try
        {
            var allFlags = (BindingFlags)(-1);
            var unityEngineUIDll = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == "UnityEngine.UI");
            _eventSystemType = unityEngineUIDll.GetType("UnityEngine.EventSystems.EventSystem");
            _eventSystemUpdate = _eventSystemType.GetMethod("Update", allFlags);
            _eventSystemUpdateHook = new Hook(_eventSystemUpdate, IgnoreUIObjectsWhenImGuiCursorIsVisible);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    private static void IgnoreUIObjectsWhenImGuiCursorIsVisible(Action<object> orig, object self)
    {
        if (DearImGuiInjection.IsCursorVisible)
        {
            return;
        }

        orig(self);
    }

    private void OnDestroy()
    {
        _eventSystemUpdateHook?.Dispose();

        DearImGuiInjection.Dispose();
    }
}