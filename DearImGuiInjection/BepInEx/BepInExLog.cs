using BepInEx.Logging;

namespace DearImGuiInjection;

internal class BepInExLog : ILog
{
    private ManualLogSource _logSource;

    internal BepInExLog(ManualLogSource logSource)
    {
        _logSource = logSource;
    }

    void ILog.Debug(object data) => _logSource.LogDebug(data);
    void ILog.Error(object data) => _logSource.LogError(data);
    void ILog.Fatal(object data) => _logSource.LogFatal(data);
    void ILog.Info(object data) => _logSource.LogInfo(data);
    void ILog.Message(object data) => _logSource.LogMessage(data);
    void ILog.Warning(object data) => _logSource.LogWarning(data);
}