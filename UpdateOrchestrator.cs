namespace ESDEUpdater;

public enum UpdateDirection { Unknown, Same, Upgrade, Downgrade }

public sealed record UpdatePlan(
    List<string> CopyItems,
    List<string> BackupFolders,
    SpaceCheckResult SpaceCheck,
    string? CurrentDataFolder,
    string? PackageDataFolder,
    bool RenameDataFolder,
    string CurrentDataBasePath);

public sealed record OldFolderSeal(DirectoryIdentity Identity);

/// <summary>
/// Executes the update pipeline: backup, rename data folder, delete old
/// program files, copy new files. No UI dependencies — status is reported
/// through a callback.
/// </summary>
public sealed class UpdateOrchestrator
{
    private readonly AppSettings _settings;
    private readonly HashSet<string> _exclusions;
    private readonly Action<string> _onStatus;

    public UpdateOrchestrator(
        AppSettings settings,
        HashSet<string> exclusions,
        Action<string> onStatus)
    {
        _settings = settings;
        _exclusions = exclusions;
        _onStatus = onStatus;
    }

    public UpdatePlan BuildUpdatePlan(string oldPath, string newPath)
    {
        var copyItems = BuildCopyItemList(newPath);
        var backupFolders = BuildBackupFolderList(oldPath);
        var spaceCheck = BackupService.CheckSpace(oldPath, newPath, copyItems, backupFolders, _settings.EnableBackup);

        var currentInfo = FolderAnalyzer.FindEsDeDataFolderInfo(oldPath);
        var packageInfo = FolderAnalyzer.FindEsDeDataFolderInfo(newPath);
        var renameDataFolder = currentInfo is not null &&
                               packageInfo is not null &&
                               !string.Equals(currentInfo.Name, packageInfo.Name, StringComparison.OrdinalIgnoreCase);

        return new UpdatePlan(
            copyItems,
            backupFolders,
            spaceCheck,
            currentInfo?.Name,
            packageInfo?.Name,
            renameDataFolder,
            currentInfo?.BasePath ?? PathSafety.NormalizeForComparison(oldPath));
    }

    public List<string> BuildDeleteList(string oldPath)
    {
        var items = new List<string>();

        foreach (var dirPath in Directory.EnumerateDirectories(oldPath))
        {
            var name = Path.GetFileName(dirPath);
            if (!IsExcluded(name))
            {
                items.Add(name);
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(oldPath))
        {
            var name = Path.GetFileName(filePath);
            if (!IsExcluded(name))
            {
                items.Add(name);
            }
        }

        return items;
    }

    public async Task ExecuteUpdateAsync(
        string oldPath,
        string newPath,
        UpdatePlan plan,
        OldFolderSeal? seal,
        CancellationToken cancellationToken = default)
    {
        if (_settings.EnableBackup && plan.BackupFolders.Count > 0)
        {
            await BackupService.CreateBackupAsync(oldPath, plan.BackupFolders, _onStatus, cancellationToken);
            _settings.LastBackupLocation = Path.Combine(oldPath, FolderNames.Backup);
            _onStatus("\u2714 Backup created.");
        }
        else if (_settings.EnableBackup)
        {
            _onStatus("\u26a0 No folders selected for backup \u2014 nothing to back up.");
        }
        else
        {
            _onStatus("\u26a0 Backup disabled \u2014 no backup created.");
        }

        if (plan.RenameDataFolder)
        {
            VerifySealAgainstDisk(oldPath, seal);
            await RenameDataFolderAsync(plan.CurrentDataBasePath, plan.CurrentDataFolder!, plan.PackageDataFolder!);
        }

        cancellationToken.ThrowIfCancellationRequested();

        VerifySealAgainstDisk(oldPath, seal);
        await Task.Run(() => DeleteOldProgramFiles(oldPath), cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        VerifySealAgainstDisk(oldPath, seal);
        foreach (var itemName in plan.CopyItems)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = Path.Combine(newPath, itemName);
            var destination = Path.Combine(oldPath, itemName);

            if (Directory.Exists(source))
            {
                _onStatus($"Copying {itemName}...");

                var result = await RobocopyService.CopyTreeAsync(
                    source,
                    destination,
                    _onStatus,
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Robocopy failed while copying {itemName}. Exit code: {result.ExitCode}");
                }

                _onStatus($"\u2714 Copying {itemName} completed.");
            }
            else if (File.Exists(source))
            {
                if (string.Equals(
                    PathSafety.NormalizeForComparison(destination),
                    PathSafety.NormalizeForComparison(Environment.ProcessPath ?? string.Empty),
                    StringComparison.OrdinalIgnoreCase))
                {
                    _onStatus($"\u26a0 Skipping {itemName} (running from this file).");
                    continue;
                }

                _onStatus($"Copying {itemName}...");
                await Task.Run(() => File.Copy(source, destination, overwrite: true), cancellationToken);
                _onStatus($"\u2714 Copying {itemName} completed.");
            }
            else
            {
                _onStatus($"\u26a0 Skipping {itemName} (not found in the package).");
            }
        }

        _onStatus("\u2714 Finished.");

        _onStatus(string.Empty);
        _onStatus("Next steps for ES-DE:");
        _onStatus("  \u2192 Open ES-DE and go to Utilities \u2192 Create/update system directories");
        _onStatus("    to register any new game systems added in this version.");
        _onStatus("  \u2192 Open ES-DE \u2192 Theme Downloader to update themes for new system support.");
        _onStatus("  \u2192 Ensure all customizations are in the ES-DE/custom_systems/ folder.");
        _onStatus(string.Empty);
    }

    public string BuildConfirmationMessage(string oldPath, string newPath, UpdatePlan plan, string? currentVersion, string? packageVersion, UpdateDirection direction, HashSet<string> exclusions)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("UPDATE PREVIEW");
        sb.AppendLine();

        sb.AppendLine("Current:");
        sb.AppendLine($"  {(currentVersion is not null ? $"v{currentVersion}" : "unknown version")}");
        sb.AppendLine($"  {oldPath}");

        if (!FolderAnalyzer.HasEsDeExecutable(oldPath))
        {
            sb.AppendLine("  \u26a0 ES-DE.exe NOT FOUND \u2014 REPAIR MODE.");
            sb.AppendLine("  The package executable will be installed into the Current folder.");
        }

        sb.AppendLine();

        sb.AppendLine("Package:");
        sb.AppendLine($"  {(packageVersion is not null ? $"v{packageVersion}" : "unknown version")}");
        sb.AppendLine($"  {newPath}");
        sb.AppendLine();

        if (direction is UpdateDirection.Upgrade or UpdateDirection.Downgrade &&
            currentVersion is not null && packageVersion is not null)
        {
            sb.AppendLine($"{direction}: v{currentVersion} \u2192 v{packageVersion}");
            sb.AppendLine();
        }

        sb.AppendLine("PROGRAM FILES");
        sb.AppendLine();

        var deleteItems = BuildDeleteList(oldPath);
        if (deleteItems.Count > 0)
        {
            sb.AppendLine($"To remove ({deleteItems.Count}):");
            sb.AppendLine(TruncatedLine(deleteItems, MaxPreviewItems));
        }
        else
        {
            sb.AppendLine("To remove: (nothing to remove)");
        }
        sb.AppendLine();

        sb.AppendLine($"To install ({plan.CopyItems.Count}):");
        sb.AppendLine(TruncatedLine(plan.CopyItems, MaxPreviewItems));
        sb.AppendLine();

        sb.AppendLine("USER DATA \u2014 PRESERVED");
        sb.AppendLine();
        sb.AppendLine($"  {FolderNames.Emulators}, {plan.CurrentDataFolder ?? FolderNames.EsDe}, {FolderNames.Roms}");
        sb.AppendLine("  These folders will NOT be deleted or copied.");
        sb.AppendLine();

        if (!string.Equals(
            PathSafety.NormalizeForComparison(plan.CurrentDataBasePath),
            PathSafety.NormalizeForComparison(oldPath),
            StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"  portable.txt points the user data folder to: {plan.CurrentDataBasePath}");
            sb.AppendLine("  portable.txt and its target location are kept; the package's portable.txt is NOT copied.");
            sb.AppendLine();
        }

        if (exclusions.Count > 0)
        {
            sb.AppendLine("EXCLUDED \u2014 KEPT (NOT DELETED, NOT OVERWRITTEN)");
            sb.AppendLine();
            sb.AppendLine(TruncatedLine(
                exclusions.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
                MaxPreviewItems));
            sb.AppendLine();
        }

        if (plan.RenameDataFolder)
        {
            sb.AppendLine("DATA FOLDER RENAME");
            sb.AppendLine();
            sb.AppendLine($"  {plan.CurrentDataFolder} \u2192 {plan.PackageDataFolder}");
            sb.AppendLine();
            sb.AppendLine(BuildDataFolderRenameMessage(plan.CurrentDataFolder!, plan.PackageDataFolder!, currentVersion, packageVersion));
            sb.AppendLine();
        }

        sb.AppendLine("BACKUP");
        sb.AppendLine();
        if (!_settings.EnableBackup)
        {
            sb.AppendLine("  OFF \u2014 nothing will be backed up.");
        }
        else if (plan.BackupFolders.Count > 0)
        {
            sb.AppendLine($"  ON \u2014 {string.Join(", ", plan.BackupFolders)}");
        }
        else
        {
            sb.AppendLine("  \u26a0 ON \u2014 no folders selected for backup");
        }
        sb.AppendLine();

        sb.AppendLine("DISK SPACE");
        sb.AppendLine();
        var sc = plan.SpaceCheck;
        sb.AppendLine($"  Drive {sc.CopyDriveRoot}");
        sb.AppendLine($"  Free:     {DiskSpaceHelper.FormatBytes(sc.CopyDriveAvailableBytes)}");
        if (sc.BackupEnabled)
        {
            sb.AppendLine($"  Required: {DiskSpaceHelper.FormatBytes(sc.CopyBytesRequired + sc.BackupBytesRequired)}");
        }
        else
        {
            sb.AppendLine($"  Required: {DiskSpaceHelper.FormatBytes(sc.CopyBytesRequired)}");
        }
        sb.AppendLine();
        sb.AppendLine(sc.HasEnoughSpace
            ? "  \u2713 Enough space available"
            : "  \u2717 Not enough disk space");

        if (sc.CopyUnmeasuredFiles > 0 || sc.BackupUnmeasuredFiles > 0)
        {
            var parts = new List<string>();
            if (sc.CopyUnmeasuredFiles > 0)
            {
                parts.Add($"{sc.CopyUnmeasuredFiles} copy file(s)");
            }

            if (sc.BackupUnmeasuredFiles > 0)
            {
                parts.Add($"{sc.BackupUnmeasuredFiles} backup file(s)");
            }

            sb.AppendLine($"  \u26a0 {string.Join(" and ", parts)} could not be measured \u2014 sizes may be too low.");
        }

        return sb.ToString();
    }

    private const int MaxPreviewItems = 8;

    private static string TruncatedLine(IReadOnlyList<string> items, int max)
    {
        var shown = items.Take(max).ToList();
        var line = string.Join(", ", shown);
        if (items.Count > max)
        {
            line += $", \u2026and {items.Count - max} more";
        }
        return $"  {line}";
    }

    private bool IsExcluded(string name) =>
        FolderNames.IsPreservedTopLevel(name) || _exclusions.Contains(name);

    private List<string> BuildCopyItemList(string packagePath)
    {
        var items = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(packagePath))
        {
            var name = Path.GetFileName(directory);
            if (!IsExcluded(name))
            {
                items.Add(name);
            }
        }

        foreach (var file in Directory.EnumerateFiles(packagePath))
        {
            var name = Path.GetFileName(file);
            if (!IsExcluded(name))
            {
                items.Add(name);
            }
        }

        return items;
    }

    private List<string> BuildBackupFolderList(string oldPath)
    {
        var folders = new List<string>();
        if (_settings.BackupEmulators)
        {
            folders.Add(FolderNames.Emulators);
        }

        if (_settings.BackupEsDe)
        {
            var info = FolderAnalyzer.FindEsDeDataFolderInfo(oldPath);
            if (info is null)
            {
                folders.Add(FolderNames.EsDe);
            }
            else if (string.Equals(
                PathSafety.NormalizeForComparison(info.BasePath),
                PathSafety.NormalizeForComparison(oldPath),
                StringComparison.OrdinalIgnoreCase))
            {
                folders.Add(info.Name);
            }
            else
            {
                var segment = FolderAnalyzer.GetTopLevelSegment(oldPath, info.BasePath);
                if (segment is not null)
                {
                    folders.Add(segment);
                }
            }
        }

        if (_settings.BackupRoms)
        {
            folders.Add(FolderNames.Roms);
        }

        if (_settings.BackupRomsAll)
        {
            folders.Add(FolderNames.RomsAll);
        }

        return folders;
    }

    private void DeleteOldProgramFiles(string oldPath)
    {
        var itemsDeleted = 0;
        var itemsFailed = 0;

        foreach (var dirPath in Directory.EnumerateDirectories(oldPath))
        {
            var name = Path.GetFileName(dirPath);

            if (IsExcluded(name))
            {
                continue;
            }

            _onStatus($"Deleting {name}...");

            try
            {
                Directory.Delete(dirPath, recursive: true);
                _onStatus($"\u2714 Deleted {name}.");
                itemsDeleted++;
            }
            catch (Exception ex)
            {
                _onStatus($"\u26a0 Could not delete {name}: {ex.Message}");
                itemsFailed++;
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(oldPath))
        {
            var name = Path.GetFileName(filePath);

            if (IsExcluded(name))
            {
                continue;
            }

            if (string.Equals(
                PathSafety.NormalizeForComparison(filePath),
                PathSafety.NormalizeForComparison(Environment.ProcessPath ?? string.Empty),
                StringComparison.OrdinalIgnoreCase))
            {
                _onStatus($"\u26a0 Skipping {name} (running from this file).");
                continue;
            }

            _onStatus($"Deleting {name}...");

            try
            {
                File.Delete(filePath);
                _onStatus($"\u2714 Deleted {name}.");
                itemsDeleted++;
            }
            catch (Exception ex)
            {
                _onStatus($"\u26a0 Could not delete {name}: {ex.Message}");
                itemsFailed++;
            }
        }

        if (itemsDeleted == 0 && itemsFailed > 0)
        {
            throw new InvalidOperationException(
                "All program files failed to be deleted. The update cannot continue because " +
                "old and new files would conflict. Close any programs that may be using files " +
                "in the ES-DE folder and try again.");
        }
    }

    private void VerifySealAgainstDisk(string oldPath, OldFolderSeal? seal)
    {
        if (seal is null)
        {
            return;
        }

        var canonical = PathSafety.Canonicalize(oldPath, out _);
        var currentIdentity = canonical is null
            ? null
            : PathSafety.GetDirectoryIdentity(canonical);

        if (currentIdentity is null || !currentIdentity.Value.Matches(seal.Identity))
        {
            throw new InvalidOperationException(
                "The Current ES-DE folder changed on disk after it was validated." + Environment.NewLine +
                Environment.NewLine +
                oldPath + Environment.NewLine +
                Environment.NewLine +
                "The folder was moved, replaced, or linked elsewhere while the update was running. " +
                "The update stopped before making further changes. " +
                "Verify the folder and start the update again.");
        }
    }

    private async Task RenameDataFolderAsync(string currentDataBasePath, string currentDataFolder, string packageDataFolder)
    {
        var source = Path.Combine(currentDataBasePath, currentDataFolder);
        var target = Path.Combine(currentDataBasePath, packageDataFolder);

        if (Directory.Exists(target))
        {
            throw new InvalidOperationException(
                $"The update cannot continue.\n\n" +
                $"Both \"{currentDataFolder}\" and \"{packageDataFolder}\" data folders were found at:\n" +
                currentDataBasePath + "\n\n" +
                "The updater cannot safely determine which data folder should be preserved. " +
                "Please remove or resolve the duplicate folder manually, then run the update again.");
        }

        _onStatus($"\u2192 Data folder rename: {currentDataFolder} \u2192 {packageDataFolder}.");
        await Task.Run(() => Directory.Move(source, target));
        _onStatus("\u2714 Data folder renamed.");
    }

    public static string BuildVersionLine(string label, string? version) =>
        version is null ? $"\u2714 {label} verified." : $"\u2714 {label} verified (v{version}).";

    public static string BuildDataFolderRenameMessage(string sourceName, string targetName, string? currentVersion, string? packageVersion)
    {
        var currentLabel = currentVersion is not null ? $"v{currentVersion}" : "the current version";
        var packageLabel = packageVersion is not null ? $"v{packageVersion}" : "this package";

        if (string.Equals(targetName, ".emulationstation", StringComparison.OrdinalIgnoreCase))
        {
            return
                $"This downgrade is from {currentLabel} to {packageLabel}." + Environment.NewLine +
                $"{packageLabel} uses \u201c.emulationstation\u201d for user data." + Environment.NewLine +
                "Your existing data folder will be renamed so the older" + Environment.NewLine +
                "version can continue to use your settings, gamelists," + Environment.NewLine +
                "and themes.";
        }

        return
            $"This upgrade is from {currentLabel} to {packageLabel}." + Environment.NewLine +
            $"ES-DE 3.0.0+ uses \u201cES-DE\u201d for user data." + Environment.NewLine +
            "Your existing data folder will be renamed so the newer" + Environment.NewLine +
            "version can continue to use your settings, gamelists," + Environment.NewLine +
            "and themes.";
    }
}
