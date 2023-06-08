namespace TestPlugin;

public class Class1
{
    private void Awake()
    {
        DearImGuiInjection.DearImGuiInjection.Render += Thing;
    }

    private void Thing()
    {

    }
}