namespace RendererFinder.Renderers;

public enum RendererKind
{
    None,
    D3D11,
    D3D12
}

public interface IRenderer
{
    public bool Init();

    public void Dispose();
}