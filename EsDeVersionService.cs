using System.Diagnostics;

namespace ESDEUpdater;

public static class EsDeVersionService
{
    public static string? FindExecutable(string rootFolderPath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rootFolderPath) || !Directory.Exists(rootFolderPath))
            {
                return null;
            }

            string? fallback = null;
            foreach (var file in Directory.EnumerateFiles(rootFolderPath, "*.exe"))
            {
                var name = Path.GetFileName(file);

                if (FolderNames.IsUpdaterEntry(name))
                {
                    continue;
                }

                if (name.StartsWith("ES-DE", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }

                fallback ??= file;
            }

            return fallback;
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not enumerate executables in {rootFolderPath}: {ex.Message}");
            return null;
        }
    }

    public static string? TryGetDisplayVersion(string rootFolderPath)
    {
        var exe = FindExecutable(rootFolderPath);
        if (exe is null)
        {
            return null;
        }

        try
        {
            var info = FileVersionInfo.GetVersionInfo(exe);

            var raw = string.IsNullOrWhiteSpace(info.ProductVersion)
                ? info.FileVersion
                : info.ProductVersion;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return raw.Trim().Replace(',', '.');
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not read version info from {exe}: {ex.Message}");
            return null;
        }
    }

    public static Version? TryParse(string? displayVersion)
    {
        if (string.IsNullOrWhiteSpace(displayVersion))
        {
            return null;
        }

        var text = displayVersion.Trim().Replace(',', '.');

        if (Version.TryParse(text, out var version))
        {
            return version;
        }

        var end = 0;
        while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.'))
        {
            end++;
        }

        if (end > 0 && end < text.Length &&
            Version.TryParse(text[..end], out var prefixVersion))
        {
            return prefixVersion;
        }

        return null;
    }
}
