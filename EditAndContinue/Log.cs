namespace EditAndContinue;

internal static class Log
{
    internal static void Debug(object data) => UnityEngine.Debug.Log($"[Debug] {data}");
    internal static void Error(object data) => UnityEngine.Debug.LogError($"[Error] {data}");
    internal static void Fatal(object data) => UnityEngine.Debug.LogError($"[Fatal] {data}");
    internal static void Info(object data) => UnityEngine.Debug.Log($"[Info] {data}");
    internal static void Message(object data) => UnityEngine.Debug.Log($"[Message] {data}");
    internal static void Warning(object data) => UnityEngine.Debug.LogWarning($"[Warning] {data}");
}