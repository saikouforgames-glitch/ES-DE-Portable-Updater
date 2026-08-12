namespace ESDEUpdater.Tests;

public class EsDeValidationTests : IDisposable
{
    private readonly UnprotectedTemp _tmp = new();
    private string _old = string.Empty;
    private string _new = string.Empty;

    public void Dispose() => _tmp.Dispose();

    private void BuildValidSetup()
    {
        _old = _tmp.CreateDirectory("old");
        EsDeFixture.CreateOldInstallation(_old);
        _new = _tmp.CreateDirectory("new");
        EsDeFixture.CreateFreshPackage(_new);
    }

    [Fact]
    public void ValidateOldFolder_AcceptsValidInstallation()
    {
        BuildValidSetup();
        Assert.Null(EsDeValidation.ValidateOldFolder(_old));
    }

    [Fact]
    public void ValidateOldFolder_RejectsMissingRomsFolder()
    {
        BuildValidSetup();
        Directory.Delete(Path.Combine(_old, "ROMs"), recursive: true);

        var error = EsDeValidation.ValidateOldFolder(_old);

        Assert.NotNull(error);
        Assert.Contains("ROMs", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOldFolder_RejectsMissingDataFolder()
    {
        BuildValidSetup();
        Directory.Delete(Path.Combine(_old, "ES-DE"), recursive: true);

        var error = EsDeValidation.ValidateOldFolder(_old);

        Assert.NotNull(error);
        Assert.Contains("user data", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateOldFolder_DetectsDataFolderSelection()
    {
        BuildValidSetup();

        var error = EsDeValidation.ValidateOldFolder(Path.Combine(_old, "ES-DE"));

        Assert.NotNull(error);
        Assert.Contains("user data folder", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateNewFolder_AcceptsValidPackage()
    {
        BuildValidSetup();
        Assert.Null(EsDeValidation.ValidateNewFolder(_new));
    }

    [Fact]
    public void ValidateNewFolder_RejectsMissingExecutable()
    {
        BuildValidSetup();
        File.Delete(Path.Combine(_new, "ES-DE.exe"));

        var error = EsDeValidation.ValidateNewFolder(_new);

        Assert.NotNull(error);
        Assert.Contains("executable", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_SucceedsForValidSetup()
    {
        BuildValidSetup();
        Assert.True(EsDeValidation.ValidateForUpdate(_old, _new).IsSuccess);
    }

    [Fact]
    public void ValidateForUpdate_RejectsEmptyPaths()
    {
        var result = EsDeValidation.ValidateForUpdate(string.Empty, string.Empty);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ValidateForUpdate_RejectsMissingOldFolder()
    {
        BuildValidSetup();
        var missing = _tmp.Child("does-not-exist");

        var result = EsDeValidation.ValidateForUpdate(missing, _new);

        Assert.False(result.IsSuccess);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_DetectsReversedFolders()
    {
        BuildValidSetup();
        File.WriteAllText(Path.Combine(_old, "ES-DE.exe"), string.Empty);

        var result = EsDeValidation.ValidateForUpdate(_new, _old);

        Assert.False(result.IsSuccess);
        Assert.Contains("reversed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_RejectsTwoFreshFolders()
    {
        BuildValidSetup();
        var anotherFresh = _tmp.CreateDirectory("another-fresh");
        EsDeFixture.CreateFreshPackage(anotherFresh);

        var result = EsDeValidation.ValidateForUpdate(_new, anotherFresh);

        Assert.False(result.IsSuccess);
        Assert.Contains("newly extracted", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_RejectsEmptyEmulatorsInOld()
    {
        BuildValidSetup();
        Directory.Delete(Path.Combine(_old, "Emulators", "RetroArch-Win64"), recursive: true);
        Directory.Delete(Path.Combine(_old, "Emulators", "DuckStation"), recursive: true);

        var result = EsDeValidation.ValidateForUpdate(_old, _new);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty Emulators", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_RejectsRomFilesInPackage()
    {
        BuildValidSetup();
        Directory.CreateDirectory(Path.Combine(_new, "ROMs", "nes"));
        File.WriteAllText(Path.Combine(_new, "ROMs", "nes", "game.nes"), "x");

        var result = EsDeValidation.ValidateForUpdate(_old, _new);

        Assert.False(result.IsSuccess);
        Assert.Contains("ROM file", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_RejectsEmulatorFoldersInPackage()
    {
        BuildValidSetup();
        Directory.CreateDirectory(Path.Combine(_new, "Emulators", "PCSX2-Win64"));

        var result = EsDeValidation.ValidateForUpdate(_old, _new);

        Assert.False(result.IsSuccess);
        Assert.Contains("emulator folder", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateForUpdate_AcceptsRepairWhenExecutableMissingInOld()
    {
        BuildValidSetup();
        File.WriteAllText(Path.Combine(_old, "Emulators", "RetroArch-Win64", "retroarch.exe"), string.Empty);

        var result = EsDeValidation.ValidateForUpdate(_old, _new);

        Assert.True(result.IsSuccess, result.Message);
    }
}