# DearImGuiInjection

Inject [Dear ImGui](https://github.com/ocornut/imgui) into any process with C#.

## Usage

The keybind for bringing up the cursor for interaction is by default the `Insert` key, which can be modified in the configuration file.

## Mod Developers

Download this package and add a reference to `DearImguiSharp.dll` and `DearImGuiInjection.dll` in your C# project.

Above your `BaseUnityPlugin` class definition
```csharp
[BepInDependency(DearImGuiInjection.Metadata.GUID)]
```

```csharp
DearImGuiInjection.DearImGuiInjection.Render += MyUI;
```

```csharp
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
```

Make sure to only interact with anything `UnityEngine` related from its main thread, easily doable through `UnityMainThreadDispatcher`

```csharp
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
```

## Credits

[Sewer56](https://github.com/Sewer56)