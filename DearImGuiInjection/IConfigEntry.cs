namespace DearImGuiInjection;

public interface IConfigEntry<T>
{
    public T Get();
    public void Set(T value);
}