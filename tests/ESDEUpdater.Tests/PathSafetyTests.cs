namespace ESDEUpdater.Tests;

public class PathSafetyTests
{
    [Theory]
    [InlineData(@"C:\ES-DE", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE\Games", @"C:\ES-DE")]
    [InlineData(@"C:/ES-DE/Games", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE\Games", @"C:/ES-DE")]
    [InlineData(@"C:/ES-DE/Games", @"C:/ES-DE")]
    [InlineData(@"C:\ES-DE\", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE", @"C:\ES-DE\")]
    [InlineData(@"C:\ES-DE\", @"C:/ES-DE/")]
    [InlineData(@"C:\Windows", @"C:\")]
    [InlineData(@"C:/Windows", @"C:\")]
    [InlineData(@"C:\Windows", @"C:/")]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:\ES-DE\Games", @"C:\")]
    [InlineData(@"C:\ES-DE\Games\..\data", @"C:\ES-DE")]
    public void IsWithinOrEqual_ReturnsTrue_ForContainedOrEqual(string path, string container)
    {
        Assert.True(PathSafety.IsWithinOrEqual(path, container));
    }

    [Theory]
    [InlineData(@"C:\ES-DE-Games", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE2", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE3\Games", @"C:\ES-DE")]
    [InlineData(@"C:/ES-DE-Games", @"C:\ES-DE")]
    [InlineData(@"C:\ES-DE\Games", @"C:\ES-DE\Utilities")]
    [InlineData(@"C:\Windows", @"C:\Program Files")]
    [InlineData(@"D:\anything", @"C:\")]
    public void IsWithinOrEqual_ReturnsFalse_ForNonContained(string path, string container)
    {
        Assert.False(PathSafety.IsWithinOrEqual(path, container));
    }

    [Theory]
    [InlineData(@"C:\", @"C:\")]
    [InlineData(@"C:/", @"C:\")]
    [InlineData(@"C://", @"C:\")]
    [InlineData(@"C:\\", @"C:\")]
    [InlineData(@"D:\", @"D:\")]
    [InlineData(@"D:/", @"D:\")]
    public void NormalizeForComparison_PreservesDriveRoots(string input, string expected)
    {
        Assert.Equal(expected, PathSafety.NormalizeForComparison(input));
    }

    [Fact]
    public void Canonicalize_RejectsNonexistentPath()
    {
        var missing = Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"));

        var result = PathSafety.Canonicalize(missing, out var error);

        Assert.Null(result);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("C:\\ES-DE\"bad")]
    [InlineData("C:\\ES-DE*bad")]
    [InlineData("C:\\ES-DE?bad")]
    [InlineData("C:\\ES-DE|bad")]
    [InlineData("C:\\ES-DE<bad")]
    [InlineData("C:\\ES-DE>bad")]
    [InlineData("C:\\ES-DE\rbad")]
    public void Canonicalize_RejectsUnsafeCharacters(string path)
    {
        Assert.Null(PathSafety.Canonicalize(path, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void Canonicalize_RejectsUncPath()
    {
        var result = PathSafety.Canonicalize(@"\\server\share\folder", out var error);

        Assert.Null(result);
        Assert.Contains("Network", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Canonicalize_RejectsGlobalRoot()
    {
        var result = PathSafety.Canonicalize(@"\\?\GLOBALROOT\Device\HarddiskVolume1", out var error);

        Assert.Null(result);
        Assert.Contains("GLOBALROOT", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Canonicalize_ResolvesValidDirectory()
    {
        using var tmp = new UnprotectedTemp();
        var dir = tmp.CreateDirectory("ES-DE");

        var result = PathSafety.Canonicalize(dir + @"\", out var error);

        Assert.Null(error);
        Assert.Equal(PathSafety.NormalizeForComparison(dir), PathSafety.NormalizeForComparison(result!));
    }

    [Fact]
    public void Canonicalize_ResolvesDirectorySymlink()
    {
        using var tmp = new UnprotectedTemp();
        var target = tmp.CreateDirectory("target");
        var link = tmp.Child("link");

        try
        {
            Directory.CreateSymbolicLink(link, target);
        }
        catch (Exception)
        {
            // Directory symlinks need admin rights or Developer Mode;
            // this environment cannot exercise reparse-point resolution.
            return;
        }

        var result = PathSafety.Canonicalize(link, out var error);

        Assert.Null(error);
        Assert.Equal(
            PathSafety.NormalizeForComparison(target),
            PathSafety.NormalizeForComparison(result!));
    }

    [Fact]
    public void DirectoryIdentity_Matches_ForSamePhysicalFolder()
    {
        using var tmp = new UnprotectedTemp();
        var dir = tmp.CreateDirectory("ES-DE");

        var a = PathSafety.GetDirectoryIdentity(PathSafety.Canonicalize(dir, out _)!);
        var b = PathSafety.GetDirectoryIdentity(PathSafety.Canonicalize(dir + @"\", out _)!);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.True(a.Value.Matches(b.Value));
        Assert.True(b.Value.Matches(a.Value));
    }

    [Fact]
    public void DirectoryIdentity_DoesNotMatch_ForDifferentFolders()
    {
        using var tmp = new UnprotectedTemp();
        var a = PathSafety.GetDirectoryIdentity(PathSafety.Canonicalize(tmp.CreateDirectory("a"), out _)!);
        var b = PathSafety.GetDirectoryIdentity(PathSafety.Canonicalize(tmp.CreateDirectory("b"), out _)!);

        Assert.False(a.Value.Matches(b.Value));
    }
}