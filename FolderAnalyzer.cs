using System.Diagnostics;

namespace ESDEUpdater;

public static class FolderAnalyzer
{
    public const string PortableTxt = "portable.txt";

    /// <summary>
    /// The name of an ES-DE user data folder and the base directory holding it.
    /// The base is the ES-DE folder itself for ordinary installs, or the
    /// location pointed to by portable.txt when that file redirects the data.
    /// </summary>
    public sealed record DataFolderInfo(string Name, string BasePath);

    public static string PortableTxtPath(string rootPath) => Path.Combine(rootPath, PortableTxt);

    /// <summary>
    /// Path to the portable.txt file, or null / empty content when absent or unset.
    /// </summary>
    public static bool IsPortableTxtRedirected(string rootPath)
    {
        var path = PortableTxtPath(rootPath);
        if (!File.Exists(path))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(ReadPortableTxtContent(path));
    }

    /// <summary>
    /// Resolves the base directory written inside portable.txt, relative to the
    /// ES-DE folder (or absolute). Returns the normalized base path plus the
    /// top-level segment name when that base lives inside the ES-DE folder
    /// (the segment the delete sweep would otherwise remove).
    /// </summary>
    public static string? TryResolvePortableDataBase(string rootPath, out string? topLevelName)
    {
        topLevelName = null;

        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return null;
        }

        var file = PortableTxtPath(rootPath);
        if (!File.Exists(file))
        {
            return null;
        }

        var content = ReadPortableTxtContent(file);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        string basePath;
        try
        {
            basePath = Path.IsPathRooted(content)
                ? content
                : Path.Combine(rootPath, content);
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not resolve the portable.txt path '{content}': {ex.Message}");
            return null;
        }

        var normalized = PathSafety.NormalizeForComparison(basePath);
        var rootNormalized = PathSafety.NormalizeForComparison(rootPath);

        if (PathSafety.IsWithinOrEqual(normalized, rootNormalized) &&
            !string.Equals(normalized, rootNormalized, StringComparison.OrdinalIgnoreCase))
        {
            topLevelName = GetTopLevelSegment(rootNormalized, normalized);
        }

        return normalized;
    }

    /// <summary>
    /// First path segment of <paramref name="path"/> relative to
    /// <paramref name="rootPath"/> — the top-level name the delete sweep
    /// would remove. Null when the path is not inside the root or equals it.
    /// </summary>
    public static string? GetTopLevelSegment(string rootPath, string path)
    {
        var root = PathSafety.NormalizeForComparison(rootPath);
        var normalized = PathSafety.NormalizeForComparison(path);

        if (!PathSafety.IsWithinOrEqual(normalized, root) ||
            string.Equals(normalized, root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relative = normalized[(root.Length + 1)..];
        var first = relative.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? null : first;
    }

    /// <summary>
    /// Locates the ES-DE user data folder. When portable.txt redirects the data
    /// location, that pointed-to base is authoritative and the folder names
    /// inside the ES-DE folder itself are ignored.
    /// </summary>
    public static DataFolderInfo? FindEsDeDataFolderInfo(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return null;
        }

        var redirectedBase = TryResolvePortableDataBase(rootPath, out _);
        if (redirectedBase is not null)
        {
            var redirectedName = FindEsDeDataFolder(redirectedBase);
            return redirectedName is null
                ? null
                : new DataFolderInfo(redirectedName, redirectedBase);
        }

        var rootName = FindEsDeDataFolder(rootPath);
        return rootName is null
            ? null
            : new DataFolderInfo(rootName, PathSafety.NormalizeForComparison(rootPath));
    }

    private static string? ReadPortableTxtContent(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            return text.Trim();
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not read {path}: {ex.Message}");
            return null;
        }
    }

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

                if (FolderNames.IsUpdaterEntry(name))
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

        return new FolderAnalysis
        {
            RootPath = rootPath,
            FolderExists = true,
            HasEsDeExecutable = HasEsDeExecutable(rootPath),
            HasEsDeDataFolder = FindEsDeDataFolderInfo(rootPath) is not null,
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
            foreach (var file in Directory.EnumerateFiles(romsPath, "*", SearchOption.AllDirectories))
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
