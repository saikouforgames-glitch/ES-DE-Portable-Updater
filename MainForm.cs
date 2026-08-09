namespace ESDEUpdater;

public partial class MainForm : Form
{
    private static readonly string[] ExcludedFromCopy = FolderNames.PreservedFolders;

    private enum UpdateDirection { Unknown, Same, Upgrade, Downgrade }

    private sealed record UpdatePlan(
        List<string> CopyItems,
        List<string> BackupFolders,
        SpaceCheckResult SpaceCheck,
        string? CurrentDataFolder,
        string? PackageDataFolder,
        bool RenameDataFolder);

    private sealed record OldFolderSeal(DirectoryIdentity Identity);

    private AppSettings _settings = new();
    private bool _updateRunning;
    private int _lastReportedDownloadPercent = -1;
    private UpdateDirection _direction = UpdateDirection.Unknown;
    private string? _currentVersion;
    private string? _packageVersion;
    private OldFolderSeal? _seal;

    public MainForm()
    {
        InitializeComponent();
        Diagnostics.Log = AppendStatus;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        _settings = SettingsService.Load();

        if (_settings.RememberLastFolders)
        {
            txtOldFolder.Text = _settings.LastOldPath;
            txtNewFolder.Text = _settings.LastNewPath;

            if (!string.IsNullOrWhiteSpace(_settings.LastOldPath) && !Directory.Exists(_settings.LastOldPath))
            {
                AppendStatus($"⚠ The saved Current ES-DE folder no longer exists: {_settings.LastOldPath}");
            }

            if (!string.IsNullOrWhiteSpace(_settings.LastNewPath) && !Directory.Exists(_settings.LastNewPath))
            {
                AppendStatus($"⚠ The saved Package folder no longer exists: {_settings.LastNewPath}");
            }
        }

        ApplyTheme();
        UpdateBackupUi();
        UpdatePackageUi();
        UpdateDirectionUi();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_updateRunning)
        {
            var confirm = MessageBox.Show(
                "An update is currently in progress." + Environment.NewLine +
                Environment.NewLine +
                "Closing the window now may leave your ES-DE installation in an inconsistent state." + Environment.NewLine +
                Environment.NewLine +
                "Are you sure you want to close?",
                "Update In Progress",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        Diagnostics.Log = null;
    }

    private void BtnBrowseOld_Click(object? sender, EventArgs e)
    {
        BrowseFolder(txtOldFolder, isOldFolder: true);
    }

    private void BtnBrowseNew_Click(object? sender, EventArgs e)
    {
        BrowseFolder(txtNewFolder, isOldFolder: false);
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        using var dialog = new SettingsForm(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _settings = dialog.Settings;

        if (!SettingsService.Save(_settings))
        {
            MessageBox.Show(
                "Your settings could not be saved. Check write permissions for settings.json next to the updater.",
                "Settings Not Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        if (!_settings.RememberLastFolders)
        {
            txtOldFolder.Clear();
            txtNewFolder.Clear();
        }

        ApplyTheme();
        UpdateBackupUi();
        UpdatePackageUi();
        UpdateDirectionUi();
    }

    private void BtnAbout_Click(object? sender, EventArgs e)
    {
        using var dialog = new AboutForm(_settings.Theme);
        dialog.ShowDialog(this);
    }

    private void BrowseFolder(TextBox targetTextBox, bool isOldFolder)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = isOldFolder ? "Select Current ES-DE (In Use)" : "Select Upgrade/Downgrade Package",
            UseDescriptionForTitle = true,
            SelectedPath = GetInitialBrowsePath(targetTextBox.Text)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var selectedPath = dialog.SelectedPath;
        var validationError = isOldFolder
            ? EsDeValidation.ValidateOldFolder(selectedPath)
            : EsDeValidation.ValidateNewFolder(selectedPath);

        if (validationError is not null)
        {
            MessageBox.Show(
                validationError,
                isOldFolder ? "Invalid Current ES-DE Folder" : "Invalid Package Folder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        targetTextBox.Text = selectedPath;
        UpdateDirectionUi();
        PersistSettings();
    }

    private async void BtnStartUpdate_Click(object? sender, EventArgs e)
    {
        if (_updateRunning)
        {
            return;
        }

        UpdateDirectionUi();

        var oldPath = txtOldFolder.Text.Trim();
        var newPath = txtNewFolder.Text.Trim();

        var validation = EsDeValidation.ValidateForUpdate(oldPath, newPath);
        if (!validation.IsSuccess)
        {
            MessageBox.Show(
                validation.Message,
                validation.Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var lockingProcesses = ProcessGuard.FindProcessFilesUnder(oldPath);
        if (lockingProcesses.Count > 0)
        {
            var processList = string.Join(Environment.NewLine, lockingProcesses.Select(line => "  " + line));
            MessageBox.Show(
                "These program(s) are running from the Current ES-DE folder:" + Environment.NewLine +
                Environment.NewLine +
                processList + Environment.NewLine +
                Environment.NewLine +
                "Their files will be replaced during the update, so the update cannot start while they are open." +
                Environment.NewLine +
                Environment.NewLine +
                "Close them, then click Start Upgrade again.",
                "Programs Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        AppendStatus("↳ Running-program check: no program is running from the ES-DE folder.");

        UpdatePlan plan;
        try
        {
            plan = BuildUpdatePlan(oldPath, newPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The update could not be prepared." + Environment.NewLine +
                Environment.NewLine + ex.Message,
                "Preparation Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var confirm = MessageBox.Show(
            BuildConfirmationMessage(oldPath, newPath, plan),
            "Update Preview",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var revalidation = EsDeValidation.ValidateForUpdate(oldPath, newPath);
        if (!revalidation.IsSuccess)
        {
            MessageBox.Show(
                revalidation.Message,
                revalidation.Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (!plan.SpaceCheck.HasEnoughSpace)
        {
            MessageBox.Show(
                "There is not enough free disk space to copy the program files and create the backup." + Environment.NewLine +
                Environment.NewLine +
                plan.SpaceCheck.Summary + Environment.NewLine +
                Environment.NewLine +
                "Free up disk space and try again.",
                "Insufficient Disk Space",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var canonicalOldPath = PathSafety.Canonicalize(oldPath, out var canonicalOldError);
        if (canonicalOldError is not null)
        {
            MessageBox.Show(
                canonicalOldError,
                "Folder Verification Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var oldIdentity = PathSafety.GetDirectoryIdentity(canonicalOldPath!);
        if (oldIdentity is null)
        {
            MessageBox.Show(
                "The Current ES-DE folder could not be reopened for verification." + Environment.NewLine +
                Environment.NewLine +
                oldPath + Environment.NewLine +
                Environment.NewLine +
                "This usually means the folder or its drive is not accessible right now.",
                "Folder Verification Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _seal = new OldFolderSeal(oldIdentity.Value);

        _updateRunning = true;
        SetControlsEnabled(false);
        txtStatusLog.Clear();

        try
        {
            await ExecuteUpdateAsync(oldPath, newPath, plan);
        }
        catch (Exception ex)
        {
            AppendStatus($"✖ Error: {ex.Message}");
            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine +
                "If the backup step was enabled and completed, a copy of your selected folders was created inside the " +
                "Backup folder of the current ES-DE installation. You can restore from it if needed." + Environment.NewLine +
                Environment.NewLine +
                "Extract a new ES-DE portable package and try again.",
                "Update Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _updateRunning = false;
            _seal = null;
            SetControlsEnabled(true);
            UpdateBackupUi();
            UpdatePackageUi();
            UpdateDirectionUi();
        }
    }

    private UpdatePlan BuildUpdatePlan(string oldPath, string newPath)
    {
        var copyItems = BuildCopyItemList(newPath);
        var backupFolders = BuildBackupFolderList(oldPath);
        var spaceCheck = BackupService.CheckSpace(oldPath, newPath, copyItems, backupFolders, _settings.EnableBackup);

        var currentDataFolder = FolderAnalyzer.FindEsDeDataFolder(oldPath);
        var packageDataFolder = FolderAnalyzer.FindEsDeDataFolder(newPath);
        var renameDataFolder = !string.IsNullOrWhiteSpace(currentDataFolder) &&
                               !string.IsNullOrWhiteSpace(packageDataFolder) &&
                               !string.Equals(currentDataFolder, packageDataFolder, StringComparison.OrdinalIgnoreCase);

        return new UpdatePlan(copyItems, backupFolders, spaceCheck, currentDataFolder, packageDataFolder, renameDataFolder);
    }

    private const int MaxPreviewItems = 8;

    private static List<string> BuildDeleteList(string oldPath)
    {
        var items = new List<string>();

        foreach (var dirPath in Directory.EnumerateDirectories(oldPath))
        {
            var name = Path.GetFileName(dirPath);
            if (!ExcludedFromCopy.Contains(name))
            {
                items.Add(name);
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(oldPath))
        {
            var name = Path.GetFileName(filePath);
            if (!ExcludedFromCopy.Contains(name))
            {
                items.Add(name);
            }
        }

        return items;
    }

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

    private string BuildConfirmationMessage(string oldPath, string newPath, UpdatePlan plan)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("UPDATE PREVIEW");
        sb.AppendLine();

        sb.AppendLine("Current:");
        sb.AppendLine($"  {(_currentVersion is not null ? $"v{_currentVersion}" : "unknown version")}");
        sb.AppendLine($"  {oldPath}");

        if (!FolderAnalyzer.HasEsDeExecutable(oldPath))
        {
            sb.AppendLine("  \u26a0 ES-DE.exe NOT FOUND \u2014 REPAIR MODE.");
            sb.AppendLine("  The package executable will be installed into the Current folder.");
        }

        sb.AppendLine();

        sb.AppendLine("Package:");
        sb.AppendLine($"  {(_packageVersion is not null ? $"v{_packageVersion}" : "unknown version")}");
        sb.AppendLine($"  {newPath}");
        sb.AppendLine();

        if (_direction is UpdateDirection.Upgrade or UpdateDirection.Downgrade &&
            _currentVersion is not null && _packageVersion is not null)
        {
            sb.AppendLine($"{_direction}: v{_currentVersion} \u2192 v{_packageVersion}");
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

        if (plan.RenameDataFolder)
        {
            sb.AppendLine("DATA FOLDER RENAME");
            sb.AppendLine();
            sb.AppendLine($"  {plan.CurrentDataFolder} \u2192 {plan.PackageDataFolder}");
            sb.AppendLine();
            sb.AppendLine(BuildDataFolderRenameMessage(plan.CurrentDataFolder!, plan.PackageDataFolder!, _currentVersion, _packageVersion));
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

        return sb.ToString();
    }

    private async Task ExecuteUpdateAsync(string oldPath, string newPath, UpdatePlan plan)
    {
        AppendStatus(BuildVersionLine("Current ES-DE", _currentVersion));
        AppendStatus(BuildVersionLine("Package", _packageVersion));

        if (!FolderAnalyzer.HasEsDeExecutable(oldPath))
        {
            AppendStatus("⚠ ES-DE executable not found in the Current folder — repair mode: the package executable will be installed.");
        }

        if (_direction is UpdateDirection.Upgrade or UpdateDirection.Downgrade &&
            !string.IsNullOrEmpty(_currentVersion) && !string.IsNullOrEmpty(_packageVersion))
        {
            AppendStatus($"→ {_direction} detected: {_currentVersion} → {_packageVersion}.");
        }

        if (_settings.EnableBackup && plan.BackupFolders.Count > 0)
        {
            await BackupService.CreateBackupAsync(oldPath, plan.BackupFolders, AppendStatus);
            _settings.LastBackupLocation = Path.Combine(oldPath, FolderNames.Backup);
            AppendStatus("✔ Backup created.");
        }
        else if (_settings.EnableBackup)
        {
            AppendStatus("⚠ No folders selected for backup — nothing to back up.");
        }
        else
        {
            AppendStatus("⚠ Backup disabled — no backup created.");
        }

        if (plan.RenameDataFolder)
        {
            VerifySealAgainstDisk(oldPath);
            await RenameDataFolderAsync(oldPath, plan.CurrentDataFolder!, plan.PackageDataFolder!);
        }

        VerifySealAgainstDisk(oldPath);
        await Task.Run(() => DeleteOldProgramFiles(oldPath, AppendStatus));

        VerifySealAgainstDisk(oldPath);
        foreach (var itemName in plan.CopyItems)
        {
            var source = Path.Combine(newPath, itemName);
            var destination = Path.Combine(oldPath, itemName);

            if (Directory.Exists(source))
            {
                AppendStatus($"Copying {itemName}...");

                var result = await RobocopyService.CopyTreeAsync(
                    source,
                    destination,
                    AppendStatus);

                if (!result.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Robocopy failed while copying {itemName}. Exit code: {result.ExitCode}");
                }

                AppendStatus($"✔ Copying {itemName} completed.");
            }
            else if (File.Exists(source))
            {
                if (string.Equals(
                    Path.GetFullPath(destination),
                    Path.GetFullPath(Application.ExecutablePath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    AppendStatus($"⚠ Skipping {itemName} (running from this file).");
                    continue;
                }

                AppendStatus($"Copying {itemName}...");
                await Task.Run(() => File.Copy(source, destination, overwrite: true));
                AppendStatus($"✔ Copying {itemName} completed.");
            }
            else
            {
                AppendStatus($"⚠ Skipping {itemName} (not found in the package).");
            }
        }

        AppendStatus("✔ Finished.");

        AppendStatus(string.Empty);
        AppendStatus("Next steps for ES-DE:");
        AppendStatus("  → Open ES-DE and go to Utilities → Create/update system directories");
        AppendStatus("    to register any new game systems added in this version.");
        AppendStatus("  → Open ES-DE → Theme Downloader to update themes for new system support.");
        AppendStatus("  → Ensure all customizations are in the ES-DE/custom_systems/ folder.");
        AppendStatus(string.Empty);

        _settings.LastOldPath = oldPath;
        _settings.LastNewPath = newPath;
        PersistSettings();

        if (_settings.AutoDeletePackage &&
            !string.IsNullOrWhiteSpace(_settings.LastPackageExtracted) &&
            !string.IsNullOrWhiteSpace(_settings.LastPackageZip) &&
            IsPathWithinOrEqual(newPath, _settings.LastPackageExtracted))
        {
            var cleared = TryDeleteDownloadedPackage(
                "auto-cleanup",
                clearPackagePath: true);

            if (cleared)
            {
                txtNewFolder.Clear();
                PersistSettings();
                AppendStatus("✔ Downloaded package removed automatically.");
            }
            else
            {
                AppendStatus("⚠ Downloaded package could not be fully removed — use Delete Package.");
            }
        }

        MessageBox.Show(
            "ES-DE update completed successfully.",
            "Update Complete",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void VerifySealAgainstDisk(string oldPath)
    {
        var canonical = PathSafety.Canonicalize(oldPath, out _);
        var currentIdentity = canonical is null
            ? null
            : PathSafety.GetDirectoryIdentity(canonical);

        if (_seal is null || currentIdentity is null || !currentIdentity.Value.Matches(_seal.Identity))
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

    private async Task RenameDataFolderAsync(string oldPath, string currentDataFolder, string packageDataFolder)
    {
        var source = Path.Combine(oldPath, currentDataFolder);
        var target = Path.Combine(oldPath, packageDataFolder);

        if (Directory.Exists(target))
        {
            AppendStatus($"⚠ Data folder rename skipped — \"{packageDataFolder}\" already exists in the current ES-DE folder. Your data folders stay as they are.");
            return;
        }

        AppendStatus($"→ Data folder rename: {currentDataFolder} → {packageDataFolder}.");
        await Task.Run(() => Directory.Move(source, target));
        AppendStatus("✔ Data folder renamed.");
    }

    private async void BtnDownloadLatest_Click(object? sender, EventArgs e)
    {
        if (_updateRunning)
        {
            return;
        }

        UpdateDirectionUi();

        _updateRunning = true;
        SetControlsEnabled(false);
        txtStatusLog.Clear();

        try
        {
            var releaseInfo = await FetchLatestReleaseAsync();
            if (releaseInfo is null)
            {
                return;
            }

            var latestVersion = EsDeVersionService.TryParse(releaseInfo.Version);
            var currentVersion = EsDeVersionService.TryParse(_currentVersion);

            var isOutdated = latestVersion is not null && currentVersion is not null &&
                             currentVersion.CompareTo(latestVersion) < 0;
            var hasCurrent = currentVersion is not null;

            AppendStatus($"→ Latest stable version: v{releaseInfo.Version}.");
            if (_currentVersion is not null)
            {
                AppendStatus($"→ Current ES-DE version: v{_currentVersion}.");
            }

            var (confirmMessage, confirmTitle) = BuildDownloadConfirmation(
                releaseInfo,
                _currentVersion,
                isOutdated,
                hasCurrent);

            var confirm = MessageBox.Show(
                confirmMessage,
                confirmTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                AppendStatus("Download cancelled.");
                return;
            }

            await ExecuteDownloadAsync(releaseInfo);
        }
        catch (Exception ex)
        {
            AppendStatus($"✖ Download failed: {ex.Message}");
            MessageBox.Show(
                ex.Message + Environment.NewLine + Environment.NewLine +
                "Check your internet connection and try again.",
                "Download Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _updateRunning = false;
            progressDownload.Visible = false;
            SetControlsEnabled(true);
            UpdateBackupUi();
            UpdateDirectionUi();
        }
    }

    private async Task<EsDeReleaseInfo?> FetchLatestReleaseAsync()
    {
        AppendStatus("→ Checking for the latest release on GitLab...");

        var releaseInfo = await ReleaseService.GetLatestReleaseAsync();
        if (releaseInfo is null)
        {
            AppendStatus("✖ Could not find the latest release on GitLab.");
            MessageBox.Show(
                "Could not find the latest ES-DE release on GitLab." + Environment.NewLine +
                Environment.NewLine +
                "Check your internet connection and try again.",
                "Check Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return null;
        }

        return releaseInfo;
    }

    private static (string Message, string Title) BuildDownloadConfirmation(
        EsDeReleaseInfo releaseInfo,
        string? currentVersion,
        bool isOutdated,
        bool hasCurrent)
    {
        if (isOutdated)
        {
            return (
                $"A new version is available: v{currentVersion} → v{releaseInfo.Version}." + Environment.NewLine +
                Environment.NewLine +
                $"File: {releaseInfo.FileName}{Environment.NewLine}" +
                $"Size: (determined at download){Environment.NewLine}" +
                Environment.NewLine +
                "Download the package, extract it, and set it as the Upgrade/Downgrade Package?",
                "Download Latest");
        }

        if (hasCurrent)
        {
            return (
                $"Your ES-DE is already up to date (v{currentVersion})." + Environment.NewLine +
                Environment.NewLine +
                $"Latest stable: v{releaseInfo.Version}{Environment.NewLine}" +
                $"File: {releaseInfo.FileName}{Environment.NewLine}" +
                Environment.NewLine +
                "Download anyway? This can be used for a repair or reinstall.",
                "Download Anyway");
        }

        return (
            $"The latest stable version is v{releaseInfo.Version}." + Environment.NewLine +
            Environment.NewLine +
            $"File: {releaseInfo.FileName}{Environment.NewLine}" +
            Environment.NewLine +
            "Download, extract, and use it as the Upgrade/Downgrade Package?",
            "Download Latest");
    }

    private async Task ExecuteDownloadAsync(EsDeReleaseInfo releaseInfo)
    {
        var downloadFolder = ResolveDownloadFolder();

        var zipPath = Path.Combine(downloadFolder, releaseInfo.FileName);
        var versionFolder = Path.Combine(downloadFolder, $"ES-DE-{releaseInfo.Version}");
        var extractPath = versionFolder + "-extract";

        if (Directory.Exists(extractPath))
        {
            await Task.Run(() => Directory.Delete(extractPath, recursive: true));
        }

        progressDownload.Style = ProgressBarStyle.Blocks;
        progressDownload.Value = 0;
        progressDownload.Visible = true;
        _lastReportedDownloadPercent = -1;

        try
        {
            await ReleaseService.DownloadAsync(
                releaseInfo.DownloadUrl,
                zipPath,
                AppendStatus,
                UpdateDownloadProgress);

            var md5Valid = ReleaseService.VerifyMd5(zipPath, releaseInfo.Md5);
            if (md5Valid is null)
            {
                AppendStatus("⚠ MD5 checksum not available — verification skipped.");
            }
            else if (md5Valid is false)
            {
                AppendStatus("⚠ MD5 checksum mismatch — the downloaded file may be corrupt.");
                var proceed = MessageBox.Show(
                    "The downloaded file's MD5 checksum does not match the official release." + Environment.NewLine +
                    Environment.NewLine +
                    "Continue anyway?",
                    "Checksum Mismatch",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (proceed != DialogResult.Yes)
                {
                    await TryDeleteCorruptDownloadAsync(zipPath);
                    AppendStatus("✖ Download aborted due to checksum mismatch.");
                    return;
                }
            }
            else
            {
                AppendStatus("✔ MD5 checksum verified.");
            }

            var packageRoot = await Task.Run(() => ReleaseService.ExtractPackage(zipPath, extractPath, AppendStatus));

            var validationError = EsDeValidation.ValidateNewFolder(packageRoot);
            if (validationError is not null)
            {
                AppendStatus($"✖ Extracted package is not a valid ES-DE folder: {validationError}");
                MessageBox.Show(
                    validationError + Environment.NewLine + Environment.NewLine +
                    "The downloaded package could not be validated as an ES-DE portable folder.",
                    "Invalid Package",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            txtNewFolder.Text = packageRoot;
            _settings.LastPackageZip = zipPath;
            _settings.LastPackageExtracted = extractPath;
            UpdateDirectionUi();
            PersistSettings();
            UpdatePackageUi();

            AppendStatus($"✔ Latest package ready: {packageRoot}");
            AppendStatus($"✔ Zip saved to: {zipPath}");
            MessageBox.Show(
                "The latest ES-DE package is ready." + Environment.NewLine +
                Environment.NewLine +
                $"Version: v{releaseInfo.Version}{Environment.NewLine}" +
                $"Folder: {packageRoot}" + Environment.NewLine +
                Environment.NewLine +
                "Confirm the settings and press Start Upgrade to apply.",
                "Package Ready",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
            throw;
        }
    }

    private static async Task TryDeleteCorruptDownloadAsync(string zipPath)
    {
        await Task.Run(() =>
        {
            try
            {
                File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                Diagnostics.Report($"Could not delete corrupt download {zipPath}: {ex.Message}");
            }
        });
    }

    private string ResolveDownloadFolder()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "packages");
        Directory.CreateDirectory(folder);
        return folder;
    }

    private void UpdateDownloadProgress(long bytes, long? total)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<long, long?>(UpdateDownloadProgress), bytes, total);
            return;
        }

        if (total.HasValue && total.Value > 0)
        {
            if (progressDownload.Style != ProgressBarStyle.Blocks)
            {
                progressDownload.Style = ProgressBarStyle.Blocks;
            }

            var percent = (int)(bytes * 100 / total.Value);
            percent = Math.Clamp(percent, 0, 100);
            progressDownload.Value = percent;

            var step = percent / 10 * 10;
            if (step > _lastReportedDownloadPercent)
            {
                _lastReportedDownloadPercent = step;
                AppendStatus($"→ Downloading... {step}%");
            }
        }
        else if (progressDownload.Style != ProgressBarStyle.Marquee)
        {
            progressDownload.Style = ProgressBarStyle.Marquee;
        }
    }

    private List<string> BuildCopyItemList(string packagePath)
    {
        var items = new List<string>();

        foreach (var directory in Directory.EnumerateDirectories(packagePath))
        {
            var name = Path.GetFileName(directory);
            if (!ExcludedFromCopy.Contains(name))
            {
                items.Add(name);
            }
        }

        foreach (var file in Directory.EnumerateFiles(packagePath))
        {
            items.Add(Path.GetFileName(file));
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
            folders.Add(FolderAnalyzer.FindEsDeDataFolder(oldPath) ?? FolderNames.EsDe);
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

    private void DeleteOldProgramFiles(string oldPath, Action<string>? onStatus = null)
    {
        var itemsDeleted = 0;
        var itemsFailed = 0;

        foreach (var dirPath in Directory.EnumerateDirectories(oldPath))
        {
            var name = Path.GetFileName(dirPath);

            if (ExcludedFromCopy.Contains(name))
            {
                continue;
            }

            onStatus?.Invoke($"Deleting {name}...");

            try
            {
                Directory.Delete(dirPath, recursive: true);
                onStatus?.Invoke($"✔ Deleted {name}.");
                itemsDeleted++;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"⚠ Could not delete {name}: {ex.Message}");
                itemsFailed++;
            }
        }

        foreach (var filePath in Directory.EnumerateFiles(oldPath))
        {
            var name = Path.GetFileName(filePath);

            if (ExcludedFromCopy.Contains(name))
            {
                continue;
            }

            onStatus?.Invoke($"Deleting {name}...");

            try
            {
                File.Delete(filePath);
                onStatus?.Invoke($"✔ Deleted {name}.");
                itemsDeleted++;
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"⚠ Could not delete {name}: {ex.Message}");
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

    private void UpdateDirectionUi()
    {
        _currentVersion = EsDeVersionService.TryGetDisplayVersion(txtOldFolder.Text.Trim());
        _packageVersion = EsDeVersionService.TryGetDisplayVersion(txtNewFolder.Text.Trim());

        lblOldVersion.Text = _currentVersion is null ? string.Empty : $"v{_currentVersion}";
        lblNewVersion.Text = _packageVersion is null ? string.Empty : $"v{_packageVersion}";

        var currentParsed = EsDeVersionService.TryParse(_currentVersion);
        var packageParsed = EsDeVersionService.TryParse(_packageVersion);

        if (currentParsed is null || packageParsed is null)
        {
            _direction = UpdateDirection.Unknown;
        }
        else
        {
            var comparison = packageParsed.CompareTo(currentParsed);
            _direction = comparison switch
            {
                > 0 => UpdateDirection.Upgrade,
                < 0 => UpdateDirection.Downgrade,
                _ => UpdateDirection.Same
            };
        }

        btnStartUpdate.Text = _direction switch
        {
            UpdateDirection.Upgrade => "Start Upgrade",
            UpdateDirection.Downgrade => "Start Downgrade",
            UpdateDirection.Same => "Start Repair",
            _ => "Start"
        };
    }

    private static string BuildVersionLine(string label, string? version) =>
        version is null ? $"✔ {label} verified." : $"✔ {label} verified (v{version}).";

    private static string BuildDataFolderRenameMessage(string sourceName, string targetName, string? currentVersion, string? packageVersion)
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

    private void UpdateBackupUi()
    {
        var backupPath = _settings.LastBackupLocation;
        var backupExists = !string.IsNullOrWhiteSpace(backupPath) && Directory.Exists(backupPath);

        if (!backupExists)
        {
            _settings.LastBackupLocation = string.Empty;
        }

        btnDeleteBackup.Enabled = backupExists;

        if (!_settings.EnableBackup)
        {
            lblBackupStatus.Text = "Backup: Off";
            lblBackupStatus.ForeColor = ThemeService.ResolveColor(_settings.Theme, Color.Firebrick, Color.FromArgb(170, 170, 170));
            return;
        }

        var folders = BuildBackupFolderList(txtOldFolder.Text.Trim());
        if (folders.Count == 0)
        {
            lblBackupStatus.Text = "Backup: On — no folders selected";
            lblBackupStatus.ForeColor = ThemeService.ResolveColor(_settings.Theme, Color.DarkOrange, Color.FromArgb(210, 210, 210));
            return;
        }

        lblBackupStatus.Text = backupExists
            ? $"Backup: On — {backupPath} ({string.Join(", ", folders)})"
            : $"Backup: On — {string.Join(", ", folders)}";
        lblBackupStatus.ForeColor = ThemeService.ResolveColor(_settings.Theme, Color.SeaGreen, Color.White);
    }

    private void BtnDeleteBackup_Click(object? sender, EventArgs e)
    {
        var backupPath = _settings.LastBackupLocation;

        if (string.IsNullOrWhiteSpace(backupPath) || !Directory.Exists(backupPath))
        {
            UpdateBackupUi();
            return;
        }

        var confirm = MessageBox.Show(
            "Delete the backup folder?" + Environment.NewLine +
            Environment.NewLine +
            backupPath + Environment.NewLine +
            Environment.NewLine +
            "This permanently removes the backup. This cannot be undone.",
            "Delete Backup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            Directory.Delete(backupPath, recursive: true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The backup folder could not be deleted." + Environment.NewLine +
                Environment.NewLine + backupPath + Environment.NewLine +
                Environment.NewLine + ex.Message,
                "Delete Failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _settings.LastBackupLocation = string.Empty;
        PersistSettings();
        UpdateBackupUi();
        AppendStatus("✔ Backup deleted.");
    }

    private void BtnDeletePackage_Click(object? sender, EventArgs e)
    {
        var hasZip = !string.IsNullOrWhiteSpace(_settings.LastPackageZip) &&
                     File.Exists(_settings.LastPackageZip);
        var hasExtracted = !string.IsNullOrWhiteSpace(_settings.LastPackageExtracted) &&
                           Directory.Exists(_settings.LastPackageExtracted);

        if (!hasZip && !hasExtracted)
        {
            UpdatePackageUi();
            return;
        }

        var description =
            (hasZip ? "ZIP: " + _settings.LastPackageZip + Environment.NewLine : string.Empty) +
            (hasExtracted ? "Extracted: " + _settings.LastPackageExtracted + Environment.NewLine : string.Empty);

        var confirm = MessageBox.Show(
            "Delete the downloaded package?" + Environment.NewLine +
            Environment.NewLine +
            description.TrimEnd() + Environment.NewLine +
            Environment.NewLine +
            "This permanently removes the downloaded files. This cannot be undone.",
            "Delete Package",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        var clearedNewPath = TryDeleteDownloadedPackage(
            "manual",
            clearPackagePath: hasExtracted &&
                               IsPathWithinOrEqual(txtNewFolder.Text.Trim(), _settings.LastPackageExtracted));

        if (clearedNewPath)
        {
            txtNewFolder.Clear();
            UpdateDirectionUi();
        }

        PersistSettings();

        var zipStillExists = !string.IsNullOrWhiteSpace(_settings.LastPackageZip) &&
                             File.Exists(_settings.LastPackageZip);
        var extractedStillExists = !string.IsNullOrWhiteSpace(_settings.LastPackageExtracted) &&
                                   Directory.Exists(_settings.LastPackageExtracted);

        AppendStatus(!zipStillExists && !extractedStillExists
            ? "✔ Package deleted."
            : "⚠ Some package files could not be deleted.");
    }

    private void UpdatePackageUi()
    {
        var hasZip = !string.IsNullOrWhiteSpace(_settings.LastPackageZip) &&
                     File.Exists(_settings.LastPackageZip);
        var hasExtracted = !string.IsNullOrWhiteSpace(_settings.LastPackageExtracted) &&
                           Directory.Exists(_settings.LastPackageExtracted);

        if (!hasZip)
        {
            _settings.LastPackageZip = string.Empty;
        }

        if (!hasExtracted)
        {
            _settings.LastPackageExtracted = string.Empty;
        }

        btnDeletePackage.Enabled = hasZip || hasExtracted;
    }

    private bool TryDeleteDownloadedPackage(string reason, bool clearPackagePath)
    {
        var zip = !string.IsNullOrWhiteSpace(_settings.LastPackageZip)
            ? _settings.LastPackageZip
            : null;
        var extracted = !string.IsNullOrWhiteSpace(_settings.LastPackageExtracted)
            ? _settings.LastPackageExtracted
            : null;

        var zipGone = true;
        if (!string.IsNullOrEmpty(zip) && File.Exists(zip))
        {
            try
            {
                File.Delete(zip);
            }
            catch (Exception ex)
            {
                AppendStatus($"⚠ Could not delete ZIP ({reason}): {ex.Message}");
                zipGone = false;
            }
        }

        var extractedGone = true;
        if (!string.IsNullOrEmpty(extracted) && Directory.Exists(extracted))
        {
            try
            {
                Directory.Delete(extracted, recursive: true);
            }
            catch (Exception ex)
            {
                AppendStatus($"⚠ Could not delete extracted package ({reason}): {ex.Message}");
                extractedGone = false;
            }
        }

        if (zipGone)
        {
            _settings.LastPackageZip = string.Empty;
        }

        if (extractedGone)
        {
            _settings.LastPackageExtracted = string.Empty;
        }

        UpdatePackageUi();

        return clearPackagePath && extractedGone;
    }

    private void PersistSettings()
    {
        _settings.LastOldPath = txtOldFolder.Text.Trim();
        _settings.LastNewPath = txtNewFolder.Text.Trim();

        if (!SettingsService.Save(_settings))
        {
            AppendStatus("⚠ Could not save settings.json.");
        }
    }

    private void ApplyTheme()
    {
        ThemeService.ApplyTheme(this, _settings.Theme);
    }

    private static string GetInitialBrowsePath(string currentPath)
    {
        if (!string.IsNullOrWhiteSpace(currentPath) && Directory.Exists(currentPath))
        {
            return currentPath;
        }

        return AppContext.BaseDirectory;
    }

    private bool HasBackup() =>
        !string.IsNullOrWhiteSpace(_settings.LastBackupLocation) &&
        Directory.Exists(_settings.LastBackupLocation);

    private static bool IsPathWithinOrEqual(string path, string container)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(container))
        {
            return false;
        }

        var pathFull = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var containerFull = Path.GetFullPath(container).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return pathFull.Equals(containerFull, StringComparison.OrdinalIgnoreCase) ||
               pathFull.StartsWith(containerFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private bool HasDownloadedPackage() =>
        (!string.IsNullOrWhiteSpace(_settings.LastPackageZip) &&
         File.Exists(_settings.LastPackageZip)) ||
        (!string.IsNullOrWhiteSpace(_settings.LastPackageExtracted) &&
         Directory.Exists(_settings.LastPackageExtracted));

    private void SetControlsEnabled(bool enabled)
    {
        btnBrowseOld.Enabled = enabled;
        btnBrowseNew.Enabled = enabled;
        btnStartUpdate.Enabled = enabled;
        btnSettings.Enabled = enabled;
        btnDownloadLatest.Enabled = enabled;
        btnDeleteBackup.Enabled = enabled && HasBackup();
        btnDeletePackage.Enabled = enabled && HasDownloadedPackage();

        if (!enabled)
        {
            txtStatusLog.Focus();
        }
    }

    private void AppendStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendStatus), message);
            return;
        }

        txtStatusLog.AppendText(message + Environment.NewLine);
    }
}
