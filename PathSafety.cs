using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace ESDEUpdater;

/// <summary>
/// The physical identity of an existing directory on disk.
/// Two identities are equal only when they point to the exact same directory entry
/// (same volume serial and same file index). This defeats every alias class:
/// junctions, mount points, subst drives, symlink chains, 8.3 short names,
/// casing differences and trailing-dot/space variants.
/// </summary>
public readonly record struct DirectoryIdentity(uint VolumeSerialNumber, ulong FileIndex)
{
    public bool Matches(DirectoryIdentity other) =>
        VolumeSerialNumber == other.VolumeSerialNumber && FileIndex == other.FileIndex;
}

/// <summary>
/// Fail-closed path canonicalization for ES-DE folder validation.
///
/// Rules:
///  - The raw input is rejected when it contains characters or forms that cannot be
///    handled safely (quotes, wildcards, control characters, device paths, ...).
///  - The path is normalized (GetFullPath), expanded to its long form (8.3 names are
///    resolved) and every reparse point along the way is resolved to its final target.
///  - Network paths are rejected.
///  - Any step that cannot be *proven* safe returns null and an explanation.
/// </summary>
public static class PathSafety
{
    public const int MaxPathLength = 32_767;

    private const uint FileReadAttributes = 0x0080;
    private const uint FileShareRead = 0x1;
    private const uint FileShareWrite = 0x2;
    private const uint FileShareDelete = 0x4;
    private const uint OpenExisting = 0x3;
    private const uint FileFlagBackupSemantics = 0x02000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetLongPathNameW(string lpszShortPath, char[] lpszLongPath, uint cchBuffer);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out ByHandleFileInformation lpFileInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    /// <summary>
    /// Reduces a raw input path to its canonical, physical form.
    /// Returns null (with a user-facing error) when the path cannot be proven safe and valid.
    /// </summary>
    public static string? Canonicalize(string? input, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "The folder path is empty.";
            return null;
        }

        var trimmed = input.Trim();
        if (trimmed.Length > MaxPathLength)
        {
            error = $"The path is too long ({trimmed.Length:N0} characters).\n\n" +
                    "Windows paths may be at most 32,767 characters.";
            return null;
        }

        foreach (var c in trimmed)
        {
            if (c < 0x20 || c == '"' || c == '*' || c == '?' || c == '|' || c == '<' || c == '>')
            {
                error = "The path contains characters that cannot be handled safely " +
                        "(quotes, wildcards, control characters, \\| &lt; &gt;).\n\n" +
                        $"Path: {trimmed}";
                return null;
            }
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(trimmed);
        }
        catch (Exception ex)
        {
            error = $"The path could not be resolved.\n\n{ex.Message}\n\nPath: {trimmed}";
            return null;
        }

        if (fullPath.StartsWith(@"\\?\GLOBALROOT\", StringComparison.OrdinalIgnoreCase))
        {
            error = "The path refers to a reserved system device (GLOBALROOT) and cannot be used.\n\n" +
                    $"Path: {fullPath}";
            return null;
        }

        var longForm = GetLongPathName(fullPath) ?? fullPath;

        if (longForm.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
        {
            error = "Network locations are not supported.\n\n" +
                    $"Path: {longForm}\n\n" +
                    "The updater requires ES-DE folders on a local drive.";
            return null;
        }

        if (longForm.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase) && longForm.Length > 4)
        {
            longForm = longForm[4..];
        }

        if (longForm.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase))
        {
            error = "Network locations are not supported.\n\n" +
                    $"Path: {longForm}\n\n" +
                    "The updater requires ES-DE folders on a local drive.";
            return null;
        }

        var canonical = ResolvePhysical(longForm, out error);
        if (canonical is null)
        {
            return null;
        }

        canonical = TrimTrailingSeparators(canonical);

        if (canonical.Length == 0 || !Directory.Exists(canonical))
        {
            error = "This path does not exist or is not an existing folder.\n\n" +
                    $"Path: {canonical}";
            return null;
        }

        return canonical;
    }

    /// <summary>
    /// Walks every path segment and resolves every reparse point (junction, symlink)
    /// to its final physical target. Fail-closed: any segment that cannot be inspected
    /// or resolved aborts the whole operation.
    /// </summary>
    private static string? ResolvePhysical(string longPath, out string? error)
    {
        error = null;

        var root = Path.GetPathRoot(longPath);
        if (string.IsNullOrEmpty(root))
        {
            error = "The path is not absolute.\n\n" +
                    $"Path: {longPath}\n\n" +
                    "The updater only accepts complete paths such as C:\\ES-DE.";
            return null;
        }

        var rest = longPath[root.Length..];
        var parts = rest.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

        var current = root;
        var stepCount = parts.Length + 1;

        foreach (var part in parts)
        {
            if (stepCount-- <= 0)
            {
                error = "The path contains too many nested links and could not be resolved.\n\n" +
                        $"Path: {longPath}";
                return null;
            }

            var candidate = Path.Combine(current, part);

            bool isReparse;
            try
            {
                isReparse = (File.GetAttributes(candidate) & FileAttributes.ReparsePoint) != 0;
            }
            catch (Exception ex)
            {
                error = "The folder chain could not be inspected before the update.\n\n" +
                        $"Path: {candidate}\n\n{ex.Message}\n\n" +
                        "This usually means the drive or folder is not accessible right now.";
                return null;
            }

            if (!isReparse)
            {
                current = candidate;
                continue;
            }

            FileSystemInfo? target;
            try
            {
                target = new DirectoryInfo(candidate).ResolveLinkTarget(returnFinalTarget: true);
            }
            catch (Exception ex)
            {
                error = "A folder link could not be resolved.\n\n" +
                        $"Path: {candidate}\n\n{ex.Message}";
                return null;
            }

            if (target is null)
            {
                error = "A folder link points to a location that no longer exists or that " +
                        "cannot be resolved (for example a broken link or a link loop).\n\n" +
                        $"Path: {candidate}\n\n" +
                        "Because the updater deletes files, it cannot run through a link it cannot verify.";
                return null;
            }

            current = TrimTrailingSeparators(target.FullName);
        }

        return current;
    }

    /// <summary>
    /// Returns the physical identity (volume serial + file index) of a directory.
    /// Null when the folder cannot be opened — callers must treat null as a refusal.
    /// </summary>
    public static DirectoryIdentity? GetDirectoryIdentity(string canonicalPath)
    {
        using var handle = CreateFileW(
            canonicalPath,
            FileReadAttributes,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            return null;
        }

        if (!GetFileInformationByHandle(handle, out var info))
        {
            return null;
        }

        var fileIndex = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
        return new DirectoryIdentity(info.VolumeSerialNumber, fileIndex);
    }

    public static bool IsDriveRoot(string canonicalPath)
    {
        var trimmed = TrimTrailingSeparators(canonicalPath);
        var root = Path.GetPathRoot(canonicalPath);
        if (string.IsNullOrEmpty(root))
        {
            return false;
        }

        return string.Equals(trimmed, TrimTrailingSeparators(root), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when <paramref name="path"/> equals <paramref name="container"/> or lives
    /// inside it. Both arguments must already be canonical.
    /// </summary>
    public static bool IsWithinOrEqual(string path, string container)
    {
        var p = TrimTrailingSeparators(path);
        var c = TrimTrailingSeparators(container);

        if (string.Equals(p, c, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return p.StartsWith(c + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimTrailingSeparators(string path)
    {
        if (path.Length > 3 && (path.EndsWith('\\') || path.EndsWith('/')))
        {
            return path.TrimEnd('\\', '/');
        }

        return path;
    }

    private static string? GetLongPathName(string path)
    {
        try
        {
            var buffer = new char[1024];
            var size = GetLongPathNameW(path, buffer, (uint)buffer.Length);
            if (size == 0)
            {
                return null;
            }

            if (size > buffer.Length)
            {
                var longer = new char[size];
                var secondSize = GetLongPathNameW(path, longer, (uint)longer.Length);
                if (secondSize == 0 || secondSize > longer.Length)
                {
                    return null;
                }

                return new string(longer, 0, secondSize);
            }

            return new string(buffer, 0, size);
        }
        catch (Exception ex)
        {
            Diagnostics.Report($"Could not expand path '{path}': {ex.Message}");
            return null;
        }
    }
}