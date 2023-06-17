#if NETSTANDARD2_0 || NET462

using System.Collections.Generic;
using BepInEx;
using DearImGuiInjection.BepInEx;
using ImGuiNET;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace TestPlugin;

public class TestMonoBeh : MonoBehaviour
{
    public int A = 4;
}

[BepInDependency(DearImGuiInjection.Metadata.GUID)]
[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal class TestPlugin : BaseUnityPlugin
{
    private static bool _isMyUIOpen = true;

    private static List<Hook> Hooks = new();

    private void Awake()
    {
        Log.Init(new BepInExLog(Logger));
    }

    private void OnEnable()
    {
        DearImGuiInjection.DearImGuiInjection.Render += MyUI;
    }

    private void Update()
    {
        UpdateMethod();
    }


    private static GameObject goTest;
    private static void UpdateMethod()
    {
        if (Input.GetKey("f3"))
        {
            Log.Info("WOWEEEEEEEE");

            if (!goTest)
            {
                goTest = new GameObject("TestGameObjectReload");
                var testMonoBeh = goTest.AddComponent<TestMonoBeh>();
                Log.Info("created goTest, testMonoBeh.A : " + testMonoBeh.A);
            }
            else
            {
                var testMonoBeh = goTest.GetComponent<TestMonoBeh>();
                if (testMonoBeh)
                {
                    Log.Info("got a goTest, testMonoBeh.A : " + testMonoBeh.A);
                }
                else
                {
                    Log.Info("got a goTest, but no testMonoBeh");
                }
            }
        }
    }

    private static void MyUI()
    {
        if (DearImGuiInjection.DearImGuiInjection.IsCursorVisible)
        {
            ImGui.ShowDemoWindow();

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("MainBar"))
                {
                    if (ImGui.MenuItem("MyTestPlugin"))
                    {
                        _isMyUIOpen ^= true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("MainBar", true))
                {
                    if (ImGui.MenuItem("MyTestPlugin2"))
                    {
                        _isMyUIOpen ^= true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }
        }

        if (_isMyUIOpen)
        {
            var dummy2 = true;
            if (ImGui.Begin(Metadata.GUID, ref dummy2, (int)ImGuiWindowFlags.None))
            {
                ImGui.Text("hello there");

                if (ImGui.InputText("lol", buffer_input_text, (uint)buffer_input_text.Length))
                {

                }

                if (ImGui.Button("Click me"))
                {
                    // Interacting with the unity api must be done from the unity main thread
                    // Can just use the dispatcher shipped with the library for that
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        //var go = new GameObject();
                        //go.AddComponent<Stuff>();
                    });
                }
            }

            ImGui.End();
        }
    }

    private static byte[] buffer_input_text = new byte[40];

    private void OnDisable()
    {
        DearImGuiInjection.DearImGuiInjection.Render -= MyUI;
    }
}

#else

using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.UnityEngine;
using Il2CppInterop.Runtime.Injection;
using ImGuiNET;
using MonoMod.RuntimeDetour;
using UnityEngine;

namespace TestPlugin;

public class TestPluginBehaviour : MonoBehaviour
{
    public TestPluginBehaviour(IntPtr ptr) : base(ptr) { }

    private void Update()
    {
        UpdateMethod();
    }

    private static void UpdateMethod()
    {
        if (Input.GetKeyInt(BepInEx.Unity.IL2CPP.UnityEngine.KeyCode.F6))
        {
            Screen.SetResolution(1280, 720, false);
        }

        if (Input.GetKeyInt(BepInEx.Unity.IL2CPP.UnityEngine.KeyCode.F7))
        {
            Screen.SetResolution(1920, 1080, false);
        }
    }
}

internal static class LogInitier
{
    internal static void Init(ManualLogSource log)
    {
        Log.Init(new BepInExLog(log));
    }
}

[BepInDependency(DearImGuiInjection.Metadata.GUID)]
[BepInPlugin(Metadata.GUID, Metadata.Name, Metadata.Version)]
internal unsafe class TestPlugin : BasePlugin
{
    private static bool _isMyUIOpen = true;

    private static List<Hook> Hooks = new();
    private GameObject TestPluginBehaviourHolder;
    private TestPluginBehaviour TestPluginBehaviourInstance;

    public override void Load()
    {
        LogInitier.Init(Log);

        DearImGuiInjection.DearImGuiInjection.Render += MyUI;

        ClassInjector.RegisterTypeInIl2Cpp<TestPluginBehaviour>();
        TestPluginBehaviourHolder = new("TestPluginBehaviourGO");
        GameObject.DontDestroyOnLoad(TestPluginBehaviourHolder);
        TestPluginBehaviourHolder.hideFlags |= HideFlags.HideAndDontSave;
        TestPluginBehaviourInstance = TestPluginBehaviourHolder.AddComponent<TestPluginBehaviour>();
    }

    private static void MyUI()
    {
        if (DearImGuiInjection.DearImGuiInjection.IsCursorVisible)
        {
            ImGui.ShowDemoWindow();

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("MainBar"))
                {
                    if (ImGui.MenuItem("MyTestPlugin"))
                    {
                        _isMyUIOpen ^= true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("MainBar", true))
                {
                    if (ImGui.MenuItem("MyTestPlugin2"))
                    {
                        _isMyUIOpen ^= true;
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMainMenuBar();
            }
        }

        if (_isMyUIOpen)
        {
            var dummy2 = true;
            if (ImGui.Begin(Metadata.GUID, ref dummy2, (int)ImGuiWindowFlags.None))
            {
                ImGui.Text("hello there");

                if (ImGui.InputText("lol", buffer_input_text, (uint)buffer_input_text.Length))
                {

                }

                if (ImGui.Button("Click me"))
                {
                    // Interacting with the unity api must be done from the unity main thread
                    // Can just use the dispatcher shipped with the library for that
                    UnityMainThreadDispatcher.Enqueue(() =>
                    {
                        //var go = new GameObject();
                        //go.AddComponent<Stuff>();
                    });
                }
            }

            ImGui.End();
        }
    }

    private static byte[] buffer_input_text = new byte[40];
}

#endif