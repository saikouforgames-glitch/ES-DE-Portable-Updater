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

    public static bool HasExecutableInRoot(string rootPath)
    {
        try
        {
            return Directory.EnumerateFiles(rootPath, "*.exe").Any();
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not scan for executables in {rootPath}: {ex.Message}");
            return false;
        }
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
            HasEsDeExecutable = HasExecutableInRoot(rootPath),
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
