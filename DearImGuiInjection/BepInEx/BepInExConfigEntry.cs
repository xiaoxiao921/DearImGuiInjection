using BepInEx.Configuration;

namespace DearImGuiInjection;

public class BepInExConfigEntry<T> : IConfigEntry<T>
{
    private ConfigEntry<T> _configEntry;

    public BepInExConfigEntry(ConfigEntry<T> configEntry)
    {
        _configEntry = configEntry;
    }

    public T Get() => _configEntry.Value;
    public void Set(T value) => _configEntry.Value = value;
}