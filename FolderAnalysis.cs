namespace ESDEUpdater;

public sealed class FolderAnalysis
{
    public string RootPath { get; init; } = string.Empty;
    public bool FolderExists { get; init; }
    public bool HasEsDeExecutable { get; init; }
    public bool HasEsDeDataFolder { get; init; }
    public int EmulatorFolderCount { get; init; }
    public int RomFileCount { get; init; }
    public bool HasRomFiles => RomFileCount > 0;
    public bool EmulatorsIsEmpty => EmulatorFolderCount == 0;
}
