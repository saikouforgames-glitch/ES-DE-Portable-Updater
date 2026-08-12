namespace ESDEUpdater.Tests;

public class UpdateOrchestratorTests
{
    private static UpdateOrchestrator CreateOrchestrator(
        AppSettings? settings = null,
        HashSet<string>? exclusions = null,
        List<string>? status = null) =>
        new(
            settings ?? new AppSettings(),
            exclusions ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            status is null ? _ => { } : status.Add);

    private static void CreateDataFolder(string root, string name)
    {
        Directory.CreateDirectory(Path.Combine(root, name));
        File.WriteAllText(Path.Combine(root, name, "settings.xml"), string.Empty);
        File.WriteAllText(Path.Combine(root, name, "es_settings.txt"), string.Empty);
    }

    [Fact]
    public void BuildUpdatePlan_DetectsDataFolderRename()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        CreateDataFolder(oldRoot, FolderNames.EsDe);
        CreateDataFolder(newRoot, FolderNames.EmulationStation);
        File.WriteAllText(Path.Combine(newRoot, "ES-DE.exe"), string.Empty);

        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);

        Assert.True(plan.RenameDataFolder);
        Assert.Equal(FolderNames.EsDe, plan.CurrentDataFolder);
        Assert.Equal(FolderNames.EmulationStation, plan.PackageDataFolder);
    }

    [Fact]
    public void BuildUpdatePlan_NoRenameWhenDataFolderNamesMatch()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        CreateDataFolder(oldRoot, FolderNames.EsDe);
        CreateDataFolder(newRoot, FolderNames.EsDe);

        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);

        Assert.False(plan.RenameDataFolder);
    }

    [Fact]
    public void BuildUpdatePlan_CopyItemsExcludePreservedAndCustomExclusions()
    {
        using var tmp = new UnprotectedTemp();
        var newRoot = tmp.CreateDirectory("pkg");
        CreateDataFolder(newRoot, FolderNames.EsDe);
        Directory.CreateDirectory(Path.Combine(newRoot, FolderNames.Emulators));
        Directory.CreateDirectory(Path.Combine(newRoot, FolderNames.Roms));
        Directory.CreateDirectory(Path.Combine(newRoot, "custom_systems"));
        File.WriteAllText(Path.Combine(newRoot, "ES-DE.exe"), string.Empty);
        File.WriteAllText(Path.Combine(newRoot, "README.txt"), string.Empty);

        var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "custom_systems" };
        var plan = CreateOrchestrator(exclusions: exclusions).BuildUpdatePlan(tmp.Root, newRoot);

        Assert.Contains("ES-DE.exe", plan.CopyItems);
        Assert.Contains("README.txt", plan.CopyItems);
        Assert.DoesNotContain(FolderNames.Emulators, plan.CopyItems);
        Assert.DoesNotContain(FolderNames.Roms, plan.CopyItems);
        Assert.DoesNotContain(FolderNames.EsDe, plan.CopyItems);
        Assert.DoesNotContain("custom_systems", plan.CopyItems);
    }

    [Fact]
    public void BuildUpdatePlan_BackupFoldersHonorSettingsFlags()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        EsDeFixture.CreateOldInstallation(oldRoot);
        EsDeFixture.CreateFreshPackage(newRoot);

        var settings = new AppSettings
        {
            BackupEmulators = false,
            BackupEsDe = true,
            BackupRoms = false,
            BackupRomsAll = true
        };

        var plan = CreateOrchestrator(settings).BuildUpdatePlan(oldRoot, newRoot);

        Assert.Equal(new List<string> { FolderNames.EsDe, FolderNames.RomsAll }, plan.BackupFolders);
    }

    [Fact]
    public void BuildUpdatePlan_RedirectedDataFolderUsesTopLevelSegment()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        Directory.CreateDirectory(Path.Combine(oldRoot, "data", FolderNames.EsDe));
        File.WriteAllText(Path.Combine(oldRoot, FolderAnalyzer.PortableTxt), "data");
        EsDeFixture.CreateFreshPackage(newRoot);

        var settings = new AppSettings
        {
            BackupEmulators = false,
            BackupEsDe = true,
            BackupRoms = false,
            BackupRomsAll = false
        };

        var plan = CreateOrchestrator(settings).BuildUpdatePlan(oldRoot, newRoot);

        Assert.Equal(new List<string> { "data" }, plan.BackupFolders);
    }

    [Fact]
    public void BuildConfirmationMessage_ContainsRepairModeBanner()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        EsDeFixture.CreateOldInstallation(oldRoot);
        EsDeFixture.CreateFreshPackage(newRoot);
        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);

        var message = CreateOrchestrator().BuildConfirmationMessage(
            oldRoot, newRoot, plan, currentVersion: null, packageVersion: "3.0.0",
            UpdateDirection.Unknown, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains("REPAIR MODE", message);
    }

    [Fact]
    public void BuildConfirmationMessage_ContainsDataFolderRenameSection()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        CreateDataFolder(oldRoot, FolderNames.EsDe);
        CreateDataFolder(newRoot, FolderNames.EmulationStation);
        File.WriteAllText(Path.Combine(newRoot, "ES-DE.exe"), string.Empty);
        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);

        var message = CreateOrchestrator().BuildConfirmationMessage(
            oldRoot, newRoot, plan, currentVersion: "2.0.0", packageVersion: "3.0.0",
            UpdateDirection.Upgrade, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        Assert.Contains("DATA FOLDER RENAME", message);
        Assert.Contains($"{FolderNames.EsDe} \u2192 {FolderNames.EmulationStation}", message);
    }

    [Fact]
    public void BuildConfirmationMessage_ContainsExcludedSection()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        EsDeFixture.CreateOldInstallation(oldRoot);
        EsDeFixture.CreateFreshPackage(newRoot);
        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);

        var exclusions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "roms.bak" };
        var message = CreateOrchestrator().BuildConfirmationMessage(
            oldRoot, newRoot, plan, currentVersion: null, packageVersion: "3.0.0",
            UpdateDirection.Unknown, exclusions);

        Assert.Contains("EXCLUDED \u2014 KEPT", message);
        Assert.Contains("roms.bak", message);
    }

    [Fact]
    public async Task ExecuteUpdateAsync_ThrowsWhenRenameTargetAlreadyExists()
    {
        using var tmp = new UnprotectedTemp();
        var oldRoot = tmp.CreateDirectory("old");
        var newRoot = tmp.CreateDirectory("new");
        CreateDataFolder(oldRoot, FolderNames.EsDe);
        CreateDataFolder(oldRoot, FolderNames.EmulationStation);
        File.WriteAllText(Path.Combine(oldRoot, "ES-DE.exe"), string.Empty);
        CreateDataFolder(newRoot, FolderNames.EmulationStation);
        File.WriteAllText(Path.Combine(newRoot, "ES-DE.exe"), string.Empty);

        var plan = CreateOrchestrator().BuildUpdatePlan(oldRoot, newRoot);
        var status = new List<string>();
        var orchestrator = CreateOrchestrator(exclusions: new HashSet<string>(StringComparer.OrdinalIgnoreCase), status: status);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.ExecuteUpdateAsync(oldRoot, newRoot, plan, seal: null));

        Assert.True(Directory.Exists(Path.Combine(oldRoot, FolderNames.EsDe)));
        Assert.True(Directory.Exists(Path.Combine(oldRoot, FolderNames.EmulationStation)));
    }

    [Fact]
    public void BuildVersionLine_FormatsVerifiedAndVersionedOutput()
    {
        Assert.Equal("\u2714 Current ES-DE verified.", UpdateOrchestrator.BuildVersionLine("Current ES-DE", null));
        Assert.Equal("\u2714 Package verified (v3.0.0).", UpdateOrchestrator.BuildVersionLine("Package", "3.0.0"));
    }
}