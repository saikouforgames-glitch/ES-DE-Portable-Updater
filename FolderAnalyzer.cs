using System.Diagnostics;

namespace ESDEUpdater;

public static class FolderAnalyzer
{
    public static string? FindEsDeDataFolder(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return null;
        }

        foreach (var name in FolderNames.KnownDataFolders)
        {
            if (Directory.Exists(Path.Combine(rootPath, name)))
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// True when a package executable sits directly in the folder root:
    /// "ES-DE*.exe" (modern releases), "EmulationStation*.exe" (legacy 2.x releases),
    /// or any .exe whose version metadata identifies it as ES-DE / EmulationStation.
    /// The updater's own executable ("ES-DE Updater.exe") is excluded.
    /// </summary>
    public static bool HasEsDeExecutable(string rootPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(rootPath, "*.exe"))
            {
                var name = Path.GetFileName(file);

                if (name.StartsWith("ES-DE Updater", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (name.StartsWith("ES-DE", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("EmulationStation", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (HasEsDeVersionMetadata(file))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not scan for ES-DE executables in {rootPath}: {ex.Message}");
            return false;
        }

        return false;
    }

    private static bool HasEsDeVersionMetadata(string exePath)
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(exePath);

            foreach (var value in new[] { info.ProductName, info.FileDescription, info.CompanyName })
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (value.Contains("ES-DE", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("EmulationStation", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static FolderAnalysis Analyze(string rootPath, IReadOnlyCollection<string> romExtensions)
    {
        if (!Directory.Exists(rootPath))
        {
            return new FolderAnalysis { RootPath = rootPath, FolderExists = false };
        }

        var emulatorsPath = Path.Combine(rootPath, FolderNames.Emulators);
        var romsPath = Path.Combine(rootPath, FolderNames.Roms);
        var hasEmulators = Directory.Exists(emulatorsPath);
        var hasRoms = Directory.Exists(romsPath);
        var dataFolderName = FindEsDeDataFolder(rootPath);

        return new FolderAnalysis
        {
            RootPath = rootPath,
            FolderExists = true,
            HasEsDeExecutable = HasEsDeExecutable(rootPath),
            HasEsDeDataFolder = dataFolderName is not null,
            EmulatorFolderCount = hasEmulators ? CountEmulatorFolders(emulatorsPath) : 0,
            RomFileCount = hasRoms ? CountRomFiles(romsPath, romExtensions) : 0
        };
    }

    private static int CountEmulatorFolders(string emulatorsPath)
    {
        try
        {
            return Directory.EnumerateDirectories(emulatorsPath).Count();
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not count emulator folders in {emulatorsPath}: {ex.Message}");
            return 0;
        }
    }

    private static int CountRomFiles(string romsPath, IReadOnlyCollection<string> romExtensions)
    {
        var count = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(romsPath, "*.*", SearchOption.AllDirectories))
            {
                if (SupportedRomExtensions.IsSupportedRomFile(file, romExtensions))
                {
                    count++;
                }
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not count ROM files in {romsPath}: {ex.Message}");
            return count;
        }

        return count;
    }
}
