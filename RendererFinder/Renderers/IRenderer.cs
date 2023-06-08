namespace RendererFinder.Renderers;

public enum RendererKind
{
    None,
    DXGI
}

public interface IRenderer
{
    public bool Init();

    public void Dispose();
}