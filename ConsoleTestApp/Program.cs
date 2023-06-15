using System.Diagnostics;

var solutionDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

void FindSolutionDirectory()
{
    while (true)
    {
        foreach (var item in solutionDirectory.EnumerateFiles())
        {
            Console.WriteLine(item.FullName);
            if (item.Extension == ".sln")
            {
                return;
            }
        }

        solutionDirectory = solutionDirectory!.Parent;

        if (solutionDirectory == null || !solutionDirectory.Exists)
        {
            Console.WriteLine("couldnt find a sln file in up directories");
            return;
        }
    }
}

FindSolutionDirectory();

if (string.IsNullOrWhiteSpace(solutionDirectory?.FullName))
{
    return;
}

CreateRelease(solutionDirectory, "netstandard2.0");
CreateRelease(solutionDirectory, "net462");
CreateRelease(solutionDirectory, "net6");

static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
{
    // Get information about the source directory
    var dir = new DirectoryInfo(sourceDir);

    // Check if the source directory exists
    if (!dir.Exists)
        throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

    // Cache directories before we start copying
    DirectoryInfo[] dirs = dir.GetDirectories();

    // Create the destination directory
    Directory.CreateDirectory(destinationDir);

    // Get the files in the source directory and copy to the destination directory
    foreach (FileInfo file in dir.GetFiles())
    {
        string targetFilePath = Path.Combine(destinationDir, file.Name);
        file.CopyTo(targetFilePath);
    }

    // If recursive and copying subdirectories, recursively call this method
    if (recursive)
    {
        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir, true);
        }
    }
}

static void CopyAssetsDirectoryToReleaseDirectory(DirectoryInfo solutionDirectory, string releaseDirectory)
{
    var assetsFolder = Path.Combine(solutionDirectory.FullName, "DearImGuiInjection", "Assets");
    var assetsReleaseDirectory = Path.Combine(releaseDirectory, "Assets");

    Directory.CreateDirectory(assetsReleaseDirectory);

    CopyDirectory(assetsFolder, assetsReleaseDirectory, true);
}

static void CreateRelease(DirectoryInfo solutionDirectory, string configuration)
{
    var processes = new List<Process>();
    foreach (var item in Directory.GetFiles(solutionDirectory.FullName, "DearImGuiInjection.csproj", SearchOption.AllDirectories))
    {
        /*if (item.Contains("ConsoleTestApp"))
        {
            continue;
        }*/

        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build . --configuration {configuration}",
                WorkingDirectory = Path.GetDirectoryName(item),
                UseShellExecute = false
            }
        };
        p.Start();
        processes.Add(p);
    }

    foreach (var item in processes)
    {
        item.WaitForExit();
    }

    CreateReleaseArchitecture(solutionDirectory, configuration, "x64");
    CreateReleaseArchitecture(solutionDirectory, configuration, "x86");

    static void CreateReleaseArchitecture(DirectoryInfo solutionDirectory, string configuration, string architecture)
    {
        var architectureReleaseDirectory = Path.Combine(solutionDirectory.FullName, "release", $"{architecture}_{configuration}");
        try
        {
            Directory.Delete(architectureReleaseDirectory, true);
        }
        catch (Exception)
        {
        }
        Directory.CreateDirectory(architectureReleaseDirectory);

        string[] denyList =
        {
            "Harmony",
            "AsmResol",
            "AssetRipper",
            "BepInEx",
            "Cpp2",
            "Disar",
            "Gee.",
            "Il2CppInter", "LibCpp2IL",
            "Microsoft",
            "Mono.C",
            "MonoMod", "Semantic", "StableNameDotNet", "Wasm",
            "System.",
            "Iced",
            "capstone"
        };

        foreach (var dllFullPath in Directory.GetFiles(solutionDirectory.FullName, "*.dll", SearchOption.AllDirectories))
        {
            var shouldSkip = false;
            foreach (var deniedAssembly in denyList)
            {
                if (Path.GetFileName(dllFullPath).Contains(deniedAssembly))
                {
                    shouldSkip = true;
                    break;
                }
            }

            if (shouldSkip)
            {
                continue;
            }

            if (!dllFullPath.Contains($"bin\\{configuration}"))
            {
                continue;
            }

            File.Copy(dllFullPath, Path.Combine(architectureReleaseDirectory, Path.GetFileName(dllFullPath)), true);
        }

        var fileName = "cimgui.dll";
        File.Copy(
            Path.Combine(solutionDirectory.FullName, "libs", "cimgui", $"win-{architecture}", fileName),
            Path.Combine(architectureReleaseDirectory, fileName));

        CopyAssetsDirectoryToReleaseDirectory(solutionDirectory, architectureReleaseDirectory);
    }
}

/*using System.Diagnostics;
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

var cacheDirectoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DearImGuiInjection", "pdb");
dxgiPdbReader.FindOrDownloadPdb(cacheDirectoryPath);

var swapChainPresentFunctionOffset = dxgiPdbReader.FindFunctionOffset(new BytePattern[] { Encoding.ASCII.GetBytes("CDXGISwapChain::Present\0") });*/