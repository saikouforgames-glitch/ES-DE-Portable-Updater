namespace ESDEUpdater;

public partial class MainForm : Form
{
    private AppSettings _settings = new();
    private HashSet<string> _exclusions = new(StringComparer.OrdinalIgnoreCase);
    private bool _updateRunning;
    private UpdateDirection _direction = UpdateDirection.Unknown;
    private string? _currentVersion;
    private string? _packageVersion;
    private CancellationTokenSource? _updateCancellation;
    private int _lastReportedDownloadPercent = -1;
    private readonly DownloadManager _downloadManager;

    public MainForm()
    {
        InitializeComponent();
        Diagnostics.Log = AppendStatus;
        _downloadManager = new DownloadManager(
            AppendStatus,
            (message, title) =>
                MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes);
    }

    private UpdateOrchestrator CreateOrchestrator() =>
        new(_settings, _exclusions, AppendStatus);

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
        RestoreExclusions();
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

            // The user chose to close mid-update: stop the running copy/backup
            // operations instead of letting them continue invisible after the
            // window is gone. Fast local steps (rename, delete) are not cancelled.
            _updateCancellation?.Cancel();
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

    private void BtnAdvanced_Click(object? sender, EventArgs e)
    {
        var oldPath = txtOldFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(oldPath) || !Directory.Exists(oldPath))
        {
            MessageBox.Show(
                "Select a valid Current ES-DE folder first, then open Advanced.",
                "Advanced",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var gateError = ValidationGate.CheckOldLocation(oldPath);
        if (gateError is not null)
        {
            MessageBox.Show(
                gateError,
                "Invalid Current ES-DE Folder",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        using var dialog = new AdvancedForm(_settings, oldPath, _exclusions);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _exclusions = new HashSet<string>(dialog.ExclusionNames, StringComparer.OrdinalIgnoreCase);
        _settings.ExcludedTopLevelNames = _exclusions
            .Where(name => !FolderNames.IsPreservedTopLevel(name))
            .ToList();
        _settings.RememberExclusions = dialog.RememberExclusions;
        PersistSettings();

        AppendStatus($"Advanced: {_exclusions.Count} excluded item(s) will be kept (not deleted, not overwritten).");
    }

    /// <summary>
    /// Restores the persisted exclusion list and validates it against the
    /// remembered Current folder: names that no longer exist are dropped and
    /// reported, mirroring the saved-folder warning behavior.
    /// </summary>
    private void RestoreExclusions()
    {
        _exclusions.Clear();

        if (!_settings.RememberExclusions)
        {
            return;
        }

        _exclusions.UnionWith(_settings.ExcludedTopLevelNames);

        var current = txtOldFolder.Text.Trim();
        if (string.IsNullOrWhiteSpace(current) || !Directory.Exists(current))
        {
            return;
        }

        var stale = _exclusions.Where(name => !TopLevelExists(current, name)).ToList();
        foreach (var name in stale)
        {
            _exclusions.Remove(name);
        }

        if (stale.Count > 0)
        {
            _settings.ExcludedTopLevelNames = _exclusions
                .Where(name => !FolderNames.IsPreservedTopLevel(name))
                .ToList();
            AppendStatus(
                $"⚠ {stale.Count} excluded item(s) from the last session no longer exist in the Current folder " +
                $"and were ignored: {string.Join(", ", stale)}");
        }

        EnsureAutoExclusions(current);
    }

    /// <summary>
    /// Keeps portable.txt redirects safe without user interaction: when
    /// portable.txt points the data folder somewhere inside the Current folder,
    /// that location and the portable.txt file itself are excluded from both
    /// the delete sweep and the package copy.
    /// </summary>
    private void EnsureAutoExclusions(string oldPath)
    {
        if (string.IsNullOrWhiteSpace(oldPath) || !Directory.Exists(oldPath))
        {
            return;
        }

        var added = false;
        var redirectBase = FolderAnalyzer.TryResolvePortableDataBase(oldPath, out var topLevelSegment);
        if (redirectBase is not null)
        {
            if (_exclusions.Add(FolderAnalyzer.PortableTxt))
            {
                added = true;
            }

            if (topLevelSegment is not null && _exclusions.Add(topLevelSegment))
            {
                added = true;
            }
        }

        if (added)
        {
            _settings.ExcludedTopLevelNames = _exclusions
                .Where(name => !FolderNames.IsPreservedTopLevel(name))
                .ToList();
        }
    }

    private static bool TopLevelExists(string root, string name) =>
        Directory.Exists(Path.Combine(root, name)) || File.Exists(Path.Combine(root, name));

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

        EnsureAutoExclusions(oldPath);

        UpdatePlan plan;
        try
        {
            plan = CreateOrchestrator().BuildUpdatePlan(oldPath, newPath);
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
            CreateOrchestrator().BuildConfirmationMessage(
                oldPath, newPath, plan, _currentVersion, _packageVersion, _direction, _exclusions),
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

        var seal = new OldFolderSeal(oldIdentity.Value);

        _updateRunning = true;
        _updateCancellation = new CancellationTokenSource();
        SetControlsEnabled(false);
        txtStatusLog.Clear();

        try
        {
            var updateToken = _updateCancellation.Token;
            await ExecuteUpdateAsync(oldPath, newPath, plan, seal, updateToken);
        }
        catch (OperationCanceledException)
        {
            AppendStatus("↳ Update cancelled — the update was stopped. Run the update again to complete it.");
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
            _updateCancellation?.Dispose();
            _updateCancellation = null;
            SetControlsEnabled(true);
            UpdateBackupUi();
            UpdatePackageUi();
            UpdateDirectionUi();
        }
    }

private async Task ExecuteUpdateAsync(
        string oldPath,
        string newPath,
        UpdatePlan plan,
        OldFolderSeal seal,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AppendStatus(UpdateOrchestrator.BuildVersionLine("Current ES-DE", _currentVersion));
        AppendStatus(UpdateOrchestrator.BuildVersionLine("Package", _packageVersion));

        if (!FolderAnalyzer.HasEsDeExecutable(oldPath))
        {
            AppendStatus("⚠ ES-DE executable not found in the Current folder — repair mode: the package executable will be installed.");
        }

        if (_direction is UpdateDirection.Upgrade or UpdateDirection.Downgrade &&
            !string.IsNullOrEmpty(_currentVersion) && !string.IsNullOrEmpty(_packageVersion))
        {
            AppendStatus($"→ {_direction} detected: {_currentVersion} → {_packageVersion}.");
        }

        await CreateOrchestrator().ExecuteUpdateAsync(oldPath, newPath, plan, seal, cancellationToken);

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
            var releaseInfo = await _downloadManager.FetchLatestReleaseAsync();
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

            var (confirmMessage, confirmTitle) = DownloadManager.BuildDownloadConfirmation(
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

            progressDownload.Style = ProgressBarStyle.Blocks;
            progressDownload.Value = 0;
            progressDownload.Visible = true;
            _lastReportedDownloadPercent = -1;

            var result = await _downloadManager.ExecuteDownloadAsync(releaseInfo, UpdateDownloadProgress);
            if (result.Aborted)
            {
                return;
            }

            if (result.Error is not null)
            {
                MessageBox.Show(
                    result.Error + Environment.NewLine + Environment.NewLine +
                    "The downloaded package could not be validated as an ES-DE portable folder.",
                    "Invalid Package",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            txtNewFolder.Text = result.PackageRoot!;
            _settings.LastPackageZip = result.ZipPath!;
            _settings.LastPackageExtracted = result.ExtractPath!;
            UpdateDirectionUi();
            PersistSettings();
            UpdatePackageUi();

            MessageBox.Show(
                "The latest ES-DE package is ready." + Environment.NewLine +
                Environment.NewLine +
                $"Version: v{releaseInfo.Version}{Environment.NewLine}" +
                $"Folder: {result.PackageRoot}" + Environment.NewLine +
                Environment.NewLine +
                "Confirm the settings and press Start Upgrade to apply.",
                "Package Ready",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
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
        btnAdvanced.Enabled = enabled;
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
