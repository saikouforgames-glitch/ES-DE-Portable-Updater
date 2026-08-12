using System.Diagnostics;
using System.Text;

namespace ESDEUpdater;

public sealed class RobocopyResult
{
    public int ExitCode { get; init; }
    public string Output { get; init; } = string.Empty;
    public bool IsSuccess => ExitCode < 8;
}

public static class RobocopyService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(30);

    public static async Task<RobocopyResult> CopyTreeAsync(
        string source,
        string destination,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"Source folder not found: {source}");
        }

        Directory.CreateDirectory(destination);

        var outputBuilder = new StringBuilder();

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "robocopy",
            Arguments = $"\"{source}\" \"{destination}\" /E /Z /NFL /NDL /NJH /NJS /R:1 /W:1 /XJ",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            outputBuilder.AppendLine(e.Data);
            onOutputLine?.Invoke(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            outputBuilder.AppendLine(e.Data);
            onOutputLine?.Invoke(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            // The timeout and the caller token share one linked source; kill on
            // both. On timeout this becomes a normal failed result; on caller
            // cancellation the exception propagates so the update pipeline stops.
            try { process.Kill(entireProcessTree: true); } catch { }

            if (timeoutCts.IsCancellationRequested)
            {
                return new RobocopyResult
                {
                    ExitCode = -1,
                    Output = outputBuilder + Environment.NewLine + "Robocopy timed out after 30 minutes."
                };
            }

            throw;
        }

        return new RobocopyResult
        {
            ExitCode = process.ExitCode,
            Output = outputBuilder.ToString()
        };
    }
}
