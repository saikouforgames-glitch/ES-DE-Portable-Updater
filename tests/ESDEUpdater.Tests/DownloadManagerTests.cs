namespace ESDEUpdater.Tests;

public class DownloadManagerTests
{
    private static EsDeReleaseInfo CreateRelease(string version = "3.0.0") =>
        new()
        {
            Version = version,
            FileName = $"es-de-{version}-win32-portable.zip",
            DownloadUrl = $"https://example.com/es-de-{version}-win32-portable.zip",
            Md5 = "0123456789abcdef0123456789abcdef"
        };

    [Fact]
    public void BuildDownloadConfirmation_OutdatedStateProposesVersionChange()
    {
        var release = CreateRelease();

        var (message, title) = DownloadManager.BuildDownloadConfirmation(
            release, currentVersion: "2.0.0", isOutdated: true, hasCurrent: true);

        Assert.Equal("Download Latest", title);
        Assert.Contains("A new version is available", message);
        Assert.Contains("v2.0.0 \u2192 v3.0.0", message);
        Assert.Contains(release.FileName, message);
    }

    [Fact]
    public void BuildDownloadConfirmation_UpToDateStateAllowsDownloadAnyway()
    {
        var release = CreateRelease();

        var (message, title) = DownloadManager.BuildDownloadConfirmation(
            release, currentVersion: "3.0.0", isOutdated: false, hasCurrent: true);

        Assert.Equal("Download Anyway", title);
        Assert.Contains("already up to date", message);
        Assert.Contains("Download anyway?", message);
    }

    [Fact]
    public void BuildDownloadConfirmation_NoCurrentStateShowsLatestVersion()
    {
        var release = CreateRelease();

        var (message, title) = DownloadManager.BuildDownloadConfirmation(
            release, currentVersion: null, isOutdated: false, hasCurrent: false);

        Assert.Equal("Download Latest", title);
        Assert.Contains("The latest stable version is v3.0.0.", message);
        Assert.Contains(release.FileName, message);
    }
}