namespace ESDEUpdater.Tests;

public class ValidationGateTests
{
    private static readonly string SystemDrive =
        Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))!;

    [Fact]
    public void CheckOldLocation_RejectsNullAndEmpty()
    {
        Assert.NotNull(ValidationGate.CheckOldLocation(null));
        Assert.NotNull(ValidationGate.CheckOldLocation(string.Empty));
        Assert.NotNull(ValidationGate.CheckOldLocation("   "));
    }

    [Fact]
    public void CheckOldLocation_RejectsDriveRoot()
    {
        Assert.NotNull(ValidationGate.CheckOldLocation(SystemDrive));
    }

    [Fact]
    public void CheckOldLocation_RejectsUserProfile()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.NotNull(ValidationGate.CheckOldLocation(profile));
    }

    [Fact]
    public void CheckOldLocation_RejectsProgramFiles()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        Assert.NotNull(ValidationGate.CheckOldLocation(programFiles));
    }

    [Fact]
    public void CheckOldLocation_AcceptsUnprotectedTempFolder()
    {
        using var tmp = new UnprotectedTemp();
        Assert.Null(ValidationGate.CheckOldLocation(tmp.Root));
    }

    [Fact]
    public void CheckOldLocation_RejectsItsOwnUpdaterFolder()
    {
        using var tmp = new UnprotectedTemp();
        var updater = tmp.CreateDirectory("ES-DE Updater");

        Assert.NotNull(ValidationGate.CheckOldLocation(updater, updaterFolderOverride: updater));
    }

    [Fact]
    public void CheckOldLocation_RejectsFolderInsideUpdaterFolder()
    {
        using var tmp = new UnprotectedTemp();
        var updater = tmp.CreateDirectory("ES-DE Updater");
        var inside = tmp.CreateDirectory("ES-DE Updater", "data");

        Assert.NotNull(ValidationGate.CheckOldLocation(inside, updaterFolderOverride: updater));
    }

    [Fact]
    public void CheckOldLocation_RejectsUpdaterUnderSweepableTopLevel()
    {
        using var tmp = new UnprotectedTemp();
        var old = tmp.CreateDirectory("ES-DE");
        var updater = tmp.CreateDirectory("ES-DE", "datafiles", "ES-DE Updater");

        var error = ValidationGate.CheckOldLocation(old, updaterFolderOverride: updater);

        Assert.NotNull(error);
        Assert.Contains("datafiles", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckOldLocation_AcceptsUpdaterUnderPreservedTopLevel()
    {
        using var tmp = new UnprotectedTemp();
        var old = tmp.CreateDirectory("ES-DE");
        var updater = tmp.CreateDirectory("ES-DE", "Emulators", "ES-DE Updater");

        Assert.Null(ValidationGate.CheckOldLocation(old, updaterFolderOverride: updater));
    }

    [Fact]
    public void CheckNewLocation_RejectsDriveRoot()
    {
        Assert.NotNull(ValidationGate.CheckNewLocation(SystemDrive));
    }

    [Fact]
    public void CheckNewLocation_AcceptsUnprotectedTempFolder()
    {
        using var tmp = new UnprotectedTemp();
        Assert.Null(ValidationGate.CheckNewLocation(tmp.Root));
    }

    [Fact]
    public void CheckRelationship_RejectsSamePhysicalFolder()
    {
        using var tmp = new UnprotectedTemp();
        var dir = tmp.CreateDirectory("ES-DE");

        Assert.NotNull(ValidationGate.CheckRelationship(dir, dir + @"\"));
    }

    [Fact]
    public void CheckRelationship_RejectsNestedPackage()
    {
        using var tmp = new UnprotectedTemp();
        var old = tmp.CreateDirectory("ES-DE");
        var pkg = tmp.CreateDirectory("ES-DE", "packages", "pkg");

        Assert.NotNull(ValidationGate.CheckRelationship(old, pkg));
    }

    [Fact]
    public void CheckRelationship_AcceptsPackageInsideUpdaterFolder()
    {
        using var tmp = new UnprotectedTemp();
        var old = tmp.CreateDirectory("ES-DE");
        var pkg = tmp.CreateDirectory("ES-DE", "ES-DE Updater", "packages", "pkg");

        Assert.Null(ValidationGate.CheckRelationship(old, pkg));
    }

    [Fact]
    public void CheckRelationship_RejectsOldInsidePackage()
    {
        using var tmp = new UnprotectedTemp();
        var pkg = tmp.CreateDirectory("pkg");
        var old = tmp.CreateDirectory("pkg", "ES-DE");

        Assert.NotNull(ValidationGate.CheckRelationship(old, pkg));
    }

    [Fact]
    public void CheckRelationship_AcceptsIndependentFolders()
    {
        using var tmp = new UnprotectedTemp();
        Assert.Null(ValidationGate.CheckRelationship(tmp.CreateDirectory("old"), tmp.CreateDirectory("pkg")));
    }
}