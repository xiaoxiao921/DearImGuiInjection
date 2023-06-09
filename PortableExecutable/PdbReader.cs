using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using NativeMemory;

namespace PortableExecutable;

public class PdbReader
{
    private PEReader _target;

    private byte[] _pdbFileBytes;

    public PdbReader(PEReader target)
    {
        _target = target;
    }

    public bool FindOrDownloadPdb(string cacheDirectoryFullPath)
    {
        // create the cache directory if needed
        var cacheDirectory = Directory.CreateDirectory(cacheDirectoryFullPath);

        // Check if the correct PDB version is already cached

        var pdbFilePath = Path.Combine(cacheDirectory.FullName, $"{_target.RsdsPdbFileName.Replace(".pdb", string.Empty)}-{_target.PdbGuid:N}.pdb");
        var pdbFile = new FileInfo(pdbFilePath);

        if (pdbFile.Exists && pdbFile.Length != 0)
        {
            _pdbFileBytes = File.ReadAllBytes(pdbFilePath);
            return true;
        }

        // Delete any old PDB versions

        foreach (var file in cacheDirectory.EnumerateFiles().Where(file => file.Name.StartsWith(_target.RsdsPdbFileName)))
        {
            try
            {
                file.Delete();
            }
            catch (IOException)
            {
                // The file cannot be safely deleted
            }
        }

        // Download the PDB from the Microsoft symbol server

        using var httpClient = new HttpClient();

        var url = $"https://msdl.microsoft.com/download/symbols/{_target.RsdsPdbFileName}/{_target.PdbGuid:N}{_target.PdbAge}/{_target.RsdsPdbFileName}";
        using var response = httpClient.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"Failed to download required files [{_target.RsdsPdbFileName}] with status code {response.StatusCode}");
            return false;
        }

        if (response.Content.Headers.ContentLength is null)
        {
            Log.Error($"Failed to retrieve content headers for required files [{_target.RsdsPdbFileName}]");
            return false;
        }

        using var contentStreamTask = response.Content.ReadAsStreamAsync();
        contentStreamTask.Wait();
        using var contentStream = contentStreamTask.Result;
        using var fileStream = new FileStream(pdbFilePath, FileMode.Create);

        const int bufferSize = 65536;
        var copyBuffer = new byte[bufferSize];
        var bytesRead = 0;

        while (true)
        {
            var blockSize = contentStream.Read(copyBuffer, 0, bufferSize);

            if (blockSize == 0)
            {
                break;
            }

            bytesRead += blockSize;

            var bytesReadDouble = (double)bytesRead;
            var progressPercentage = bytesReadDouble / response.Content.Headers.ContentLength.Value * 100;
            var progress = progressPercentage / 2;
            Log.Info($"\rDownloading required files [{_target.RsdsPdbFileName}] - [{new string('=', (int)progress)}{new string(' ', 50 - (int)progress)}] - {(int)progressPercentage}%");

            fileStream.Write(copyBuffer, 0, blockSize);
        }

        _pdbFileBytes = new byte[fileStream.Length];
        fileStream.Position = 0;
        fileStream.Read(_pdbFileBytes, 0, (int)fileStream.Length);

        return true;
    }

    public unsafe IntPtr FindFunctionOffset(BytePattern[] bytePatterns)
    {
        fixed (byte* pdbFileStartPtr = &_pdbFileBytes[0])
        {
            IntPtr pdbStartAddress = (IntPtr)pdbFileStartPtr;
            long sizeOfPdb = _pdbFileBytes.Length;
            long pdbEndAddress = (long)(pdbFileStartPtr + sizeOfPdb);

            var match = bytePatterns.Select(p => new { p, res = p.Match(pdbStartAddress, pdbEndAddress) })
            .FirstOrDefault(m => m.res.ToInt64() > 0);

            if (match == null)
            {
                Log.Error("No function offset found, cannot hook! Please report it to the devs!");
                return IntPtr.Zero;
            }

            Log.Info($"Found at {match.res:X} ({match.res.ToInt64():X})");

            var functionOffsetPtr = (uint*)(pdbFileStartPtr + match.res.ToInt64() - 7);
            var functionOffset = *functionOffsetPtr;

            var sectionIndexPtr = (ushort*)(pdbFileStartPtr + match.res.ToInt64() - 3);
            var sectionIndex = *sectionIndexPtr - 1;

            functionOffset += _target.ImageSectionHeaders[sectionIndex].VirtualAddress;

            Log.Info("Function offset : " + functionOffset.ToString("X") + " | PE section : " + sectionIndex);

            return new IntPtr(functionOffset);
        }
    }
}