using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace ESDEUpdater;

public sealed class EsDeReleaseInfo
{
    public string Version { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string DownloadUrl { get; init; } = string.Empty;
    public string? Md5 { get; init; }
}

public static class ReleaseService
{
    private const string ApiReleasesUrl =
        "https://gitlab.com/api/v4/projects/es-de%2Femulationstation-de/releases?per_page=8";

    private const string LatestReleaseJsonUrl =
        "https://gitlab.com/es-de/emulationstation-de/-/raw/master/latest_release.json";

    private const int HttpClientTimeoutSeconds = 60;
    private const int DownloadBufferSize = 81920;
    private const int DownloadStallTimeoutSeconds = 90;
    private const string UserAgentVersion = "1.1";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(HttpClientTimeoutSeconds)
        };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("ES-DE-Updater", UserAgentVersion));
        return client;
    }

    public static async Task<EsDeReleaseInfo?> GetLatestReleaseAsync()
    {
        var json = await HttpClient.GetStringAsync(ApiReleasesUrl);
        using var document = JsonDocument.Parse(json);

        foreach (var release in document.RootElement.EnumerateArray())
        {
            var releaseName = release.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var tagName = release.TryGetProperty("tag_name", out var tagProp) ? tagProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(releaseName) || IsPrerelease(releaseName) ||
                string.IsNullOrWhiteSpace(tagName) || IsPrerelease(tagName))
            {
                continue;
            }

            if (release.TryGetProperty("upcoming_release", out var upcoming) && upcoming.GetBoolean())
            {
                continue;
            }

            var version = ParseVersion(tagName!);
            if (version is null)
            {
                continue;
            }

            var asset = FindPortableAsset(release);
            if (asset is null)
            {
                continue;
            }

            var md5 = await TryGetMd5Async(version);

            return new EsDeReleaseInfo
            {
                Version = version,
                FileName = asset.Value.Name,
                DownloadUrl = asset.Value.Url,
                Md5 = md5
            };
        }

        return null;
    }

    private static (string Name, string Url)? FindPortableAsset(JsonElement release)
    {
        if (!release.TryGetProperty("assets", out var assets) ||
            !assets.TryGetProperty("links", out var links))
        {
            return null;
        }

        foreach (var link in links.EnumerateArray())
        {
            var name = link.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var url = link.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;
            var directUrl = link.TryGetProperty("direct_asset_url", out var directProp) ? directProp.GetString() : null;

            var assetUrl = string.IsNullOrWhiteSpace(directUrl) ? url : directUrl;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(assetUrl))
            {
                continue;
            }

            if (IsWindowsPortableAsset(name))
            {
                return (name, assetUrl!);
            }
        }

        return null;
    }

    private static bool IsWindowsPortableAsset(string assetName) =>
        assetName.EndsWith("_Portable.zip", StringComparison.OrdinalIgnoreCase) &&
        assetName.Contains("-x64", StringComparison.OrdinalIgnoreCase);

    private static bool IsPrerelease(string text) =>
        text.Contains("beta", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("alpha", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("-rc", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("_rc", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("rc-", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("rc_", StringComparison.OrdinalIgnoreCase) ||
        text.Contains("pre", StringComparison.OrdinalIgnoreCase);

    private static string? ParseVersion(string tagName)
    {
        var start = 0;
        while (start < tagName.Length && !char.IsDigit(tagName[start]))
        {
            start++;
        }

        if (start >= tagName.Length)
        {
            return null;
        }

        var end = start;
        while (end < tagName.Length && (char.IsDigit(tagName[end]) || tagName[end] == '.'))
        {
            end++;
        }

        return end > start ? tagName[start..end] : null;
    }

    private static async Task<string?> TryGetMd5Async(string version)
    {
        try
        {
            var json = await HttpClient.GetStringAsync(LatestReleaseJsonUrl);
            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("stable", out var stable) ||
                !stable.TryGetProperty("version", out var stableVersion) ||
                stableVersion.GetString() != version ||
                !stable.TryGetProperty("packages", out var packages))
            {
                return null;
            }

            foreach (var package in packages.EnumerateArray())
            {
                var name = package.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                if (!string.Equals(name, "WindowsPortable", StringComparison.Ordinal))
                {
                    continue;
                }

                var md5 = package.TryGetProperty("md5", out var md5Prop) ? md5Prop.GetString() : null;
                return string.IsNullOrWhiteSpace(md5) ? null : md5.Trim();
            }
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not fetch the MD5 checksum from latest_release.json: {ex.Message}");
        }

        return null;
    }

    public static async Task DownloadAsync(
        string url,
        string destinationFile,
        Action<string>? onStatus = null,
        Action<long, long?>? onProgress = null)
    {
        onStatus?.Invoke($"Downloading: {Path.GetFileName(destinationFile)}...");

        using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalLength = response.Content.Headers.ContentLength;

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write);

        var buffer = new byte[DownloadBufferSize];
        long totalBytes = 0;
        int bytesRead;

        while (true)
        {
            using var stallCts = new CancellationTokenSource(TimeSpan.FromSeconds(DownloadStallTimeoutSeconds));
            try
            {
                bytesRead = await stream.ReadAsync(buffer, stallCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException(
                    $"Download stalled — no data received for {DownloadStallTimeoutSeconds} seconds. " +
                    "Check your internet connection and try again.");
            }

            if (bytesRead == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalBytes += bytesRead;
            onProgress?.Invoke(totalBytes, totalLength);
        }

        onStatus?.Invoke($"✔ Downloaded {DiskSpaceHelper.FormatBytes(totalBytes)}: {Path.GetFileName(destinationFile)}");
    }

    /// <summary>
    /// Verifies the MD5 checksum of a downloaded file.
    /// Returns <c>true</c>/<c>false</c> when a checksum was available and checked,
    /// or <c>null</c> when no expected checksum was supplied (verification skipped).
    /// </summary>
    public static bool? VerifyMd5(string filePath, string? expectedMd5)
    {
        if (string.IsNullOrWhiteSpace(expectedMd5))
        {
            return null;
        }

        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hash = Convert.ToHexString(md5.ComputeHash(stream));
        return string.Equals(hash, expectedMd5, StringComparison.OrdinalIgnoreCase);
    }

    public static string ExtractPackage(string zipPath, string extractDirectory, Action<string>? onStatus = null)
    {
        onStatus?.Invoke($"Extracting to: {extractDirectory}...");

        ZipFile.ExtractToDirectory(zipPath, extractDirectory, overwriteFiles: true);

        var subDirectories = Directory.GetDirectories(extractDirectory);
        var files = Directory.GetFiles(extractDirectory);

        if (subDirectories.Length == 1)
        {
            return subDirectories[0];
        }

        return extractDirectory;
    }
}