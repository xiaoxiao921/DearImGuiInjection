using System.Diagnostics;
using System.Text;
using NativeMemory;
using PortableExecutable;

var dxgiModule = Process.GetCurrentProcess().Modules.Cast<ProcessModule>().FirstOrDefault(p => p?.ModuleName != null && p.ModuleName.ToLowerInvariant().Contains("dxgi"));

if (dxgiModule == null)
{
    Log.Error("dxgiModule == null");
    return;
}

if (string.IsNullOrWhiteSpace(dxgiModule.FileName))
{
    Log.Error("string.IsNullOrWhiteSpace(dxgiModule.FileName)");
    return;
}

var peReader = PEReader.FromFilePath(dxgiModule.FileName);
if (peReader == null)
{
    Log.Error("string.IsNullOrWhiteSpace(dxgiModule.FileName)");
    return;
}
var dxgiPdbReader = new PdbReader(peReader);

var cacheDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UnityMod.DearImGui", "pdb");
dxgiPdbReader.FindOrDownloadPdb(cacheDirectoryPath);

var swapChainPresentFunctionOffset = dxgiPdbReader.FindFunctionOffset(new BytePattern[] { Encoding.ASCII.GetBytes("CDXGISwapChain::Present\0") });