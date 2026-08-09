namespace ESDEUpdater;

public static class FolderNames
{
    public const string Emulators = "Emulators";
    public const string EsDe = "ES-DE";
    public const string EmulationStation = ".emulationstation";
    public const string Roms = "ROMs";
    public const string RomsAll = "ROMs_ALL";
    public const string Backup = "Backup";
    public const string Updater = "ES-DE Updater";

    public static readonly string[] KnownDataFolders = [EsDe, EmulationStation];

    /// <summary>
    /// Top-level folder names that the update must never delete or copy over.
    /// Referenced by the delete sweep, the location gate, and the running-program guard.
    /// </summary>
    public static readonly string[] PreservedFolders =
    [
        Emulators,
        EsDe,
        EmulationStation,
        Roms,
        Backup,
        Updater
    ];
}
