using System.Collections.Generic;
using BepInEx;
using DearImGuiInjection;
using DearImGuiInjection.BepInEx;
using DearImguiSharp;
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
            var dummy = true;
            ImGui.ShowDemoWindow(ref dummy);

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("MainBar", true))
                {
                    if (ImGui.MenuItemBool("MyTestPlugin", null, false, true))
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
                    if (ImGui.MenuItemBool("MyTestPlugin2", null, false, true))
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


                if (ImGui.Button("Click me", Constants.DefaultVector2))
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

    private void OnDisable()
    {
        DearImGuiInjection.DearImGuiInjection.Render -= MyUI;
    }
}