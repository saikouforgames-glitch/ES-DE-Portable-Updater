namespace ESDEUpdater;

/// <summary>
/// The measured size of a folder tree or item list. UnmeasuredFiles counts
/// files that could not be read (locked, inaccessible); callers can distinguish
/// a complete calculation from a partial one.
/// </summary>
public readonly record struct DirectorySizeResult(long Bytes, int UnmeasuredFiles)
{
    public bool IsComplete => UnmeasuredFiles == 0;

    public static DirectorySizeResult operator +(DirectorySizeResult left, DirectorySizeResult right) =>
        new(left.Bytes + right.Bytes, left.UnmeasuredFiles + right.UnmeasuredFiles);
}

public static class DiskSpaceHelper
{
    public static long GetAvailableFreeSpace(string folderPath)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(folderPath))
            ?? throw new InvalidOperationException($"Could not determine drive for path: {folderPath}");

        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }

    public static DirectorySizeResult GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return default;
        }

        long size = 0;
        var unmeasured = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip inaccessible files, but report that the result is partial.
                    unmeasured++;
                }
            }
        }
        catch
        {
            return new DirectorySizeResult(size, unmeasured);
        }

        return new DirectorySizeResult(size, unmeasured);
    }

    public static DirectorySizeResult GetDirectoriesSize(string rootPath, IEnumerable<string> folderNames)
    {
        var total = new DirectorySizeResult(0, 0);
        foreach (var folderName in folderNames)
        {
            total += GetDirectorySize(Path.Combine(rootPath, folderName));
        }

        return total;
    }

    public static DirectorySizeResult GetItemsSize(string rootPath, IEnumerable<string> itemNames)
    {
        var total = new DirectorySizeResult(0, 0);

        foreach (var itemName in itemNames)
        {
            var fullPath = Path.Combine(rootPath, itemName);

            if (Directory.Exists(fullPath))
            {
                total += GetDirectorySize(fullPath);
            }
            else if (File.Exists(fullPath))
            {
                try
                {
                    total += new DirectorySizeResult(new FileInfo(fullPath).Length, 0);
                }
                catch
                {
                    // Skip inaccessible files, but report that the result is partial.
                    total += new DirectorySizeResult(0, 1);
                }
            }
        }

        return total;
    }

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{size:N0} {units[unitIndex]}"
            : $"{size:N2} {units[unitIndex]}";
    }
}
