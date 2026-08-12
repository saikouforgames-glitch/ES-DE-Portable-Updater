namespace ESDEUpdater.Tests;

/// <summary>
/// A temporary directory created OUTSIDE the protected areas that ValidationGate
/// refuses (Windows, Program Files, ProgramData, the user profile, $Recycle.Bin),
/// so validation tests exercise the real structural gates instead of being
/// refused by location alone. Deletes itself on dispose.
/// </summary>
public sealed class UnprotectedTemp : IDisposable
{
    public string Root { get; }

    public UnprotectedTemp()
    {
        Root = CreateRoot();
        Directory.CreateDirectory(Root);
    }

    private static string CreateRoot()
    {
        var candidates = new List<string>();

        var baseDrive = Path.GetPathRoot(AppContext.BaseDirectory);
        var systemDrive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        if (!string.IsNullOrWhiteSpace(baseDrive))
        {
            candidates.Add(baseDrive);
        }

        if (!string.IsNullOrWhiteSpace(systemDrive))
        {
            candidates.Add(systemDrive);
        }

        foreach (var drive in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var dir = Path.Combine(drive, "ESDEUpdater.Tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch
            {
                // Try the next candidate drive.
            }
        }

        throw new InvalidOperationException("No writable non-protected drive found for the test.");
    }

    public string Child(params string[] segments) => Path.Combine(segments.Prepend(Root).ToArray());

    public string CreateDirectory(params string[] segments)
    {
        var path = Child(segments);
        Directory.CreateDirectory(path);
        return path;
    }

    public string CreateFile(string relativePath, string content = "")
    {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (content.Length > 0)
        {
            File.WriteAllText(path, content);
        }
        else
        {
            File.WriteAllBytes(path, Array.Empty<byte>());
        }

        return path;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; leftover test folders are harmless.
        }
    }
}

/// <summary>
/// Builds realistic ES-DE portable folder structures for validation tests.
/// </summary>
public static class EsDeFixture
{
    public static void CreateOldInstallation(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Emulators", "RetroArch-Win64"));
        Directory.CreateDirectory(Path.Combine(root, "Emulators", "DuckStation"));
        Directory.CreateDirectory(Path.Combine(root, "ROMs", "nes"));
        Directory.CreateDirectory(Path.Combine(root, "ROMs", "snes"));
        Directory.CreateDirectory(Path.Combine(root, "ES-DE"));
        File.WriteAllText(Path.Combine(root, "ROMs", "nes", "game1.nes"), "x");
        File.WriteAllText(Path.Combine(root, "ROMs", "snes", "game2.sfc"), "y");
    }

    public static void CreateFreshPackage(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "Emulators"));
        Directory.CreateDirectory(Path.Combine(root, "ROMs"));
        Directory.CreateDirectory(Path.Combine(root, "ES-DE"));
        File.WriteAllText(Path.Combine(root, "ES-DE.exe"), string.Empty);
    }
}