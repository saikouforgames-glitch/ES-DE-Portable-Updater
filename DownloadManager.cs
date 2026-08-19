namespace ESDEUpdater;

/// <summary>
/// Handles the GitLab download flow: fetch latest release, download ZIP,
/// verify MD5, extract package. No UI dependencies — status and progress
/// are reported through callbacks.
/// </summary>
public sealed class DownloadManager
{
    private readonly Action<string> _onStatus;
    private readonly Func<string, string, bool?>? _onConfirmMismatch;

    public DownloadManager(
        Action<string> onStatus,
        Func<string, string, bool?>? onConfirmMismatch = null)
    {
        _onStatus = onStatus;
        _onConfirmMismatch = onConfirmMismatch;
    }

    public async Task<EsDeReleaseInfo?> FetchLatestReleaseAsync()
    {
        _onStatus("→ Checking for the latest release on GitLab...");

        var releaseInfo = await ReleaseService.GetLatestReleaseAsync();
        if (releaseInfo is null)
        {
            _onStatus("✖ Could not find the latest release on GitLab.");
        }

        return releaseInfo;
    }

    public static (string Message, string Title) BuildDownloadConfirmation(
        EsDeReleaseInfo releaseInfo,
        string? currentVersion,
        bool isOutdated,
        bool hasCurrent)
    {
        if (isOutdated)
        {
            return (
                $"A new version is available: v{currentVersion} \u2192 v{releaseInfo.Version}." + Environment.NewLine +
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

    public async Task<DownloadResult> ExecuteDownloadAsync(
        EsDeReleaseInfo releaseInfo,
        Action<long, long?> onProgress,
        CancellationToken cancellationToken = default)
    {
        var downloadFolder = ResolveDownloadFolder();

        var zipPath = Path.Combine(downloadFolder, releaseInfo.FileName);
        var versionFolder = Path.Combine(downloadFolder, $"ES-DE-{releaseInfo.Version}");
        var extractPath = versionFolder + "-extract";

        try
        {
            await ReleaseService.DownloadAsync(
                releaseInfo.DownloadUrl,
                zipPath,
                _onStatus,
                onProgress);

            var md5Valid = ReleaseService.VerifyMd5(zipPath, releaseInfo.Md5);
            if (md5Valid is null)
            {
                _onStatus("\u26a0 MD5 checksum not available \u2014 verification skipped.");
            }
            else if (md5Valid is false)
            {
                _onStatus("\u26a0 MD5 checksum mismatch \u2014 the downloaded file may be corrupt.");

                if (_onConfirmMismatch is not null)
                {
                    var confirmed = _onConfirmMismatch(
                        "The downloaded file's MD5 checksum does not match the official release." +
                        Environment.NewLine + Environment.NewLine + "Continue anyway?",
                        "Checksum Mismatch");

                    if (confirmed is not true)
                    {
                        await TryDeleteCorruptDownloadAsync(zipPath);
                        _onStatus("\u2716 Download aborted due to checksum mismatch.");
                        return new DownloadResult { Aborted = true };
                    }
                }
            }
            else
            {
                _onStatus("\u2714 MD5 checksum verified.");
            }

            var packageRoot = await Task.Run(
                () => ReleaseService.ExtractPackage(zipPath, extractPath, _onStatus),
                cancellationToken);

            var validationError = EsDeValidation.ValidateNewFolder(packageRoot);
            if (validationError is not null)
            {
                _onStatus($"\u2716 Extracted package is not a valid ES-DE folder: {validationError}");
                return new DownloadResult
                {
                    Error = validationError,
                    ZipPath = zipPath
                };
            }

            _onStatus($"\u2714 Latest package ready: {packageRoot}");
            _onStatus($"\u2714 Zip saved to: {zipPath}");

            return new DownloadResult
            {
                PackageRoot = packageRoot,
                ZipPath = zipPath,
                ExtractPath = extractPath
            };
        }
        catch
        {
            try
            {
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
            catch (Exception ex)
            {
                Diagnostics.Report($"Could not clean up failed download {zipPath}: {ex.Message}");
            }

            throw;
        }
    }

    public string ResolveDownloadFolder()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "packages");
        Directory.CreateDirectory(folder);
        return folder;
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

    /// <summary>
    /// Attempts to delete a downloaded package (ZIP and/or extracted folder).
    /// Non-existent items are treated as successfully deleted.
    /// Returns true when every non-null, existing item was removed.
    /// </summary>
    public static bool TryDeletePackage(
        string? zipPath,
        string? extractedPath,
        string? reason = null,
        Action<string>? onStatus = null)
    {
        var reasonSuffix = !string.IsNullOrEmpty(reason) ? $" ({reason})" : string.Empty;

        var zipGone = true;
        if (!string.IsNullOrEmpty(zipPath) && File.Exists(zipPath))
        {
            try
            {
                File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"\u26a0 Could not delete ZIP{reasonSuffix}: {ex.Message}");
                zipGone = false;
            }
        }

        var extractedGone = true;
        if (!string.IsNullOrEmpty(extractedPath) && Directory.Exists(extractedPath))
        {
            try
            {
                Directory.Delete(extractedPath, recursive: true);
            }
            catch (Exception ex)
            {
                onStatus?.Invoke($"\u26a0 Could not delete extracted package{reasonSuffix}: {ex.Message}");
                extractedGone = false;
            }
        }

        return zipGone && extractedGone;
    }
}

public sealed class DownloadResult
{
    public string? PackageRoot { get; init; }
    public string? ZipPath { get; init; }
    public string? ExtractPath { get; init; }
    public string? Error { get; init; }
    public bool Aborted { get; init; }
    public bool IsSuccess => PackageRoot is not null && Error is null && !Aborted;
}
