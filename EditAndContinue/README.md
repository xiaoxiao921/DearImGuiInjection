# EditAndContinue

Place the new version of the dll you're developing/editing in the `BepInEx/scripts` folder.

By default, the keybind to trigger a reload is `F2`.

This is an alternative to the [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug/blob/master/src/ScriptEngine/ScriptEngine.cs).

The way this plugin works is that all the methods of the previous assembly are replaced by the methods of the script folder assembly.
