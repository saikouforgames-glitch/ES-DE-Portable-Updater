namespace ESDEUpdater.Tests;

public class DiskSpaceHelperTests
{
    [Fact]
    public void GetItemsSize_MeasuresCompleteTree()
    {
        using var tmp = new UnprotectedTemp();
        var resources = tmp.CreateDirectory("pkg", "resources");
        File.WriteAllText(Path.Combine(resources, "a.bin"), new string('x', 1024));
        File.WriteAllText(Path.Combine(resources, "b.bin"), new string('y', 2048));
        tmp.CreateFile(Path.Combine("pkg", "root.dat"), "z");

        var result = DiskSpaceHelper.GetItemsSize(tmp.Child("pkg"), new[] { "resources", "root.dat" });

        Assert.Equal(1024 + 2048 + 1, result.Bytes);
        Assert.Equal(0, result.UnmeasuredFiles);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void GetDirectoriesSize_ReportsCompleteResult()
    {
        using var tmp = new UnprotectedTemp();
        var dir = tmp.CreateDirectory("backup");
        File.WriteAllText(Path.Combine(dir, "f.bin"), new string('a', 4096));

        var result = DiskSpaceHelper.GetDirectoriesSize(tmp.Root, new[] { "backup" });

        Assert.Equal(4096, result.Bytes);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void GetDirectoriesSize_MissingFolderIsZeroAndComplete()
    {
        using var tmp = new UnprotectedTemp();
        var result = DiskSpaceHelper.GetDirectoriesSize(tmp.Root, new[] { "missing" });

        Assert.Equal(0, result.Bytes);
        Assert.True(result.IsComplete);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(2048, "2.00 KB")]
    [InlineData(5 * 1024 * 1024, "5.00 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3.00 GB")]
    public void FormatBytes_FormatsUnits(long bytes, string expected)
    {
        Assert.Equal(expected, DiskSpaceHelper.FormatBytes(bytes));
    }
}