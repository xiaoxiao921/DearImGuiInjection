namespace TestPlugin;

internal interface ILog
{
    internal void Debug(object data);
    internal void Error(object data);
    internal void Fatal(object data);
    internal void Info(object data);
    internal void Message(object data);
    internal void Warning(object data);
}

internal static class Log
{
    private static ILog _log;

    internal static void Init(ILog log)
    {
        _log = log;
    }

#if DEBUG
    // Stateless for hot reload stuff
    internal static void Debug(object data) => UnityEngine.Debug.Log($"[Debug] {data}");
    internal static void Error(object data) => UnityEngine.Debug.LogError($"[Error] {data}");
    internal static void Fatal(object data) => UnityEngine.Debug.LogError($"[Fatal] {data}");
    internal static void Info(object data) => UnityEngine.Debug.Log($"[Info] {data}");
    internal static void Message(object data) => UnityEngine.Debug.Log($"[Message] {data}");
    internal static void Warning(object data) => UnityEngine.Debug.LogWarning($"[Warning] {data}");
#else
    internal static void Debug(object data) => _log.Debug(data);
    internal static void Error(object data) => _log.Error(data);
    internal static void Fatal(object data) => _log.Fatal(data);
    internal static void Info(object data) => _log.Info(data);
    internal static void Message(object data) => _log.Message(data);
    internal static void Warning(object data) => _log.Warning(data);
#endif
}