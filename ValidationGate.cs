namespace ESDEUpdater;

/// <summary>
/// Safety gates for the two ES-DE folder selections.
///
/// Philosophy: any location that cannot be *proven* safe to run a destructive
/// update against is refused. False rejection is always preferred over
/// false acceptance. These gates run before the normal structural checks.
/// </summary>
public static class ValidationGate
{
    private static readonly string[] ProtectedSystemAreas = BuildProtectedSystemAreas();

    /// <summary>
    /// Top-level folders the destructive sweep will never delete
    /// (must always mirror the preserved list in FolderNames).
    /// </summary>
    private static readonly string[] SweepExcludedNames = FolderNames.PreservedFolders;

    private static string[] BuildProtectedSystemAreas()
    {
        var areas = new List<string>();

        void AddArea(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var trimmed = path.TrimEnd('\\', '/');
            if (trimmed.Length == 0)
            {
                return;
            }

            areas.Add(trimmed);
        }

        AddArea(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        AddArea(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        AddArea(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        AddArea(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        AddArea(profile);

        if (!string.IsNullOrWhiteSpace(profile))
        {
            var usersRoot = Path.GetDirectoryName(profile);
            if (usersRoot is not null &&
                string.Equals(Path.GetFileName(usersRoot), "Users", StringComparison.OrdinalIgnoreCase))
            {
                AddArea(usersRoot);
            }
        }

        var systemDriveRoot = Path.GetPathRoot(profile);
        if (!string.IsNullOrWhiteSpace(systemDriveRoot))
        {
            AddArea(Path.Combine(systemDriveRoot, "$Recycle.Bin"));
        }

        return areas.ToArray();
    }

    /// <summary>
    /// Hard gates for the destructive (Current ES-DE) folder.
    /// Returns null when safe, or a user-facing refusal.
    /// </summary>
    /// <param name="rawPath">The selected Current folder.</param>
    /// <param name="updaterFolderOverride">Test seam: the folder the updater runs
    /// from. When omitted, <see cref="AppContext.BaseDirectory"/> is used.</param>
    public static string? CheckOldLocation(string? rawPath, string? updaterFolderOverride = null)
    {
        var canonical = PathSafety.Canonicalize(rawPath, out var error);
        if (canonical is null)
        {
            return error;
        }

        if (PathSafety.IsDriveRoot(canonical))
        {
            return
                "The Current ES-DE folder is a drive root.\n\n" +
                $"  {canonical}\n\n" +
                "The updater removes files from the Current folder before installing the new version, " +
                "and a drive root is far too broad for that. Selecting a drive root would endanger " +
                "everything on that drive.\n\n" +
                "Please select the ES-DE portable folder itself (for example D:\\ES-DE).";
        }

        foreach (var area in ProtectedSystemAreas)
        {
            if (PathSafety.IsDriveRoot(area))
            {
                if (string.Equals(Trim(area), Trim(canonical), StringComparison.OrdinalIgnoreCase))
                {
                    return
                        "The Current ES-DE folder is the system drive root.\n\n" +
                        $"  {canonical}\n\n" +
                        "The updater can never modify this location.";
                }

                continue;
            }

            if (PathSafety.IsWithinOrEqual(canonical, area))
            {
                return
                    "The Current ES-DE folder is inside a protected operating system area.\n\n" +
                    $"  {canonical}\n\n" +
                    $"The updater will not modify folders inside:\n" +
                    $"  {area}\n\n" +
                    "A portable ES-DE installation lives in its own dedicated folder, not inside " +
                    "Windows, Program Files, ProgramData or the user profile. Please select that folder.";
            }
        }

        var updaterFolder = Trim(updaterFolderOverride ?? AppContext.BaseDirectory);

        if (string.Equals(canonical, updaterFolder, StringComparison.OrdinalIgnoreCase))
        {
            return
                "The Current ES-DE folder is the very folder this updater is running from.\n\n" +
                $"  {canonical}\n\n" +
                "Running an update here would delete the updater itself while it is executing. " +
                "Please select your ES-DE portable folder.";
        }

        if (PathSafety.IsWithinOrEqual(canonical, updaterFolder))
        {
            return
                "The Current ES-DE folder is inside the folder this updater is running from.\n\n" +
                $"  Current: {canonical}\n" +
                $"  Updater: {updaterFolder}\n\n" +
                "Running an update against that folder would destroy the updater itself. " +
                "Please select your ES-DE portable folder.";
        }

        if (PathSafety.IsWithinOrEqual(updaterFolder, canonical))
        {
            var relative = Path.GetRelativePath(canonical, updaterFolder);
            var topLevelName = relative
                .Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (topLevelName is null ||
                !SweepExcludedNames.Contains(topLevelName, StringComparer.OrdinalIgnoreCase))
            {
                return
                    "The Current ES-DE folder contains the folder this updater is running from, " +
                    "in a place the update would delete.\n\n" +
                    $"  Current: {canonical}\n" +
                    $"  Updater: {updaterFolder}\n\n" +
                    "Running this update would delete the updater itself. Move the updater out of " +
                    $"the {topLevelName} folder, keep it in one of the preserved folders " +
                    $"(Emulators, ROMs, {FolderNames.EsDe}, {FolderNames.Backup}, {FolderNames.Updater}), " +
                    "or choose a different Current ES-DE folder.";
            }
        }

        return null;
    }

    /// <summary>
    /// Hard gates for the package (Upgrade/Downgrade) folder, which is read-only
    /// from the updater's perspective, so system-container locations are allowed.
    /// Returns null when safe, or a user-facing refusal.
    /// </summary>
    public static string? CheckNewLocation(string? rawPath)
    {
        var canonical = PathSafety.Canonicalize(rawPath, out var error);
        if (canonical is null)
        {
            return error;
        }

        if (PathSafety.IsDriveRoot(canonical))
        {
            return
                "The Package folder is a drive root.\n\n" +
                $"  {canonical}\n\n" +
                "Please select the folder that contains the freshly extracted ES-DE package " +
                "(for example D:\\ES-DE-2.1.0-windows-x64-portable).";
        }

        return null;
    }

    /// <summary>
    /// Relationship checks between the two selections (both must already exist):
    /// they must be two different physical folders and neither may contain the other.
    /// Returns null when safe, or a user-facing refusal.
    /// </summary>
    public static string? CheckRelationship(string? oldRawPath, string? newRawPath)
    {
        var oldPath = PathSafety.Canonicalize(oldRawPath, out var oldError);
        if (oldPath is null)
        {
            return oldError;
        }

        var newPath = PathSafety.Canonicalize(newRawPath, out var newError);
        if (newPath is null)
        {
            return newError;
        }

        var oldIdentity = PathSafety.GetDirectoryIdentity(oldPath);
        if (oldIdentity is null)
        {
            return
                "The Current ES-DE folder could not be reopened for identity verification.\n\n" +
                $"  {oldPath}\n\n" +
                "This usually means the folder or its drive is not accessible right now. " +
                "Verify the folder is still present and try again.";
        }

        var newIdentity = PathSafety.GetDirectoryIdentity(newPath);
        if (newIdentity is null)
        {
            return
                "The Package folder could not be reopened for identity verification.\n\n" +
                $"  {newPath}\n\n" +
                "This usually means the folder or its drive is not accessible right now. " +
                "Verify the folder is still present and try again.";
        }

        if (oldIdentity.Value.Matches(newIdentity.Value))
        {
            return
                "The Current ES-DE and Package folders are the same physical folder.\n\n" +
                $"  {oldPath}\n\n" +
                "Even when two paths look different (links, short names or drive aliases), " +
                "this check proved they point to the same real folder.\n\n" +
                "You must select two different folders: your current installation and " +
                "the package you extracted.";
        }

        if (PathSafety.IsWithinOrEqual(newPath, oldPath))
        {
            return
                "The Package folder is inside the Current ES-DE folder.\n\n" +
                $"  Current: {oldPath}\n" +
                $"  Package: {newPath}\n\n" +
                "The updater removes old program files from the Current folder before installing, " +
                "which would delete the package you extracted there. Select a package location " +
                "outside your current installation.";
        }

        if (PathSafety.IsWithinOrEqual(oldPath, newPath))
        {
            return
                "The Current ES-DE folder is inside the Package folder.\n\n" +
                $"  Current: {oldPath}\n" +
                $"  Package: {newPath}\n\n" +
                "These two selections must be independent folders. Select your current installation " +
                "and the freshly extracted package as two separate, non-nested folders.";
        }

        return null;
    }

    private static string Trim(string path) => path.TrimEnd('\\', '/');
}