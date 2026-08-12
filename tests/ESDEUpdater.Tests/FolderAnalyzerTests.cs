namespace ESDEUpdater.Tests;

public class FolderAnalyzerTests
{
    [Fact]
    public void FindEsDeDataFolderInfo_DetectsEsDe()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory("ES-DE");

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(FolderNames.EsDe, info.Name);
        Assert.Equal(PathSafety.NormalizeForComparison(tmp.Root), PathSafety.NormalizeForComparison(info.BasePath));
    }

    [Fact]
    public void FindEsDeDataFolderInfo_DetectsLegacyEmulationStationFolder()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory(".emulationstation");

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(FolderNames.EmulationStation, info.Name);
    }

    [Fact]
    public void FindEsDeDataFolderInfo_PrefersEsDeWhenBothExist()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory("ES-DE");
        tmp.CreateDirectory(".emulationstation");

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(FolderNames.EsDe, info.Name);
    }

    [Fact]
    public void FindEsDeDataFolderInfo_ReturnsNullWithoutDataFolder()
    {
        using var tmp = new UnprotectedTemp();
        Assert.Null(FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root));
    }

    [Fact]
    public void FindEsDeDataFolderInfo_FollowsPortableRedirectInsideRoot()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory("data", "ES-DE");
        tmp.CreateFile(FolderAnalyzer.PortableTxt, "data");

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(FolderNames.EsDe, info.Name);
        Assert.Equal(
            PathSafety.NormalizeForComparison(tmp.Child("data")),
            PathSafety.NormalizeForComparison(info.BasePath));
    }

    [Fact]
    public void FindEsDeDataFolderInfo_FollowsPortableRedirectOutsideRoot()
    {
        using var tmp = new UnprotectedTemp();
        using var outside = new UnprotectedTemp();
        outside.CreateDirectory("ES-DE");
        tmp.CreateFile(FolderAnalyzer.PortableTxt, outside.Root);

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(
            PathSafety.NormalizeForComparison(outside.Root),
            PathSafety.NormalizeForComparison(info.BasePath));
    }

    [Fact]
    public void FindEsDeDataFolderInfo_IgnoresEmptyPortableTxt()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory("ES-DE");
        tmp.CreateFile(FolderAnalyzer.PortableTxt, string.Empty);

        var info = FolderAnalyzer.FindEsDeDataFolderInfo(tmp.Root);

        Assert.NotNull(info);
        Assert.Equal(
            PathSafety.NormalizeForComparison(tmp.Root),
            PathSafety.NormalizeForComparison(info.BasePath));
    }

    [Fact]
    public void TryResolvePortableDataBase_ReportsTopLevelSegmentForInsideRedirect()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateDirectory("data", "ignored");
        tmp.CreateFile(FolderAnalyzer.PortableTxt, "data");

        var basePath = FolderAnalyzer.TryResolvePortableDataBase(tmp.Root, out var topLevel);

        Assert.NotNull(basePath);
        Assert.Equal("data", topLevel);
    }

    [Fact]
    public void TryResolvePortableDataBase_HasNoSegmentForOutsideRedirect()
    {
        using var tmp = new UnprotectedTemp();
        using var outside = new UnprotectedTemp();
        tmp.CreateFile(FolderAnalyzer.PortableTxt, outside.Root);

        var basePath = FolderAnalyzer.TryResolvePortableDataBase(tmp.Root, out var topLevel);

        Assert.Equal(PathSafety.NormalizeForComparison(outside.Root), basePath);
        Assert.Null(topLevel);
    }

    [Fact]
    public void TryResolvePortableDataBase_ReturnsNullWithoutPortableTxt()
    {
        using var tmp = new UnprotectedTemp();
        var basePath = FolderAnalyzer.TryResolvePortableDataBase(tmp.Root, out var topLevel);

        Assert.Null(basePath);
        Assert.Null(topLevel);
    }

    [Theory]
    [InlineData("a", "a")]
    [InlineData("a\\b", "a")]
    [InlineData("a/b", "a")]
    public void GetTopLevelSegment_ReturnsFirstSegment(string relative, string expected)
    {
        using var tmp = new UnprotectedTemp();
        var dir = tmp.CreateDirectory("root");
        var target = Path.Combine(dir, relative);

        Assert.Equal(expected, FolderAnalyzer.GetTopLevelSegment(dir, target));
    }

    [Fact]
    public void GetTopLevelSegment_ReturnsNullForRootOrOutside()
    {
        using var tmp = new UnprotectedTemp();
        using var outside = new UnprotectedTemp();
        var root = tmp.CreateDirectory("root");

        Assert.Null(FolderAnalyzer.GetTopLevelSegment(root, root));
        Assert.Null(FolderAnalyzer.GetTopLevelSegment(root, outside.Root));
    }

    [Fact]
    public void HasEsDeExecutable_RecognizesModernAndLegacyNames()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateFile("ES-DE.exe");
        Assert.True(FolderAnalyzer.HasEsDeExecutable(tmp.Root));

        using var legacy = new UnprotectedTemp();
        legacy.CreateFile("EmulationStation.exe");
        Assert.True(FolderAnalyzer.HasEsDeExecutable(legacy.Root));
    }

    [Fact]
    public void HasEsDeExecutable_IgnoresUpdaterEntry()
    {
        using var tmp = new UnprotectedTemp();
        tmp.CreateFile("ES-DE Updater.exe");

        Assert.False(FolderAnalyzer.HasEsDeExecutable(tmp.Root));
    }

    [Fact]
    public void HasEsDeExecutable_ReturnsFalseWithoutExecutables()
    {
        using var tmp = new UnprotectedTemp();
        var root = tmp.CreateDirectory("ES-DE");

        Assert.False(FolderAnalyzer.HasEsDeExecutable(root));
    }

    [Fact]
    public void Analyze_CountsRomsIncludingExtensionlessFileCorrectly()
    {
        using var tmp = new UnprotectedTemp();
        EsDeFixture.CreateOldInstallation(tmp.Root);
        tmp.CreateFile(Path.Combine("ROMs", "nes", "readme"));

        var analysis = FolderAnalyzer.Analyze(
            tmp.Root,
            SupportedRomExtensions.GetSupportedExtensions(tmp.Root));

        Assert.True(analysis.FolderExists);
        Assert.Equal(2, analysis.RomFileCount);
        Assert.Equal(2, analysis.EmulatorFolderCount);
        Assert.True(analysis.HasEsDeDataFolder);
        Assert.False(analysis.HasEsDeExecutable);
    }

    [Fact]
    public void Analyze_ReturnsNonExistentFolder()
    {
        using var tmp = new UnprotectedTemp();
        var missing = tmp.Child("does-not-exist");

        var analysis = FolderAnalyzer.Analyze(missing, SupportedRomExtensions.GetSupportedExtensions(missing));

        Assert.False(analysis.FolderExists);
        Assert.Equal(0, analysis.RomFileCount);
        Assert.Equal(0, analysis.EmulatorFolderCount);
    }
}