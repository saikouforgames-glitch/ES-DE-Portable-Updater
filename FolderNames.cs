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

    /// <summary>
    /// True when a top-level item is part of the updater's own footprint:
    /// the "ES-DE Updater" folder, its executable, or any versioned variant.
    /// Mirrors the prefix rule used by executable and version detection.
    /// </summary>
    public static bool IsUpdaterEntry(string? name) =>
        !string.IsNullOrEmpty(name) &&
        name.StartsWith(Updater, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when a top-level item name is never touched: one of the preserved
    /// folders or an updater-related entry. Single source for the running-program
    /// guard, the location gate, and the delete sweep.
    /// </summary>
    public static bool IsPreservedTopLevel(string? name) =>
        IsUpdaterEntry(name) || PreservedFolders.Contains(name, StringComparer.OrdinalIgnoreCase);
}
