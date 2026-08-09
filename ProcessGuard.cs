using System.ComponentModel;
using System.Diagnostics;

namespace ESDEUpdater;

/// <summary>
/// Detects programs that are currently running from a folder the updater is about
/// to modify. Only programs whose executable lives in the destructive scope count
/// as conflicts: preserving folders (Emulators, ROMs, user data, Backup, the
/// updater itself) are never touched by the update, so programs running from
/// inside them are ignored.
/// </summary>
public static class ProcessGuard
{
    private static readonly string[] PreservedFolderNames = FolderNames.PreservedFolders;

    /// <summary>
    /// Returns display strings ("process name — full exe path") for every running
    /// process whose executable sits in the destructive scope of
    /// <paramref name="folderPath"/> (folders that the update will delete or replace).
    /// System processes that cannot be inspected are skipped silently.
    /// </summary>
    public static IReadOnlyList<string> FindProcessFilesUnder(string folderPath)
    {
        var results = new List<string>();

        string? folder = null;
        try
        {
            var full = Path.GetFullPath(folderPath.TrimEnd('\\', '/'));
            folder = full.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"ProcessGuard: could not resolve folder '{folderPath}': {ex.Message}");
            return results;
        }

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id == Environment.ProcessId)
                {
                    continue;
                }

                var mainModule = process.MainModule;
                if (mainModule?.FileName is null)
                {
                    continue;
                }

                var exePath = Path.GetFullPath(mainModule.FileName);
                if (!exePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = exePath[folder.Length..];
                var topLevelName = relative.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (topLevelName is not null &&
                    PreservedFolderNames.Contains(topLevelName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add($"{process.ProcessName} \u2014 {exePath}");
            }
            catch (Win32Exception)
            {
                // Expected for system processes and elevated programs:
                // the executable path cannot be read. They can never be part
                // of this update's destructive scope, so skip silently.
            }
            catch (Exception ex)
            {
                Diagnostics.Report($"ProcessGuard: skipped process {process.ProcessName}: {ex.Message}");
            }
        }

        return results;
    }
}