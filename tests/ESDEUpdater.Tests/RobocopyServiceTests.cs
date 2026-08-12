namespace ESDEUpdater.Tests;

public class RobocopyServiceTests
{
    [Fact]
    public async Task CopyTreeAsync_CopiesContents()
    {
        using var tmp = new UnprotectedTemp();
        var src = tmp.CreateDirectory("src");
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        var dst = tmp.Child("dst");

        var result = await RobocopyService.CopyTreeAsync(src, dst);

        Assert.True(result.IsSuccess, result.Output);
        Assert.True(File.Exists(Path.Combine(dst, "a.txt")));
    }

    [Fact]
    public async Task CopyTreeAsync_ThrowsOperationCanceled_OnCallerCancellation()
    {
        using var tmp = new UnprotectedTemp();
        var src = tmp.CreateDirectory("src");
        Directory.CreateDirectory(Path.Combine(src, "data"));
        var payload = new byte[8 * 1024 * 1024];
        File.WriteAllBytes(Path.Combine(src, "data", "big-1.bin"), payload);
        File.WriteAllBytes(Path.Combine(src, "data", "big-2.bin"), payload);
        var dst = tmp.Child("dst");

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => RobocopyService.CopyTreeAsync(src, dst, null, cts.Token));
    }
}