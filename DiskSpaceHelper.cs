namespace ESDEUpdater;

public static class DiskSpaceHelper
{
    public static long GetAvailableFreeSpace(string folderPath)
    {
        var root = Path.GetPathRoot(Path.GetFullPath(folderPath))
            ?? throw new InvalidOperationException($"Could not determine drive for path: {folderPath}");

        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }

    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        long size = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip inaccessible files.
                }
            }
        }
        catch
        {
            return size;
        }

        return size;
    }

    public static long GetDirectoriesSize(string rootPath, IEnumerable<string> folderNames)
    {
        long total = 0;

        foreach (var folderName in folderNames)
        {
            total += GetDirectorySize(Path.Combine(rootPath, folderName));
        }

        return total;
    }

    public static long GetItemsSize(string rootPath, IEnumerable<string> itemNames)
    {
        long total = 0;

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
                    total += new FileInfo(fullPath).Length;
                }
                catch
                {
                    // Skip inaccessible files.
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
