namespace ESDEUpdater;

public sealed class SpaceCheckResult
{
    public string CopyDriveRoot { get; init; } = string.Empty;
    public long CopyDriveAvailableBytes { get; init; }
    public long CopyBytesRequired { get; init; }
    public long BackupBytesRequired { get; init; }
    public bool BackupEnabled { get; init; }
    public bool HasEnoughSpace { get; init; }

    /// <summary>Files that could not be measured in the Package while estimating the copy size.</summary>
    public int CopyUnmeasuredFiles { get; init; }

    /// <summary>Files that could not be measured in the Current folder while estimating the backup size.</summary>
    public int BackupUnmeasuredFiles { get; init; }

    private string PartialSizesNote
    {
        get
        {
            if (CopyUnmeasuredFiles == 0 && BackupUnmeasuredFiles == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            if (CopyUnmeasuredFiles > 0)
            {
                parts.Add($"{CopyUnmeasuredFiles} copy file(s)");
            }

            if (BackupUnmeasuredFiles > 0)
            {
                parts.Add($"{BackupUnmeasuredFiles} backup file(s)");
            }

            return $"\n  \u26a0 {string.Join(" and ", parts)} could not be measured \u2014 sizes may be too low.";
        }
    }

    public string Summary
    {
        get
        {
            if (!BackupEnabled)
            {
                return
                    $"Copy to Current drive ({CopyDriveRoot})\n" +
                    $"  Free space: {DiskSpaceHelper.FormatBytes(CopyDriveAvailableBytes)}\n" +
                    $"  Required: {DiskSpaceHelper.FormatBytes(CopyBytesRequired)}\n" +
                    "Backup is disabled — no additional space required." +
                    PartialSizesNote;
            }

            return
                $"Copy to Current drive ({CopyDriveRoot})\n" +
                $"  Free space: {DiskSpaceHelper.FormatBytes(CopyDriveAvailableBytes)}\n" +
                $"  Copy required: {DiskSpaceHelper.FormatBytes(CopyBytesRequired)}\n" +
                $"  Backup required: {DiskSpaceHelper.FormatBytes(BackupBytesRequired)}\n" +
                $"  Total required: {DiskSpaceHelper.FormatBytes(CopyBytesRequired + BackupBytesRequired)}" +
                PartialSizesNote;
        }
    }
}

public static class BackupService
{
    private const long MinDiskSafetyMarginBytes = 256L * 1024 * 1024;
    private const double DiskSafetyMarginPercent = 0.05;

    public static SpaceCheckResult CheckSpace(
        string oldPath,
        string newPath,
        IReadOnlyCollection<string> copyItems,
        IReadOnlyCollection<string> backupFolders,
        bool backupEnabled)
    {
        // Copy and backup both land in the Old installation (Old drive).
        var driveRoot = Path.GetPathRoot(Path.GetFullPath(oldPath)) ?? string.Empty;
        var available = DiskSpaceHelper.GetAvailableFreeSpace(oldPath);
        var copySize = DiskSpaceHelper.GetItemsSize(newPath, copyItems);
        var backupSize = backupEnabled
            ? DiskSpaceHelper.GetDirectoriesSize(oldPath, backupFolders)
            : default;

        var totalRequired = copySize.Bytes + backupSize.Bytes;
        var margin = Math.Max(MinDiskSafetyMarginBytes, (long)(totalRequired * DiskSafetyMarginPercent));
        var hasEnough = available >= totalRequired + margin;

        return new SpaceCheckResult
        {
            CopyDriveRoot = driveRoot,
            CopyDriveAvailableBytes = available,
            CopyBytesRequired = copySize.Bytes,
            BackupBytesRequired = backupSize.Bytes,
            BackupEnabled = backupEnabled,
            HasEnoughSpace = hasEnough,
            CopyUnmeasuredFiles = copySize.UnmeasuredFiles,
            BackupUnmeasuredFiles = backupSize.UnmeasuredFiles
        };
    }

    public static async Task CreateBackupAsync(
        string oldPath,
        IReadOnlyCollection<string> backupFolders,
        Action<string>? onStatus = null,
        CancellationToken cancellationToken = default)
    {
        if (backupFolders.Count == 0)
        {
            return;
        }

        var backupRoot = Path.Combine(oldPath, FolderNames.Backup);
        Directory.CreateDirectory(backupRoot);

        foreach (var folderName in backupFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = Path.Combine(oldPath, folderName);
            if (!Directory.Exists(source))
            {
                onStatus?.Invoke($"⚠ Skipping backup of {folderName} (not found in Current ES-DE).");
                continue;
            }

            var destination = Path.Combine(backupRoot, folderName);
            onStatus?.Invoke($"Creating backup of {folderName}...");

            var result = await RobocopyService.CopyTreeAsync(source, destination, onStatus, cancellationToken);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Backup failed while copying {folderName}. Exit code: {result.ExitCode}");
            }

            onStatus?.Invoke($"✔ Backup of {folderName} completed.");
        }
    }

    public static List<string> BuildBackupFolderList(AppSettings settings, string oldPath)
    {
        var folders = new List<string>();
        if (settings.BackupEmulators)
        {
            folders.Add(FolderNames.Emulators);
        }

        if (settings.BackupEsDe)
        {
            var info = FolderAnalyzer.FindEsDeDataFolderInfo(oldPath);
            if (info is null)
            {
                folders.Add(FolderNames.EsDe);
            }
            else if (PathSafety.EqualsNormalized(info.BasePath, oldPath))
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

        if (settings.BackupRoms)
        {
            folders.Add(FolderNames.Roms);
        }

        if (settings.BackupRomsAll)
        {
            folders.Add(FolderNames.RomsAll);
        }

        return folders;
    }
}
