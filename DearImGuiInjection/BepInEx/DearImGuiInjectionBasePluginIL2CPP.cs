#if NET6

using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DearImGuiInjection.Backends;
using Il2CppInterop.Runtime.Injection;
using MonoMod.RuntimeDetour;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DearImGuiInjection.BepInEx;

internal static class LogInitier
{
    internal static void Init(ManualLogSource log)
    {
        Log.Init(new BepInExLog(log));
    }
}

[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class DearImGuiInjectionBasePluginIL2CPP : BasePlugin
{
    private System.Reflection.MethodInfo _eventSystemUpdate;
    private Hook _eventSystemUpdateHook;

    private static GameObject UnityMainThreadDispatcherHolder;
    private static UnityMainThreadDispatcher UnityMainThreadDispatcherInstance;

    public override void Load()
    {
        LogInitier.Init(Log);

        var imguiIniConfigDirectoryPath = Paths.ConfigPath;

        var myPluginInfo = IL2CPPChainloader.Instance.Plugins[Metadata.GUID];
        var assetsFolder = Path.Combine(Path.GetDirectoryName(myPluginInfo.Location), "Assets");

        var cursorVisibilityConfig = new BepInExConfigEntry<VirtualKey>(
            Config.Bind("Keybinds", "CursorVisibility",
            DearImGuiInjection.CursorVisibilityToggleDefault,
            "Key for switching the cursor visibility."));
        DearImGuiInjection.Init(imguiIniConfigDirectoryPath, assetsFolder, cursorVisibilityConfig);
        SetupIgnoreUIObjectsWhenImGuiCursorIsVisible();


        ClassInjector.RegisterTypeInIl2Cpp<UnityMainThreadDispatcher>();
        UnityMainThreadDispatcherHolder = new("DearImGui.UnityMainThreadDispatcher");
        GameObject.DontDestroyOnLoad(UnityMainThreadDispatcherHolder);
        UnityMainThreadDispatcherHolder.hideFlags |= HideFlags.HideAndDontSave;
        UnityMainThreadDispatcherInstance = UnityMainThreadDispatcherHolder.AddComponent<UnityMainThreadDispatcher>();
    }

    private void SetupIgnoreUIObjectsWhenImGuiCursorIsVisible()
    {
        try
        {
            var allFlags = (System.Reflection.BindingFlags)(-1);
            _eventSystemUpdate = typeof(EventSystem).GetMethod(nameof(EventSystem.Update), allFlags);
            //_eventSystemUpdateHook = new Hook(_eventSystemUpdate, IgnoreUIObjectsWhenImGuiCursorIsVisible);
        }
        catch (Exception e)
        {
            Log.LogError(e);
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

#endif