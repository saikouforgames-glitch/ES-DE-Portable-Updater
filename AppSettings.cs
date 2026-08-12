namespace ESDEUpdater;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public sealed class AppSettings
{
    public string LastOldPath { get; set; } = string.Empty;
    public string LastNewPath { get; set; } = string.Empty;
    public bool RememberLastFolders { get; set; } = true;
    public bool RememberExclusions { get; set; } = true;
    public List<string> ExcludedTopLevelNames { get; set; } = [];
    public bool EnableBackup { get; set; } = false;
    public bool BackupEmulators { get; set; } = true;
    public bool BackupEsDe { get; set; } = true;
    public bool BackupRoms { get; set; } = true;
    public bool BackupRomsAll { get; set; } = false;
    public string LastBackupLocation { get; set; } = string.Empty;
    public bool AutoDeletePackage { get; set; } = true;
    public string LastPackageZip { get; set; } = string.Empty;
    public string LastPackageExtracted { get; set; } = string.Empty;
    public AppThemeMode Theme { get; set; } = AppThemeMode.System;
}
